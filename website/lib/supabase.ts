import "server-only";

/**
 * Calls a database function from supabase/schema.sql through Supabase's REST API.
 * Server-only: it uses the project's secret key, which must never reach the browser.
 */
export class SupabaseNotConfiguredError extends Error {}

function supabaseConfig() {
  const url = process.env.SUPABASE_URL?.trim().replace(/\/+$/, "");
  const key = process.env.SUPABASE_SECRET_KEY?.trim();
  if (!url || !key) {
    throw new SupabaseNotConfiguredError("SUPABASE_URL and SUPABASE_SECRET_KEY must be set.");
  }
  return { url, key };
}

export async function rpc<T>(fn: string, args: Record<string, unknown>): Promise<T> {
  const { url, key } = supabaseConfig();
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    Accept: "application/json",
    apikey: key,
  };
  // New-style secret keys (sb_secret_…) go only in `apikey`; a legacy service_role
  // key is a JWT and is also sent as the bearer token.
  if (!key.startsWith("sb_")) {
    headers.Authorization = `Bearer ${key}`;
  }

  const response = await fetch(`${url}/rest/v1/rpc/${fn}`, {
    method: "POST",
    headers,
    body: JSON.stringify(args),
    cache: "no-store",
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    throw new Error(`Supabase ${fn} failed (${response.status}): ${detail.slice(0, 300)}`);
  }
  if (response.status === 204) {
    return undefined as T;
  }
  return (await response.json()) as T;
}
