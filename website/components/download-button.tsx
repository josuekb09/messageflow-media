"use client";

import { useState } from "react";
import { useI18n } from "@/components/language-provider";
import { NotifyDialog, wasNotifyAsked } from "@/components/notify-dialog";
import { site } from "@/lib/site";

type DownloadButtonProps = {
  size?: "md" | "lg";
  className?: string;
};

export function DownloadButton({ size = "md", className = "" }: DownloadButtonProps) {
  const { t } = useI18n();
  const [notifyOpen, setNotifyOpen] = useState(false);
  const large = size === "lg";

  return (
    <>
      <a
        href={site.downloadHref}
        target="_blank"
        rel="noopener noreferrer"
        // The download always goes ahead; the popup only offers update emails alongside it.
        onClick={() => {
          if (!wasNotifyAsked()) setNotifyOpen(true);
        }}
        className={`inline-flex items-center justify-center rounded-lg bg-brand font-semibold text-brand-ink shadow-sm transition-colors hover:bg-brand-hover ${
          large ? "h-12 px-6 text-[15px]" : "h-9 px-3.5 text-sm"
        } ${className}`}
      >
        {t.download.button}
      </a>
      <NotifyDialog open={notifyOpen} onClose={() => setNotifyOpen(false)} />
    </>
  );
}
