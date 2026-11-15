/*
 * Every other icon on the site comes from lucide-react. The GitHub mark is kept
 * here as an inline SVG because lucide no longer ships brand logos. Same stroke
 * style as lucide (24 px grid, round caps, no fill), so it sits in the same set.
 */

type IconProps = { className?: string };

export function GitHubIcon({ className }: IconProps) {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={2}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      <path d="M9 19c-4 1.3-4-2-5.5-2.5M15 21v-3.4a3 3 0 0 0-.8-2.2c2.7-.3 5.5-1.3 5.5-6a4.6 4.6 0 0 0-1.3-3.2 4.3 4.3 0 0 0-.1-3.2s-1-.3-3.3 1.3a11.5 11.5 0 0 0-6 0C6.7 2.7 5.7 3 5.7 3a4.3 4.3 0 0 0-.1 3.2A4.6 4.6 0 0 0 4.3 9.4c0 4.7 2.8 5.7 5.5 6a3 3 0 0 0-.8 2.2V21" />
    </svg>
  );
}
