"use client";

import { useI18n } from "@/components/language-provider";

export function HowItWorks() {
  const { t } = useI18n();

  return (
    <section id="install" className="bg-white">
      <div className="mx-auto max-w-6xl px-5 py-20 sm:px-8 sm:py-24">
        <h2 className="text-3xl font-semibold tracking-tight text-ink">
          {t.install.title}
        </h2>
        <ol className="mt-10 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {t.install.steps.map((step) => (
            <li
              key={step.n}
              className="rounded-xl border border-line bg-page p-6"
            >
              <p className="text-xs font-semibold tracking-wide text-brand">
                {step.n}
              </p>
              <p className="mt-3 text-sm font-medium leading-6 text-ink">
                {step.title}
              </p>
            </li>
          ))}
        </ol>
      </div>
    </section>
  );
}
