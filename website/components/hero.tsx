"use client";

import { DownloadButton } from "@/components/download-button";
import { useI18n } from "@/components/language-provider";
import { interpolate, localizedSize } from "@/lib/i18n";
import { site } from "@/lib/site";

export function Hero() {
  const { t, locale } = useI18n();

  return (
    <section className="relative overflow-hidden bg-page">
      {/* A faint gold glow behind the headline, the only decoration on the page. */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute left-1/2 top-0 h-[28rem] w-[56rem] max-w-[160vw] -translate-x-1/2 rounded-full bg-brand/10 blur-3xl"
      />
      <div className="relative mx-auto max-w-4xl px-4 pb-20 pt-16 text-center sm:px-8 sm:pb-24 sm:pt-24">
        <p className="text-[12px] font-medium uppercase tracking-[0.16em] text-ink-muted sm:text-[13px]">
          {t.hero.eyebrow}
        </p>
        <h1 className="mt-6 text-balance text-[2.6rem] font-medium leading-[1.05] text-ink sm:text-6xl md:text-[4.4rem]">
          {t.hero.title}
        </h1>
        <p className="mx-auto mt-7 max-w-2xl text-pretty text-[17px] leading-8 text-ink-secondary sm:text-lg sm:leading-8">
          {t.hero.subtitle}
        </p>
        <div className="mt-10 flex flex-col items-stretch gap-3 sm:flex-row sm:items-start sm:justify-center">
          <div className="flex flex-col items-stretch sm:items-center">
            <DownloadButton size="lg" className="w-full sm:w-auto" />
            <p className="mt-2.5 text-[13px] text-ink-muted">
              {interpolate(t.hero.downloadMeta, {
                version: site.version,
                size: localizedSize(locale, site.installerSize),
              })}
            </p>
          </div>
          <a
            href="#product"
            className="inline-flex h-12 w-full items-center justify-center rounded-lg border border-ink/20 px-6 text-[15px] font-medium text-ink transition-colors hover:border-ink/50 sm:w-auto"
          >
            {t.hero.secondaryCta}
          </a>
        </div>
      </div>
    </section>
  );
}
