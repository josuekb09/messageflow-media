import type { Locale } from "@/lib/i18n";

export const interfaceScreenshots: Record<
  Locale,
  { src: string; width: number; height: number }
> = {
  en: { src: "/media/app-english.png", width: 1918, height: 1008 },
  fr: { src: "/media/app-french.png", width: 1918, height: 1009 },
  sw: { src: "/media/app-swahili.png", width: 1918, height: 1006 },
};
