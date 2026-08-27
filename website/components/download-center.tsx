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
        <div className="rounded-2xl border border-line bg-white p-5 sm:flex sm:items-center sm:justify-between sm:p-10">
          <div>
            <h2 className="text-2xl font-semibold tracking-tight text-ink sm:text-3xl">
              {t.download.heading}
            </h2>
            <p className="mt-2 flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-ink-secondary">
              <span className="break-all">{site.downloadFileName}</span>
              <span className="text-line">·</span>
              <span>v{site.version}</span>
              <span className="text-line">·</span>
              <span>{site.releaseDate}</span>
              <span className="text-line">·</span>
              <span>{site.platform}</span>
            </p>
            <p className="mt-3 max-w-lg text-sm leading-6 text-ink-muted">
              {t.download.note}
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
      className="mt-6 rounded-2xl border border-line bg-white p-5 sm:p-8"
    >
      <h3 className="text-lg font-semibold tracking-tight text-ink">
        {t.download.safetyTitle}
      </h3>
      <p className="mt-2 max-w-3xl text-sm leading-6 text-ink-secondary">
        {t.download.safetyLead}
      </p>
      <p className="mt-3 max-w-3xl text-sm leading-6 text-ink-muted">
        {t.download.notMalware}
      </p>
      <div className="mt-6 grid gap-4 sm:grid-cols-2">
        <div className="rounded-xl border border-line bg-page p-4 sm:p-5">
          <p className="font-semibold text-ink">{t.download.chromeTitle}</p>
          <p className="mt-2 text-sm leading-6 text-ink-secondary">
            {t.download.chromeBody}
          </p>
        </div>
        <div className="rounded-xl border border-line bg-page p-4 sm:p-5">
          <p className="font-semibold text-ink">{t.download.windowsTitle}</p>
          <p className="mt-2 text-sm leading-6 text-ink-secondary">
            {t.download.windowsBody}
          </p>
        </div>
      </div>
      <p className="mt-6 text-[13px] font-medium text-ink">{t.download.hashLabel}</p>
      <code className="mt-2 block break-all rounded-lg border border-line bg-page px-3 py-2 font-mono text-[12px] leading-5 text-ink-secondary">
        {site.installerSha256}
      </code>
    </div>
  );
}

export function DownloadHero() {
  const { t } = useI18n();

  return (
    <div className="mx-auto max-w-6xl px-5 pt-20 sm:px-8">
      <h1 className="text-pretty text-2xl font-semibold tracking-tight text-ink sm:text-3xl">
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
