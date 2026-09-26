"use client";

import { useI18n } from "@/components/language-provider";
import { interpolate, localizedSize } from "@/lib/i18n";
import { site } from "@/lib/site";

export function HowItWorks() {
  const { t, locale } = useI18n();
  const size = localizedSize(locale, site.installerSize);

  return (
    <section id="install" className="scroll-mt-16 border-t border-line bg-surface">
      <div className="mx-auto max-w-6xl px-5 py-20 sm:px-8 sm:py-24">
        <div className="max-w-2xl">
          <h2 className="text-balance text-3xl font-medium text-ink sm:text-4xl">
            {t.install.title}
          </h2>
          <p className="mt-4 text-[15px] leading-7 text-ink-secondary sm:text-base">
            {t.install.lead}
          </p>
        </div>
        <ol className="mt-12 grid gap-x-8 gap-y-10 sm:grid-cols-2 lg:grid-cols-4">
          {t.install.steps.map((step) => (
            <li key={step.n} className="border-t border-ink/15 pt-6">
              {/* Large serif numerals, so gold on white stays above 3:1 contrast. */}
              <p className="font-serif text-4xl font-medium tabular-nums text-brand" aria-hidden="true">
                {step.n}
              </p>
              <h3 className="mt-4 text-lg font-medium text-ink">{step.title}</h3>
              <p className="mt-2 text-[15px] leading-7 text-ink-secondary">
                {interpolate(step.body, { size })}
              </p>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
