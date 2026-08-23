"use client";

import { useI18n } from "@/components/language-provider";
import { site } from "@/lib/site";

type DownloadButtonProps = {
  size?: "md" | "lg";
};

export function DownloadButton({ size = "md" }: DownloadButtonProps) {
  const { t } = useI18n();
  const large = size === "lg";

  return (
    <a
      href={site.downloadHref}
      download={site.downloadFileName}
      className={`inline-flex items-center justify-center rounded-lg bg-brand font-medium text-white transition-colors hover:bg-brand-hover ${
        large ? "h-11 px-5 text-[15px]" : "h-9 px-3.5 text-sm"
      }`}
    >
      {t.download.button}
    </a>
  );
}
