// Real captures of the Windows app, version 1.0.10 (15 Nov 2026), converted to WebP.
// Originals: docs/screenshots-2026-11-15/*.png. The pixels are not retouched; any
// frame or shadow around them is CSS.
//
// The sermons capture is kept in the repo (website/unpublished-screens/) but is
// not shown on the site and not served from public/.
export const screens = {
  bible: { src: "/screens/bible.webp", width: 1456, height: 764 },
  songs: { src: "/screens/songs.webp", width: 1456, height: 764 },
  projection: { src: "/screens/projection.webp", width: 1456, height: 738 },
  light: { src: "/screens/light.webp", width: 1456, height: 764 },
} as const;

export type ScreenKey = keyof typeof screens;

/** Order of the app tour slideshow. */
export const tourOrder: readonly ScreenKey[] = ["bible", "songs", "projection", "light"];
