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
