import type { MetadataRoute } from "next";
import { site } from "@/lib/site";

// Icons are the desktop app's own icon (messageflow-app-icon-1024.png), resized only.
export default function manifest(): MetadataRoute.Manifest {
  return {
    name: site.name,
    short_name: "MessageFlow",
    description: site.description,
    start_url: "/",
    display: "browser",
    background_color: "#0a1220",
    theme_color: "#0a1220",
    icons: [
      { src: "/brand/icons/icon-192.png", sizes: "192x192", type: "image/png" },
      { src: "/brand/icons/icon-512.png", sizes: "512x512", type: "image/png" },
    ],
  };
}
