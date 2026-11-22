"use client";

import { BrandLogo } from "@/components/brand-logo";
import { DownloadButton } from "@/components/download-button";
import { useI18n } from "@/components/language-provider";

export function CtaBand() {
  const { t } = useI18n();

  return (
    <section className="border-t border-line bg-surface">
      <div className="mx-auto max-w-6xl px-4 py-20 sm:px-8 sm:py-24">
        <div className="reveal relative isolate overflow-hidden rounded-3xl border border-line bg-card px-6 py-14 text-center shadow-card sm:px-12 sm:py-16">
          <BrandLogo className="mx-auto h-16 w-16" />
          <h2 className="mx-auto mt-7 max-w-2xl text-balance text-[2rem] font-semibold leading-[1.12] text-ink sm:text-[2.75rem]">
            {t.cta.title}
          </h2>
          <p className="mx-auto mt-5 max-w-xl text-pretty text-[17px] leading-8 text-ink-secondary">
            {t.cta.body}
          </p>
          <div className="mt-9 flex justify-center">
            <DownloadButton size="lg" className="w-full sm:w-auto" />
          </div>
        </div>
      </div>
    </section>
  );
}
