import { readFile } from "node:fs/promises";
import { join } from "node:path";
import { ImageResponse } from "next/og";
import { site } from "@/lib/site";

export const alt = `${site.name} — ${site.tagline}`;
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

export default async function OpenGraphImage() {
  // The desktop app's own icon (messageflow-app-icon-1024.png resized to 512 px), unchanged.
  const icon = await readFile(join(process.cwd(), "public/brand/icons/icon-512.png"));
  const iconSrc = `data:image/png;base64,${icon.toString("base64")}`;

  return new ImageResponse(
    (
      <div
        style={{
          height: "100%",
          width: "100%",
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          padding: 80,
          background: "#ffffff",
          color: "#0A0A0A",
        }}
      >
        <div style={{ display: "flex", alignItems: "center" }}>
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={iconSrc} width={96} height={96} alt="" />
          <div style={{ display: "flex", marginLeft: 28, fontSize: 30, fontWeight: 600, color: "#0A0A0A" }}>
            MessageFlow Media · v{site.version}
          </div>
        </div>
        <div
          style={{
            display: "flex",
            fontSize: 48,
            fontWeight: 650,
            marginTop: 36,
            lineHeight: 1.2,
            maxWidth: 920,
          }}
        >
          {site.tagline}
        </div>
        <div style={{ display: "flex", marginTop: 28, fontSize: 22, color: "#737373" }}>
          {site.releaseDate} · Windows 10 / 11
        </div>
      </div>
    ),
    { ...size },
  );
}
