"use client";

import { useI18n } from "@/components/language-provider";
import { DownloadIcon, InstallIcon, ScreenIcon, SearchIcon } from "@/components/line-icons";
import { SectionHeading } from "@/components/section-heading";
import { interpolate, localizedSize } from "@/lib/i18n";
import { site } from "@/lib/site";

// Same order as the dictionary steps: download, install, connect, search and project.
const icons = [DownloadIcon, InstallIcon, ScreenIcon, SearchIcon] as const;

export function HowItWorks() {
  const { t, locale } = useI18n();
  const sizes = {
    size: localizedSize(locale, site.installerSize),
    space: localizedSize(locale, site.requiredFreeSpace),
  };

  return (
    <section id="install" className="scroll-mt-16 border-t border-line bg-page">
      <div className="mx-auto max-w-6xl px-4 py-20 sm:px-8 sm:py-24">
        <div className="reveal">
          <SectionHeading title={t.install.title} lead={t.install.lead} />
        </div>
        <ol className="mt-12 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {t.install.steps.map((step, index) => {
            const Icon = icons[index] ?? DownloadIcon;
            return (
              <li key={step.n} className="reveal flex flex-col rounded-2xl border border-line bg-card p-6 shadow-card">
                <div className="flex items-center justify-between">
                  <Icon className="h-6 w-6 text-brand" />
                  {/* Large serif numerals, so gold on ivory stays above 3:1 contrast. */}
                  <span className="font-serif text-3xl font-medium tabular-nums text-brand" aria-hidden="true">
                    {step.n}
                  </span>
                </div>
                <h3 className="mt-6 text-[1.35rem] font-medium leading-tight text-ink">{step.title}</h3>
                <p className="mt-2 text-[15px] leading-7 text-ink-secondary">
                  {interpolate(step.body, sizes)}
                </p>
              </li>
            );
          })}
        </ol>
      </div>
    </section>
  );
}
