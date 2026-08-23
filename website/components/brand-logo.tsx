/* eslint-disable @next/next/no-img-element */

type BrandLogoProps = {
  variant?: "mark" | "icon";
  className?: string;
};

/**
 * Official MessageFlow mark (the blue wave “M”) from the WPF desktop app.
 *
 * Copied from:
 *   src/MessageFlow.App/Assets/Brand/messageflow-mark.svg
 * Served on the site as:
 *   /brand/mark.svg  →  website/public/brand/mark.svg
 *
 * App icon copied from:
 *   src/MessageFlow.App/Assets/Brand/messageflow-app-icon.svg
 *
 * The horizontal wordmark SVG uses white “Message” text and is not used on this
 * light page. The mark is paired with charcoal “MessageFlow” type instead,
 * matching the desktop title bar (mark + name).
 */
const sources = {
  mark: { src: "/brand/mark.svg", alt: "MessageFlow" },
  icon: { src: "/brand/app-icon.svg", alt: "MessageFlow" },
} as const;

export function BrandLogo({ variant = "mark", className }: BrandLogoProps) {
  const asset = sources[variant];
  return <img src={asset.src} alt={asset.alt} className={className} />;
}
