/* eslint-disable @next/next/no-img-element */

type BrandLogoProps = {
  className?: string;
};

/**
 * The MessageFlow brand logo: the gold rounded square with the cream "M", the same
 * file the desktop app uses for its window, taskbar and installer icon.
 *
 * Copied unchanged from:
 *   src/MessageFlow.App/Assets/Brand/messageflow-app-icon.svg
 * Served on the site as:
 *   /brand/app-icon.svg  →  website/public/brand/app-icon.svg
 *
 * This is the only logo used on the site. The bare "M" (messageflow-mark.svg) is the
 * in-app glyph and is not shown on its own here, so two different M logos never
 * appear side by side. The gold square reads on both the light and the dark theme,
 * so it needs no backdrop.
 */
export function BrandLogo({ className }: BrandLogoProps) {
  return <img src="/brand/app-icon.svg" alt="MessageFlow" width={512} height={512} className={className} />;
}
