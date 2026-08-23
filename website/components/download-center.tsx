"use client";

import { DownloadButton } from "@/components/download-button";
import { useI18n } from "@/components/language-provider";
import { Container } from "@/components/ui";
import { interpolate } from "@/lib/i18n";
import { site } from "@/lib/site";

export function DownloadCenter() {
  const { t } = useI18n();

  return (
    <section id="download" className="relative overflow-hidden bg-slate-950 py-20 sm:py-24">
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_center,rgba(99,102,241,0.28),transparent_60%)]" />
      <Container className="relative">
        <div className="rounded-3xl border border-white/10 bg-white/5 p-8 backdrop-blur sm:flex sm:items-center sm:justify-between sm:p-10">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-white sm:text-3xl">
              {t.download.heading}
            </h2>
            <p className="mt-3 text-sm text-slate-300">
              {site.downloadFileName}
              <span className="mx-2 text-white/20">·</span>
              v{site.version}
              <span className="mx-2 text-white/20">·</span>
              {site.installerSize}
            </p>
            <p className="mt-3 max-w-lg text-sm leading-6 text-slate-400">
              {t.download.note}
            </p>
          </div>
          <div className="mt-6 sm:mt-0">
            <DownloadButton size="lg" />
          </div>
        </div>
      </Container>
    </section>
  );
}

export function DownloadHero() {
  const { t } = useI18n();

  return (
    <div className="mesh-hero border-b border-slate-200/80">
      <Container className="pt-16 pb-6 sm:pt-20">
        <h1 className="text-3xl font-semibold tracking-tight text-ink sm:text-5xl">
          {t.download.pageTitle}
        </h1>
        <p className="mt-4 max-w-xl text-base leading-7 text-slate-600">
          {interpolate(t.download.pageSubtitle, {
            version: site.version,
            date: site.releaseDate,
          })}
        </p>
      </Container>
    </div>
  );
}
