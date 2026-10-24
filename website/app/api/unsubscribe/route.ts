import { isToken } from "@/lib/subscription";
import { rpc } from "@/lib/supabase";

// Two callers: the button on /unsubscribe (JSON body), and the one-click
// "Unsubscribe" of Gmail/Outlook, which POSTs to the List-Unsubscribe URL with
// the token in the query string (RFC 8058).
export async function POST(request: Request) {
  let token: unknown = new URL(request.url).searchParams.get("token");
  if (!token && request.headers.get("content-type")?.includes("application/json")) {
    try {
      token = ((await request.json()) as { token?: unknown }).token;
    } catch {
      token = null;
    }
  }
  if (!isToken(token)) {
    return Response.json({ ok: false }, { status: 400 });
  }

  try {
    // Already removed counts as success: the person is off the list either way.
    await rpc<boolean>("unsubscribe", { p_token: token });
    return Response.json({ ok: true });
  } catch (error) {
    console.error("unsubscribe:", error);
    return Response.json({ ok: false }, { status: 503 });
  }
}
