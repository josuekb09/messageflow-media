"use client";

import { DownloadButton } from "@/components/download-button";
import { useI18n } from "@/components/language-provider";
import { interpolate } from "@/lib/i18n";
import { site } from "@/lib/site";

export function Hero() {
  const { t } = useI18n();

  return (
    <section className="bg-page">
      <div className="mx-auto max-w-4xl px-5 pb-16 pt-16 text-center sm:px-8 sm:pb-20 sm:pt-24">
        <p className="text-[13px] font-medium text-ink-muted">
          {interpolate(t.hero.eyebrow, { version: site.version })}
        </p>
        <h1 className="mt-5 text-balance text-[2.25rem] font-medium leading-[1.1] text-ink sm:text-5xl md:text-[3.75rem]">
          {t.hero.title}
        </h1>
        <p className="mx-auto mt-6 max-w-2xl text-pretty text-base leading-7 text-ink-secondary sm:text-lg sm:leading-8">
          {t.hero.subtitle}
        </p>
        <div className="mt-9 flex flex-col items-stretch gap-3 sm:flex-row sm:items-center sm:justify-center">
          <DownloadButton size="lg" className="w-full sm:w-auto" />
          <a
            href="#product"
            className="inline-flex h-12 w-full items-center justify-center rounded-lg border border-ink/15 px-6 text-[15px] font-medium text-ink transition-colors hover:border-ink/40 sm:w-auto"
          >
            {t.hero.secondaryCta}
          </a>
        </div>
      </div>
    </section>
  );
}
