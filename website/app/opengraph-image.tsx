import { ImageResponse } from "next/og";
import { site } from "@/lib/site";

export const alt = `${site.name} — ${site.tagline}`;
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

export default function OpenGraphImage() {
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
          color: "#0F172A",
        }}
      >
        <div style={{ display: "flex", fontSize: 20, color: "#2563EB", fontWeight: 600 }}>
          MessageFlow · v{site.version}
        </div>
        <div
          style={{
            display: "flex",
            fontSize: 48,
            fontWeight: 650,
            marginTop: 18,
            lineHeight: 1.2,
            maxWidth: 920,
          }}
        >
          {site.tagline}
        </div>
        <div style={{ display: "flex", marginTop: 28, fontSize: 22, color: "#64748B" }}>
          {site.releaseDate} · Windows 10 / 11
        </div>
      </div>
    ),
    { ...size },
  );
}
