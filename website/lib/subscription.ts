import { isLocale, type Locale } from "@/lib/i18n";

// Shared by the popup and the API route, so both accept exactly the same addresses.
const EMAIL_PATTERN = /^[^\s@"(),:;<>[\\\]]+@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z]{2,}$/i;

export function normalizeEmail(value: string) {
  return value.trim().toLowerCase();
}

export function isValidEmail(value: string) {
  const email = normalizeEmail(value);
  const [local = ""] = email.split("@");
  return (
    email.length <= 254 &&
    local.length <= 64 &&
    !email.includes("..") &&
    !local.startsWith(".") &&
    !local.endsWith(".") &&
    EMAIL_PATTERN.test(email)
  );
}

export type SubscribeRequest = { email: string; language: Locale; honeypot: boolean };

export function parseSubscribeRequest(body: unknown): SubscribeRequest | null {
  if (!body || typeof body !== "object") {
    return null;
  }
  const raw = body as Record<string, unknown>;
  const email = typeof raw.email === "string" ? normalizeEmail(raw.email) : "";
  if (!isValidEmail(email)) {
    return null;
  }
  const language = typeof raw.language === "string" && isLocale(raw.language) ? raw.language : "en";
  const honeypot = typeof raw.website === "string" && raw.website.trim().length > 0;
  return { email, language, honeypot };
}

/** Tokens are 64 hex characters (see new_token() in supabase/schema.sql). */
export function isToken(value: unknown): value is string {
  return typeof value === "string" && /^[0-9a-f]{64}$/.test(value);
}
