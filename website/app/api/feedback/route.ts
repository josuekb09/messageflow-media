import {
  CATEGORY_SUBJECT,
  parseFeedbackPayload,
} from "@/lib/feedback";
import { site } from "@/lib/site";

const WEB3FORMS_ENDPOINT = "https://api.web3forms.com/submit";
const FORMSUBMIT_ENDPOINT = `https://formsubmit.co/ajax/${site.supportEmail}`;

type ErrorCode = "validation" | "upstream" | "invalid_json";

function jsonError(code: ErrorCode, status: number, fields?: Record<string, true>) {
  return Response.json({ ok: false, code, fields }, { status });
}

function isSuccessFlag(value: unknown): boolean {
  return value === true || value === "true";
}

function isFailureFlag(value: unknown): boolean {
  return value === false || value === "false";
}

function looksLikeActivation(message: unknown): boolean {
  if (typeof message !== "string") return false;
  const text = message.toLowerCase();
  return (
    text.includes("activat") ||
    text.includes("confirm") ||
    text.includes("check your email")
  );
}

function isUpstreamSuccess(httpOk: boolean, result: unknown): boolean {
  if (!result || typeof result !== "object") return httpOk;
  const record = result as { success?: unknown; message?: unknown };
  if (isSuccessFlag(record.success)) return true;
  if (isFailureFlag(record.success)) return looksLikeActivation(record.message);
  return httpOk;
}

async function postJson(url: string, payload: Record<string, unknown>): Promise<boolean> {
  const response = await fetch(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
    },
    body: JSON.stringify(payload),
    cache: "no-store",
  });

  let result: unknown = {};
  try {
    result = await response.json();
  } catch {
    return response.ok;
  }

  return isUpstreamSuccess(response.ok, result);
}

function messageBody(
  displayName: string,
  email: string,
  categoryLabel: string,
  message: string,
) {
  return [
    `To: ${site.supportEmail}`,
    `Category: ${categoryLabel}`,
    `Name: ${displayName}`,
    `Email: ${email}`,
    "",
    message,
  ].join("\n");
}

async function sendViaWeb3Forms(
  accessKey: string,
  displayName: string,
  email: string,
  categoryLabel: string,
  subject: string,
  body: string,
): Promise<boolean> {
  return postJson(WEB3FORMS_ENDPOINT, {
    access_key: accessKey,
    subject,
    from_name: displayName,
    name: displayName,
    email,
    replyto: email,
    to: site.supportEmail,
    category: categoryLabel,
    message: body,
  });
}

async function sendViaFormSubmit(
  displayName: string,
  email: string,
  categoryLabel: string,
  subject: string,
  body: string,
): Promise<boolean> {
  return postJson(FORMSUBMIT_ENDPOINT, {
    name: displayName,
    email,
    message: body,
    category: categoryLabel,
    _subject: subject,
    _template: "table",
    _captcha: "false",
  });
}

export async function POST(request: Request) {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return jsonError("invalid_json", 400);
  }

  const parsed = parseFeedbackPayload(body);
  if (!parsed.ok) {
    return jsonError("validation", 400, parsed.fields);
  }

  if (parsed.honeypot) {
    return Response.json({ ok: true });
  }

  const { name, email, category, message } = parsed.data;
  const categoryLabel = CATEGORY_SUBJECT[category];
  const displayName = name || "Anonymous";
  const subject = `[MessageFlow] ${categoryLabel}`;
  const bodyText = messageBody(displayName, email, categoryLabel, message);

  const accessKey = process.env.WEB3FORMS_ACCESS_KEY?.trim();
  if (accessKey) {
    try {
      if (await sendViaWeb3Forms(accessKey, displayName, email, categoryLabel, subject, bodyText)) {
        return Response.json({ ok: true });
      }
    } catch {
      // FormSubmit is the no-key default and the fallback.
    }
  }

  try {
    if (await sendViaFormSubmit(displayName, email, categoryLabel, subject, bodyText)) {
      return Response.json({ ok: true });
    }
  } catch {
    return jsonError("upstream", 502);
  }

  return jsonError("upstream", 502);
}
