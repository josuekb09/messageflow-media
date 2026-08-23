import {
  buildFeedbackMail,
  buildFormSubmitPayload,
  FORMSUBMIT_AJAX_URL,
  FORMSUBMIT_SUBJECT,
  isFormSubmitAccepted,
  parseFeedbackPayload,
} from "@/lib/feedback";
import { site } from "@/lib/site";

const WEB3FORMS_ENDPOINT = "https://api.web3forms.com/submit";

type ErrorCode = "validation" | "upstream" | "invalid_json";

function jsonError(code: ErrorCode, status: number, fields?: Record<string, true>) {
  return Response.json({ ok: false, code, fields }, { status });
}

async function postJson(
  url: string,
  payload: Record<string, unknown>,
  headers?: Record<string, string>,
): Promise<boolean> {
  const response = await fetch(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      ...headers,
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

  return isFormSubmitAccepted(response.ok, result);
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

  const mail = buildFeedbackMail(parsed.data);
  const { email } = parsed.data;

  const accessKey = process.env.WEB3FORMS_ACCESS_KEY?.trim();
  if (accessKey) {
    try {
      const sent = await postJson(WEB3FORMS_ENDPOINT, {
        access_key: accessKey,
        subject: FORMSUBMIT_SUBJECT,
        from_name: mail.displayName,
        name: mail.displayName,
        email,
        replyto: email,
        to: site.supportEmail,
        category: mail.categoryLabel,
        message: parsed.data.message,
      });
      if (sent) {
        return Response.json({ ok: true });
      }
    } catch {
      // FormSubmit is the no-key default and the fallback.
    }
  }

  try {
    const sent = await postJson(
      FORMSUBMIT_AJAX_URL,
      buildFormSubmitPayload(parsed.data),
      {
        Origin: site.url,
        Referer: `${site.url}/feedback`,
      },
    );
    if (sent) {
      return Response.json({ ok: true });
    }
  } catch {
    return jsonError("upstream", 502);
  }

  return jsonError("upstream", 502);
}
