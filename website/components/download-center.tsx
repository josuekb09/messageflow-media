"use client";

import { DownloadButton } from "@/components/download-button";
import { useI18n } from "@/components/language-provider";
import { interpolate } from "@/lib/i18n";
import { site } from "@/lib/site";

export function DownloadCenter() {
  const { t } = useI18n();

  return (
    <section id="download" className="border-t border-line bg-page">
      <div className="mx-auto max-w-6xl px-5 py-24 sm:px-8">
        <div className="rounded-2xl border border-line bg-white p-8 sm:flex sm:items-center sm:justify-between sm:p-10">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-ink sm:text-3xl">
              {t.download.heading}
            </h2>
            <p className="mt-2 text-sm text-ink-secondary">
              {site.downloadFileName}
              <span className="mx-2 text-line">·</span>
              v{site.version}
              <span className="mx-2 text-line">·</span>
              {site.releaseDate}
              <span className="mx-2 text-line">·</span>
              {site.platform}
            </p>
            <p className="mt-3 max-w-lg text-sm leading-6 text-ink-muted">
              {t.download.note}
            </p>
          </div>
          <div className="mt-6 sm:mt-0">
            <DownloadButton size="lg" />
          </div>
        </div>
      </div>
    </section>
  );
}

export function DownloadHero() {
  const { t } = useI18n();

  return (
    <div className="mx-auto max-w-6xl px-5 pt-20 sm:px-8">
      <h1 className="text-3xl font-semibold tracking-tight text-ink">
        {t.download.pageTitle}
      </h1>
      <p className="mt-3 max-w-xl text-ink-secondary">
        {interpolate(t.download.pageSubtitle, {
          version: site.version,
          date: site.releaseDate,
        })}
      </p>
    </div>
  );
}
