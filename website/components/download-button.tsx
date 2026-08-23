"use client";

import { ArrowDownToLine } from "lucide-react";
import { useI18n } from "@/components/language-provider";
import { cn } from "@/lib/cn";
import { site } from "@/lib/site";

type DownloadButtonProps = {
  size?: "md" | "lg";
  className?: string;
};

export function DownloadButton({ size = "md", className }: DownloadButtonProps) {
  const { t } = useI18n();
  const large = size === "lg";

  return (
    <a
      href={site.downloadHref}
      download={site.downloadFileName}
      className={cn(
        "inline-flex items-center justify-center gap-2 rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 font-semibold text-white shadow-lg shadow-indigo-500/25 transition-all duration-200 ease-in-out hover:-translate-y-0.5 hover:shadow-xl hover:shadow-indigo-500/30 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-indigo-500/40",
        large ? "h-12 px-6 text-[15px]" : "h-10 px-4 text-sm",
        className,
      )}
    >
      <ArrowDownToLine className="h-4 w-4" />
      {t.download.button}
    </a>
  );
}
