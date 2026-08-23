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
          background: "linear-gradient(180deg, #0f172a 0%, #1e1b4b 55%, #312e81 100%)",
          color: "#f8fafc",
        }}
      >
        <div
          style={{
            display: "flex",
            alignItems: "center",
            fontSize: 20,
            color: "#a5b4fc",
            fontWeight: 600,
            letterSpacing: 0.4,
          }}
        >
          {site.name} · v{site.version}
        </div>
        <div
          style={{
            display: "flex",
            fontSize: 52,
            fontWeight: 700,
            marginTop: 22,
            lineHeight: 1.15,
            maxWidth: 940,
            letterSpacing: -1.2,
          }}
        >
          {site.tagline}
        </div>
        <div style={{ display: "flex", marginTop: 28, fontSize: 22, color: "#cbd5e1" }}>
          Offline · English · Français · Kiswahili · {site.platform}
        </div>
      </div>
    ),
    { ...size },
  );
}
