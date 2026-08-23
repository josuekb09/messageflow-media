/* eslint-disable @next/next/no-img-element */

import Link from "next/link";
import { cn } from "@/lib/cn";
import { site } from "@/lib/site";

type BrandLogoProps = {
  variant?: "mark" | "icon";
  className?: string;
};

const sources = {
  mark: { src: "/brand/mark.svg", alt: site.name },
  icon: { src: "/brand/app-icon.svg", alt: site.name },
} as const;

export function BrandLogo({ variant = "mark", className }: BrandLogoProps) {
  const asset = sources[variant];
  return <img src={asset.src} alt={asset.alt} className={className} />;
}

export function BrandLockup({
  tone = "light",
  className,
}: {
  tone?: "light" | "dark";
  className?: string;
}) {
  return (
    <Link href="/" className={cn("flex items-center gap-2.5", className)}>
      <span className="inline-flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-blue-500 to-indigo-600 shadow-md shadow-indigo-500/25">
        <img src="/brand/mark-white.svg" alt="" className="h-5 w-5" />
      </span>
      <span
        className={cn(
          "text-[15px] font-semibold tracking-tight",
          tone === "dark" ? "text-white" : "text-ink",
        )}
      >
        {site.name}
      </span>
    </Link>
  );
}
