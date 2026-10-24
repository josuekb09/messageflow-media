import { createHash, timingSafeEqual } from "node:crypto";
import { announcementEmail, ResendRejectedError, sendBatch, sendEmail } from "@/lib/email";
import { isLocale } from "@/lib/i18n";
import { extractWhatsNew } from "@/lib/release-notes";
import { site } from "@/lib/site";
import { isValidEmail, normalizeEmail } from "@/lib/subscription";
import { rpc } from "@/lib/supabase";

// Called by .github/workflows/announce-release.yml when a release is published.
export const maxDuration = 300;

const BATCH_SIZE = 100;
// Resend allows a few requests per second; stay well under it.
const PAUSE_BETWEEN_BATCHES_MS = 600;

type Release = { tag_name: string; name: string | null; body: string | null; draft: boolean; prerelease: boolean };
type Recipient = { subscriber_id: string; email: string; language: string; unsubscribe_token: string };

function authorized(request: Request) {
  const secret = process.env.ANNOUNCE_SECRET?.trim();
  const header = request.headers.get("authorization") ?? "";
  if (!secret || secret.length < 32 || !header.startsWith("Bearer ")) {
    return false;
  }
  const digest = (value: string) => createHash("sha256").update(value).digest();
  return timingSafeEqual(digest(header.slice("Bearer ".length).trim()), digest(secret));
}

async function github(path: string): Promise<Release | null> {
  const token = process.env.GITHUB_TOKEN?.trim();
  const response = await fetch(`https://api.github.com/repos/${site.githubRepo}${path}`, {
    headers: {
      Accept: "application/vnd.github+json",
      "User-Agent": "messageflow-website",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    cache: "no-store",
  });
  if (response.status === 404) {
    return null;
  }
  if (!response.ok) {
    throw new Error(`GitHub ${path} failed (${response.status})`);
  }
  return (await response.json()) as Release;
}

const sleep = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms));

function fail(status: number, error: string, extra?: Record<string, unknown>) {
  return Response.json({ ok: false, error, ...extra }, { status });
}

export async function POST(request: Request) {
  if (!authorized(request)) {
    return fail(401, "unauthorized");
  }

  let tag = "";
  let previewTo = "";
  try {
    const body = (await request.json()) as { tag?: unknown; preview_to?: unknown };
    tag = typeof body.tag === "string" ? body.tag.trim() : "";
    previewTo = typeof body.preview_to === "string" ? normalizeEmail(body.preview_to) : "";
  } catch {
    return fail(400, "invalid_json");
  }
  if (!/^[\w.\-]{1,100}$/.test(tag)) {
    return fail(400, "invalid_tag");
  }
  if (previewTo && !isValidEmail(previewTo)) {
    return fail(400, "invalid_preview_to");
  }

  // GitHub, not the caller, decides what is announced. "latest" never returns a
  // draft or a pre-release, and an older release can never be announced after a newer one.
  let release: Release | null;
  try {
    release = previewTo
      ? await github(`/releases/tags/${encodeURIComponent(tag)}`)
      : await github("/releases/latest");
  } catch (error) {
    console.error("announce github:", error);
    return fail(502, "github_unavailable");
  }
  if (!release || release.draft || release.prerelease) {
    return fail(409, "not_a_published_release", { tag });
  }
  if (!previewTo && release.tag_name !== tag) {
    return Response.json({ ok: true, skipped: "not_latest", tag, latest: release.tag_name });
  }

  const version = release.tag_name.replace(/^v(?=\d)/i, "");
  const notes = extractWhatsNew(release.body ?? "");

  // Preview: one email to one address, nothing recorded, so a real announcement can still follow.
  if (previewTo) {
    try {
      await sendEmail(announcementEmail("en", previewTo, version, notes, "preview"));
    } catch (error) {
      console.error("announce preview:", error);
      return fail(502, "email_failed");
    }
    return Response.json({ ok: true, preview: true, tag, sent: 1 });
  }

  let claim: "new" | "resume" | "sent";
  try {
    claim = await rpc<"new" | "resume" | "sent">("claim_announcement", { p_tag: tag, p_version: version });
  } catch (error) {
    console.error("announce claim:", error);
    return fail(503, "database_unavailable");
  }
  if (claim === "sent") {
    return Response.json({ ok: true, skipped: "already_announced", tag });
  }

  let sent = 0;
  for (;;) {
    let batch: Recipient[];
    try {
      batch = await rpc<Recipient[]>("reserve_deliveries", { p_tag: tag, p_limit: BATCH_SIZE });
    } catch (error) {
      console.error("announce reserve:", error);
      return fail(503, "database_unavailable", { tag, sent });
    }
    if (batch.length === 0) {
      break;
    }

    try {
      await sendBatch(
        batch.map((recipient) =>
          announcementEmail(
            isLocale(recipient.language) ? recipient.language : "en",
            recipient.email,
            version,
            notes,
            recipient.unsubscribe_token,
          ),
        ),
        `announce/${tag}/${batch[0].subscriber_id}/${batch.length}`,
      );
    } catch (error) {
      console.error("announce send:", error);
      if (error instanceof ResendRejectedError) {
        // Refused outright, so nobody in this batch got it: free them for a retry.
        await rpc("release_reservations", {
          p_tag: tag,
          p_subscriber_ids: batch.map((recipient) => recipient.subscriber_id),
        }).catch(() => {});
      }
      // Otherwise the outcome is unknown; those people stay marked so they can never get it twice.
      return fail(502, "email_failed", { tag, sent });
    }

    sent += batch.length;
    if (batch.length < BATCH_SIZE) {
      break;
    }
    await sleep(PAUSE_BETWEEN_BATCHES_MS);
  }

  let total = sent;
  try {
    total = await rpc<number>("finish_announcement", { p_tag: tag });
  } catch (error) {
    console.error("announce finish:", error);
  }
  return Response.json({ ok: true, tag, sent, total, resumed: claim === "resume" });
}
