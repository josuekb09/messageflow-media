"use client";

import { useI18n } from "@/components/language-provider";

export function FeatureMatrix() {
  const { t } = useI18n();

  return (
    <section id="features" className="border-y border-line bg-page">
      <div className="mx-auto max-w-6xl px-5 py-20 sm:px-8 sm:py-24">
        <h2 className="text-3xl font-semibold tracking-tight text-ink">
          {t.features.title}
        </h2>
        <div className="mt-10 grid gap-5 md:grid-cols-3">
          {t.features.items.map((feature) => (
            <article
              key={feature.title}
              className="rounded-xl border border-line bg-white p-6 sm:p-7"
            >
              <h3 className="text-base font-semibold text-ink">{feature.title}</h3>
              <p className="mt-3 text-sm leading-6 text-ink-secondary">
                {feature.body}
              </p>
            </article>
          ))}
        </div>

        <h3
          id="library"
          className="mt-16 text-2xl font-semibold tracking-tight text-ink"
        >
          {t.library.title}
        </h3>
        <div className="mt-8 grid gap-5 md:grid-cols-3">
          {t.library.items.map((item) => (
            <article
              key={item.title}
              className="rounded-xl border border-line bg-white p-6 sm:p-7"
            >
              <h4 className="text-base font-semibold text-ink">{item.title}</h4>
              <p className="mt-3 text-sm leading-6 text-ink-secondary">
                {item.body}
              </p>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}
