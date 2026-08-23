import {
  CATEGORY_SUBJECT,
  parseFeedbackPayload,
} from "@/lib/feedback";
import { site } from "@/lib/site";

const WEB3FORMS_ENDPOINT = "https://api.web3forms.com/submit";

type ErrorCode = "validation" | "not_configured" | "upstream" | "invalid_json";

function jsonError(code: ErrorCode, status: number, fields?: Record<string, true>) {
  return Response.json({ ok: false, code, fields }, { status });
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

  const accessKey = process.env.WEB3FORMS_ACCESS_KEY?.trim();
  if (!accessKey) {
    return jsonError("not_configured", 503);
  }

  const { name, email, category, message } = parsed.data;
  const categoryLabel = CATEGORY_SUBJECT[category];
  const displayName = name || "Anonymous";

  const payload = {
    access_key: accessKey,
    subject: `[MessageFlow] ${categoryLabel}`,
    from_name: displayName,
    name: displayName,
    email,
    replyto: email,
    to: site.supportEmail,
    category: categoryLabel,
    message: [
      `To: ${site.supportEmail}`,
      `Category: ${categoryLabel}`,
      `Name: ${displayName}`,
      `Email: ${email}`,
      "",
      message,
    ].join("\n"),
  };

  let upstream: Response;
  try {
    upstream = await fetch(WEB3FORMS_ENDPOINT, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
      },
      body: JSON.stringify(payload),
      cache: "no-store",
    });
  } catch {
    return jsonError("upstream", 502);
  }

  let result: { success?: boolean } = {};
  try {
    result = (await upstream.json()) as { success?: boolean };
  } catch {
    return jsonError("upstream", 502);
  }

  if (!upstream.ok || result.success === false) {
    return jsonError("upstream", 502);
  }

  return Response.json({ ok: true });
}
