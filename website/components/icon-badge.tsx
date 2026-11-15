import type { LucideIcon } from "lucide-react";

/** The rounded icon tile used on every card; it fills with cyan when the card is hovered. */
export function IconBadge({ icon: Icon }: { icon: LucideIcon }) {
  return (
    <span className="card-icon inline-flex h-12 w-12 shrink-0 items-center justify-center rounded-2xl bg-accent-soft text-accent-text ring-1 ring-inset ring-accent/25">
      <Icon aria-hidden="true" className="h-6 w-6" strokeWidth={1.75} />
    </span>
  );
}
