-- MessageFlow Media: release announcement subscribers.
--
-- Run this once in the Supabase dashboard: SQL Editor > New query > paste > Run.
-- It is safe to run again; every statement checks what already exists.
--
-- Only the website server talks to these tables, with the project's secret
-- (service_role) key. Row Level Security is on with no policies, so the public
-- publishable/anon key can read or write nothing.

-- 64 hex characters (244 random bits) from the built-in UUID generator.
create or replace function public.new_token()
returns text
language sql
volatile
set search_path = public
as $$
  select replace(gen_random_uuid()::text, '-', '') || replace(gen_random_uuid()::text, '-', '');
$$;

create table if not exists public.subscribers (
  id uuid primary key default gen_random_uuid(),
  -- Stored lower-cased and trimmed by subscribe(); unique, so an address is on the list once.
  email text not null unique check (email = lower(btrim(email)) and length(email) between 3 and 254),
  language text not null default 'en' check (language in ('en', 'fr', 'sw')),
  confirmed boolean not null default false,
  created_at timestamptz not null default now(),
  confirmed_at timestamptz,
  confirm_token text unique,
  confirm_sent_at timestamptz,
  unsubscribe_token text not null unique default public.new_token(),
  -- Not used yet. Reserved for WhatsApp/SMS announcements later.
  phone text
);

-- One row per release that was announced. The tag is the primary key, so a
-- release can be claimed only once.
create table if not exists public.release_announcements (
  tag text primary key,
  version text not null,
  status text not null default 'sending' check (status in ('sending', 'sent')),
  started_at timestamptz not null default now(),
  finished_at timestamptz,
  recipient_count integer not null default 0
);

-- One row per (release, subscriber) email. The row is written before the email
-- is sent, so nobody can receive the same release twice, even if a run is retried.
create table if not exists public.release_deliveries (
  tag text not null references public.release_announcements (tag) on delete cascade,
  subscriber_id uuid not null references public.subscribers (id) on delete cascade,
  reserved_at timestamptz not null default now(),
  primary key (tag, subscriber_id)
);

alter table public.subscribers enable row level security;
alter table public.release_announcements enable row level security;
alter table public.release_deliveries enable row level security;

revoke all on public.subscribers, public.release_announcements, public.release_deliveries
  from anon, authenticated;

-- Adds or refreshes a subscriber and says whether to send a confirmation email.
-- A confirmed address is left untouched; a pending one gets a fresh link, at
-- most once a minute so the form cannot be used to flood someone's inbox.
create or replace function public.subscribe(p_email text, p_language text)
returns table (status text, confirm_token text, unsubscribe_token text)
language plpgsql
set search_path = public
as $$
declare
  v_email text := lower(btrim(p_email));
  v_language text := case when p_language in ('en', 'fr', 'sw') then p_language else 'en' end;
  v_row public.subscribers%rowtype;
begin
  insert into public.subscribers (email, language)
  values (v_email, v_language)
  on conflict (email) do nothing;

  select * into v_row from public.subscribers s where s.email = v_email for update;

  if v_row.confirmed then
    return query select 'already_confirmed'::text, null::text, v_row.unsubscribe_token;
    return;
  end if;

  if v_row.confirm_sent_at is not null and v_row.confirm_sent_at > now() - interval '1 minute' then
    return query select 'throttled'::text, null::text, v_row.unsubscribe_token;
    return;
  end if;

  update public.subscribers s
  set confirm_token = public.new_token(),
      confirm_sent_at = now(),
      language = v_language
  where s.id = v_row.id
  returning * into v_row;

  return query select 'send_confirmation'::text, v_row.confirm_token, v_row.unsubscribe_token;
end;
$$;

-- Lets a failed confirmation email be retried right away.
create or replace function public.reset_confirmation_throttle(p_email text)
returns void
language sql
set search_path = public
as $$
  update public.subscribers set confirm_sent_at = null
  where email = lower(btrim(p_email)) and not confirmed;
$$;

-- Confirms the subscriber whose link was clicked. Links expire after 7 days.
create or replace function public.confirm_subscription(p_token text)
returns text
language plpgsql
set search_path = public
as $$
declare
  v_row public.subscribers%rowtype;
begin
  select * into v_row from public.subscribers where confirm_token = p_token for update;
  if not found then
    return 'invalid';
  end if;
  if v_row.confirm_sent_at < now() - interval '7 days' then
    return 'expired';
  end if;

  update public.subscribers
  set confirmed = true, confirmed_at = now(), confirm_token = null
  where id = v_row.id;
  return 'confirmed';
end;
$$;

-- Unsubscribing deletes the address completely.
create or replace function public.unsubscribe(p_token text)
returns boolean
language sql
set search_path = public
as $$
  with removed as (
    delete from public.subscribers where unsubscribe_token = p_token returning 1
  )
  select exists (select 1 from removed);
$$;

-- Claims a release for announcing. Returns 'new' the first time, 'resume' if an
-- earlier run stopped part-way, and 'sent' once it has been fully announced.
create or replace function public.claim_announcement(p_tag text, p_version text)
returns text
language plpgsql
set search_path = public
as $$
declare
  v_status text;
begin
  insert into public.release_announcements (tag, version)
  values (p_tag, p_version)
  on conflict (tag) do nothing;
  if found then
    return 'new';
  end if;

  select status into v_status from public.release_announcements where tag = p_tag;
  return case when v_status = 'sent' then 'sent' else 'resume' end;
end;
$$;

-- Reserves the next batch of confirmed subscribers who have not had this
-- release yet. The insert is atomic, so two runs at once never pick the same person.
create or replace function public.reserve_deliveries(p_tag text, p_limit integer)
returns table (subscriber_id uuid, email text, language text, unsubscribe_token text)
language sql
set search_path = public
as $$
  with picked as (
    select s.id
    from public.subscribers s
    where s.confirmed
      and not exists (
        select 1 from public.release_deliveries d
        where d.tag = p_tag and d.subscriber_id = s.id
      )
    order by s.created_at, s.id
    limit p_limit
  ),
  reserved as (
    insert into public.release_deliveries (tag, subscriber_id)
    select p_tag, picked.id from picked
    on conflict do nothing
    returning release_deliveries.subscriber_id
  )
  select s.id, s.email, s.language, s.unsubscribe_token
  from reserved r
  join public.subscribers s on s.id = r.subscriber_id;
$$;

-- Frees reservations for emails the mail service definitely refused, so a retry can send them.
create or replace function public.release_reservations(p_tag text, p_subscriber_ids uuid[])
returns void
language sql
set search_path = public
as $$
  delete from public.release_deliveries
  where tag = p_tag and subscriber_id = any (p_subscriber_ids);
$$;

create or replace function public.finish_announcement(p_tag text)
returns integer
language plpgsql
set search_path = public
as $$
declare
  v_count integer;
begin
  select count(*) into v_count from public.release_deliveries where tag = p_tag;
  update public.release_announcements
  set status = 'sent', finished_at = now(), recipient_count = v_count
  where tag = p_tag;
  return v_count;
end;
$$;

revoke execute on function
  public.new_token(),
  public.subscribe(text, text),
  public.reset_confirmation_throttle(text),
  public.confirm_subscription(text),
  public.unsubscribe(text),
  public.claim_announcement(text, text),
  public.reserve_deliveries(text, integer),
  public.release_reservations(text, uuid[]),
  public.finish_announcement(text)
from public, anon, authenticated;

grant execute on function
  public.new_token(),
  public.subscribe(text, text),
  public.reset_confirmation_throttle(text),
  public.confirm_subscription(text),
  public.unsubscribe(text),
  public.claim_announcement(text, text),
  public.reserve_deliveries(text, integer),
  public.release_reservations(text, uuid[]),
  public.finish_announcement(text)
to service_role;
