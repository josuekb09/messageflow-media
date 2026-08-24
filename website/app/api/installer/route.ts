import { NextResponse } from "next/server";

const SOURCE =
  process.env.INSTALLER_BLOB_URL ??
  "https://github.com/josuekb09/messageflow-media/releases/download/v1.0.2/MessageFlowMediaSetup.exe";

export const dynamic = "force-dynamic";
export const runtime = "nodejs";

async function resolveInstallerUrl(): Promise<string | Response> {
  const upstream = await fetch(SOURCE, {
    method: "GET",
    redirect: "manual",
    headers: {
      Accept: "application/octet-stream",
      "User-Agent": "MessageFlowMediaDownload/1.0",
    },
    cache: "no-store",
  });

  const location = upstream.headers.get("location");
  if (location) {
    return location;
  }

  if (upstream.ok && upstream.body) {
    return new Response(upstream.body, {
      headers: {
        "Content-Type": "application/octet-stream",
        "Content-Disposition": 'attachment; filename="MessageFlowMediaSetup.exe"',
      },
    });
  }

  return new Response("The installer could not be downloaded. Please try again in a moment.", {
    status: 502,
  });
}

export async function GET() {
  const resolved = await resolveInstallerUrl();
  if (typeof resolved !== "string") {
    return resolved;
  }

  return NextResponse.redirect(resolved, 302);
}

export async function HEAD() {
  return GET();
}
