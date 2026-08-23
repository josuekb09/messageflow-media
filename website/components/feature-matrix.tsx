"use client";

import { useI18n } from "@/components/language-provider";

export function FeatureMatrix() {
  const { t } = useI18n();

  return (
    <section id="features" className="border-y border-line bg-page">
      <div className="mx-auto max-w-6xl px-5 py-20 sm:px-8 sm:py-24">
        <h2 className="text-3xl font-semibold tracking-tight text-ink sm:text-4xl">
          {t.features.title}
        </h2>
        <div className="mt-10 grid gap-4 sm:grid-cols-2 lg:grid-cols-4 lg:grid-rows-2">
          {t.features.items.map((feature, index) => (
            <article
              key={feature.title}
              className={`rounded-2xl border border-line bg-white p-6 sm:p-8 ${
                index === 0
                  ? "sm:col-span-2 lg:col-span-2 lg:row-span-2"
                  : "lg:col-span-2"
              }`}
            >
              <h3
                className={`font-semibold tracking-tight text-ink ${
                  index === 0 ? "text-xl sm:text-2xl" : "text-base sm:text-lg"
                }`}
              >
                {feature.title}
              </h3>
              <p
                className={`mt-3 leading-7 text-ink-secondary ${
                  index === 0 ? "max-w-md text-[15px]" : "text-sm"
                }`}
              >
                {feature.body}
              </p>
            </article>
          ))}
        </div>

        <h3
          id="library"
          className="mt-20 text-2xl font-semibold tracking-tight text-ink sm:text-3xl"
        >
          {t.library.title}
        </h3>
        <div className="mt-8 grid gap-4 md:grid-cols-3">
          {t.library.items.map((item) => (
            <article
              key={item.title}
              className="rounded-2xl border border-line bg-white p-6 sm:p-7"
            >
              <h4 className="text-lg font-semibold tracking-tight text-ink">
                {item.title}
              </h4>
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
