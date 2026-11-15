"use client";

import { Download } from "lucide-react";
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
        className={`btn-download group inline-flex items-center justify-center gap-2 rounded-xl bg-accent font-semibold text-accent-ink hover:bg-accent-strong ${
          large ? "h-12 px-6 text-[15px]" : "h-9 px-3.5 text-sm"
        } ${className}`}
      >
        <span aria-hidden="true" className="shine" />
        <Download aria-hidden="true" className={`btn-icon shrink-0 ${large ? "h-[18px] w-[18px]" : "h-4 w-4"}`} strokeWidth={2.25} />
        {t.download.button}
      </a>
      <NotifyDialog open={notifyOpen} onClose={() => setNotifyOpen(false)} />
    </>
  );
}
