/* eslint-disable @next/next/no-img-element */

type BrandLogoProps = {
  className?: string;
};

/**
 * The MessageFlow logo as the desktop app shows it in its header: the "M" mark in
 * the app's cyan accent (#38BDF8, AccentBrush in Themes/DarkTheme.xaml).
 *
 * public/brand/mark-cyan.svg is src/MessageFlow.App/Assets/Brand/messageflow-mark.svg
 * with only the stroke colour changed to #38BDF8; the shape is untouched. The same
 * file is used for the favicon and every icon size, so one logo appears everywhere.
 */
export function BrandLogo({ className }: BrandLogoProps) {
  return <img src="/brand/mark-cyan.svg" alt="MessageFlow" width={512} height={512} className={className} />;
}
