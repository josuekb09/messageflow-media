import type { MetadataRoute } from "next";
import { site } from "@/lib/site";

export default function sitemap(): MetadataRoute.Sitemap {
  return [
    {
      url: site.url,
      lastModified: new Date("2026-08-22"),
      changeFrequency: "monthly",
      priority: 1,
    },
    {
      url: `${site.url}/download`,
      lastModified: new Date("2026-08-22"),
      changeFrequency: "monthly",
      priority: 0.9,
    },
    {
      url: `${site.url}/feedback`,
      lastModified: new Date("2026-08-23"),
      changeFrequency: "monthly",
      priority: 0.6,
    },
  ];
}
