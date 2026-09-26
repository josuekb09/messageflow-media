"use client";

import { useI18n } from "@/components/language-provider";
import { site } from "@/lib/site";

type DownloadButtonProps = {
  size?: "md" | "lg";
  className?: string;
};

export function DownloadButton({ size = "md", className = "" }: DownloadButtonProps) {
  const { t } = useI18n();
  const large = size === "lg";

  return (
    <a
      href={site.downloadHref}
      target="_blank"
      rel="noopener noreferrer"
      className={`inline-flex items-center justify-center rounded-lg bg-brand font-semibold text-ink transition-colors hover:bg-brand-hover ${
        large ? "h-12 px-6 text-[15px]" : "h-9 px-3.5 text-sm"
      } ${className}`}
    >
      {t.download.button}
    </a>
  );
}
