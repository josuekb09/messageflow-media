"use client";

import { BrandLogo } from "@/components/brand-logo";
import { DownloadButton } from "@/components/download-button";
import { useI18n } from "@/components/language-provider";

export function CtaBand() {
  const { t } = useI18n();

  return (
    <section className="border-t border-line bg-surface">
      <div className="reveal mx-auto max-w-3xl px-4 py-20 text-center sm:px-8 sm:py-24">
        <BrandLogo className="mx-auto h-12 w-12" />
        <h2 className="mt-7 text-balance text-[2.1rem] font-medium leading-[1.15] text-ink sm:text-5xl">
          {t.cta.title}
        </h2>
        <p className="mx-auto mt-5 max-w-xl text-pretty text-[17px] leading-8 text-ink-secondary">
          {t.cta.body}
        </p>
        <div className="mt-9 flex justify-center">
          <DownloadButton size="lg" className="w-full sm:w-auto" />
        </div>
      </div>
    </section>
  );
}
