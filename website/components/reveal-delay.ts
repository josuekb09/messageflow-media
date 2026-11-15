import type { CSSProperties } from "react";

/** Inline style that staggers a `.reveal` item in a row of cards (index 0, 1, 2…). */
export function revealDelay(index: number, stepMs = 90): CSSProperties {
  return { "--reveal-delay": `${index * stepMs}ms` } as CSSProperties;
}
