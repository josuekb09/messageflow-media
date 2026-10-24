import { confirmationEmail, ResendNotConfiguredError, sendEmail } from "@/lib/email";
import { parseSubscribeRequest } from "@/lib/subscription";
import { rpc, SupabaseNotConfiguredError } from "@/lib/supabase";

type SubscribeRow = {
  status: "send_confirmation" | "already_confirmed" | "throttled";
  confirm_token: string | null;
  unsubscribe_token: string;
};

type ErrorCode = "validation" | "invalid_json" | "not_configured" | "upstream";

function jsonError(code: ErrorCode, status: number) {
  return Response.json({ ok: false, code }, { status });
}

export async function POST(request: Request) {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return jsonError("invalid_json", 400);
  }

  const parsed = parseSubscribeRequest(body);
  if (!parsed) {
    return jsonError("validation", 400);
  }
  if (parsed.honeypot) {
    return Response.json({ ok: true });
  }

  let row: SubscribeRow | undefined;
  try {
    [row] = await rpc<SubscribeRow[]>("subscribe", {
      p_email: parsed.email,
      p_language: parsed.language,
    });
  } catch (error) {
    console.error("subscribe:", error);
    return jsonError(error instanceof SupabaseNotConfiguredError ? "not_configured" : "upstream", 503);
  }

  // The answer is the same whether or not the address was already on the list,
  // so the form cannot be used to find out who has subscribed.
  if (row?.status !== "send_confirmation" || !row.confirm_token) {
    return Response.json({ ok: true });
  }

  try {
    await sendEmail(
      confirmationEmail(parsed.language, parsed.email, row.confirm_token, row.unsubscribe_token),
    );
  } catch (error) {
    console.error("subscribe email:", error);
    await rpc("reset_confirmation_throttle", { p_email: parsed.email }).catch(() => {});
    return jsonError(error instanceof ResendNotConfiguredError ? "not_configured" : "upstream", 503);
  }

  return Response.json({ ok: true });
}
