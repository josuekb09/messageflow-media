"use client";

import { useI18n } from "@/components/language-provider";
import { BookIcon, CrossIcon, MusicNoteIcon } from "@/components/line-icons";

// Same order as the dictionary items: sermons, Bible, songs.
const icons = [BookIcon, CrossIcon, MusicNoteIcon] as const;

export function FeatureMatrix() {
  const { t } = useI18n();

  return (
    <section id="features" className="scroll-mt-16 bg-page">
      <div className="mx-auto max-w-6xl px-5 py-20 sm:px-8 sm:py-24">
        <div className="max-w-2xl">
          <h2 className="text-balance text-3xl font-medium text-ink sm:text-4xl">
            {t.features.title}
          </h2>
          <p className="mt-4 text-[15px] leading-7 text-ink-secondary sm:text-base">
            {t.features.lead}
          </p>
        </div>
        <div className="mt-12 grid gap-5 md:grid-cols-3">
          {t.features.items.map((feature, index) => {
            const Icon = icons[index] ?? BookIcon;
            return (
              <article
                key={feature.title}
                className="flex flex-col rounded-xl border border-line bg-page p-6 sm:p-7"
              >
                <Icon className="h-7 w-7 text-ink" />
                <h3 className="mt-6 text-xl font-medium text-ink">{feature.title}</h3>
                <p className="mt-3 flex-1 text-[15px] leading-7 text-ink-secondary">{feature.body}</p>
                <p className="mt-6 border-t border-line pt-4 text-sm leading-6 text-ink-muted">
                  {feature.detail}
                </p>
              </article>
            );
          })}
        </div>
      </div>
    </section>
  );
}
