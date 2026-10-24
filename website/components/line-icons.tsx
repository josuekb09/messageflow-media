/*
 * Line icons matching the app's navigation rail (library, Bible cross, music note):
 * one stroke weight, round caps, no fill. They inherit the text colour.
 */

import type { ReactNode } from "react";

type IconProps = { className?: string };

function Stroke({ className, children }: IconProps & { children: ReactNode }) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.5}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      {children}
    </svg>
  );
}

export function BookIcon({ className }: IconProps) {
  return (
    <Stroke className={className}>
      <path d="M3 5.5c2.8-1.2 5.8-1.2 9 .8 3.2-2 6.2-2 9-.8v13c-2.8-1.2-5.8-1.2-9 .8-3.2-2-6.2-2-9-.8z" />
      <path d="M12 6.3v13" />
    </Stroke>
  );
}

export function CrossIcon({ className }: IconProps) {
  return (
    <Stroke className={className}>
      <path d="M12 3v18M6.5 8.5h11" />
    </Stroke>
  );
}

export function MusicNoteIcon({ className }: IconProps) {
  return (
    <Stroke className={className}>
      <path d="M14 17V4.5c2 .4 3.6 1.6 4.5 3.5" />
      <ellipse cx="11" cy="17.5" rx="3" ry="2.5" />
    </Stroke>
  );
}

/** An open book with a check: what is shown matches the printed page. */
export function FaithfulIcon({ className }: IconProps) {
  return (
    <Stroke className={className}>
      <path d="M3 5.5c2.8-1.2 5.8-1.2 9 .8 3.2-2 6.2-2 9-.8v8" />
      <path d="M3 5.5v13c2.8-1.2 5.8-1.2 9 .8" />
      <path d="M12 6.3v7" />
      <path d="M14.5 18l2 2 4-4.5" />
    </Stroke>
  );
}

export function StopwatchIcon({ className }: IconProps) {
  return (
    <Stroke className={className}>
      <circle cx="12" cy="13.5" r="7.5" />
      <path d="M12 9.5v4l2.5 2M10 2.5h4M12 2.5V6" />
    </Stroke>
  );
}

/** A laptop: everything runs on the computer, with no internet. */
export function LaptopIcon({ className }: IconProps) {
  return (
    <Stroke className={className}>
      <rect x="4" y="5" width="16" height="11" rx="1.5" />
      <path d="M2 19.5h20" />
    </Stroke>
  );
}

export function DownloadIcon({ className }: IconProps) {
  return (
    <Stroke className={className}>
      <path d="M12 3.5v11M7.5 10l4.5 4.5 4.5-4.5" />
      <path d="M4.5 16.5v2a2 2 0 0 0 2 2h11a2 2 0 0 0 2-2v-2" />
    </Stroke>
  );
}

export function InstallIcon({ className }: IconProps) {
  return (
    <Stroke className={className}>
      <rect x="3.5" y="4.5" width="17" height="15" rx="2" />
      <path d="M3.5 8.5h17M8 14l2.5 2.5L16 11" />
    </Stroke>
  );
}

/** A screen on a stand: the projector or TV. */
export function ScreenIcon({ className }: IconProps) {
  return (
    <Stroke className={className}>
      <rect x="2.5" y="4" width="19" height="12.5" rx="1.5" />
      <path d="M12 16.5V20M8 20h8" />
    </Stroke>
  );
}

export function SearchIcon({ className }: IconProps) {
  return (
    <Stroke className={className}>
      <circle cx="10.5" cy="10.5" r="6.5" />
      <path d="M15.5 15.5L20.5 20.5" />
    </Stroke>
  );
}

export function GitHubIcon({ className }: IconProps) {
  return (
    <Stroke className={className}>
      <path d="M9 19c-4 1.3-4-2-5.5-2.5M15 21v-3.4a3 3 0 0 0-.8-2.2c2.7-.3 5.5-1.3 5.5-6a4.6 4.6 0 0 0-1.3-3.2 4.3 4.3 0 0 0-.1-3.2s-1-.3-3.3 1.3a11.5 11.5 0 0 0-6 0C6.7 2.7 5.7 3 5.7 3a4.3 4.3 0 0 0-.1 3.2A4.6 4.6 0 0 0 4.3 9.4c0 4.7 2.8 5.7 5.5 6a3 3 0 0 0-.8 2.2V21" />
    </Stroke>
  );
}
