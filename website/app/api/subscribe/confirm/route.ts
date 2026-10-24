import { isToken } from "@/lib/subscription";
import { rpc } from "@/lib/supabase";

// Called by the button on /subscribe/confirm, never by the email link itself,
// so mail scanners that open links cannot confirm an address on their own.
export async function POST(request: Request) {
  let token: unknown;
  try {
    token = ((await request.json()) as { token?: unknown }).token;
  } catch {
    return Response.json({ ok: false, status: "invalid" }, { status: 400 });
  }
  if (!isToken(token)) {
    return Response.json({ ok: false, status: "invalid" }, { status: 400 });
  }

  try {
    const status = await rpc<"confirmed" | "expired" | "invalid">("confirm_subscription", {
      p_token: token,
    });
    return Response.json({ ok: status === "confirmed", status });
  } catch (error) {
    console.error("confirm:", error);
    return Response.json({ ok: false, status: "error" }, { status: 503 });
  }
}
