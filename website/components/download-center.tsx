"use client";

import { DownloadButton } from "@/components/download-button";
import { useI18n } from "@/components/language-provider";
import { interpolate, localizedMonth, localizedSize } from "@/lib/i18n";
import { site } from "@/lib/site";

export function DownloadCenter() {
  const { t, locale } = useI18n();

  return (
    <section id="download" className="border-t border-line bg-surface">
      <div className="mx-auto max-w-6xl px-5 py-24 sm:px-8">
        <div className="rounded-2xl border border-line bg-card p-5 sm:flex sm:items-center sm:justify-between sm:p-10">
          <div>
            <h2 className="text-2xl font-medium text-ink sm:text-3xl">
              {t.download.heading}
            </h2>
            <p className="mt-2 flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-ink-secondary">
              <span>v{site.version}</span>
              <span className="text-line">·</span>
              <span>{localizedMonth(locale, site.releaseMonth)}</span>
              <span className="text-line">·</span>
              <span>{site.platform}</span>
            </p>
            <p className="mt-3 max-w-lg text-sm leading-6 text-ink-muted">
              {interpolate(t.download.note, { size: localizedSize(locale, site.installerSize) })}
            </p>
          </div>
          <div className="mt-6 sm:mt-0 sm:shrink-0">
            <DownloadButton size="lg" className="w-full sm:w-auto" />
          </div>
        </div>
        <DownloadSafety />
      </div>
    </section>
  );
}

function DownloadSafety() {
  const { t } = useI18n();

  return (
    <div
      id="windows-warning"
      className="mt-6 rounded-2xl border border-line bg-card p-5 sm:p-8"
    >
      <h3 className="text-lg font-medium text-ink">
        {t.download.safetyTitle}
      </h3>
      <p className="mt-2 max-w-3xl text-sm leading-6 text-ink-secondary">
        {t.download.windowsBody}
      </p>
      <p className="mt-1 max-w-3xl text-sm leading-6 text-ink-secondary">
        {t.download.browserBody}
      </p>
    </div>
  );
}

export function DownloadHero() {
  const { t, locale } = useI18n();

  return (
    <div className="mx-auto max-w-6xl px-5 pt-20 sm:px-8">
      <h1 className="text-pretty text-3xl font-medium text-ink sm:text-4xl">
        {t.download.pageTitle}
      </h1>
      <p className="mt-3 max-w-xl text-ink-secondary">
        {interpolate(t.download.pageSubtitle, {
          version: site.version,
          date: localizedMonth(locale, site.releaseMonth),
        })}
      </p>
    </div>
  );
}
