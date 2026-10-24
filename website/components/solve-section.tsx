"use client";

import { useI18n } from "@/components/language-provider";
import { FaithfulIcon, LaptopIcon, StopwatchIcon } from "@/components/line-icons";
import { SectionHeading } from "@/components/section-heading";

// Same order as the dictionary items: faithful, ready, free and offline.
const icons = [FaithfulIcon, StopwatchIcon, LaptopIcon] as const;

export function SolveSection() {
  const { t } = useI18n();

  return (
    <section id="solve" className="scroll-mt-16 border-t border-line bg-surface">
      <div className="mx-auto max-w-6xl px-4 py-20 sm:px-8 sm:py-24">
        <div className="reveal">
          <SectionHeading title={t.solve.title} />
        </div>
        <div className="mt-12 grid gap-5 md:grid-cols-3">
          {t.solve.items.map((item, index) => {
            const Icon = icons[index] ?? FaithfulIcon;
            return (
              <article
                key={item.title}
                className="reveal flex flex-col rounded-2xl border border-line bg-card p-6 shadow-card sm:p-8"
              >
                <span className="inline-flex h-12 w-12 items-center justify-center rounded-full border border-brand/40 text-brand">
                  <Icon className="h-6 w-6" />
                </span>
                <h3 className="mt-6 text-[1.55rem] font-medium leading-tight text-ink">{item.title}</h3>
                <p className="mt-3 flex-1 text-[15px] leading-7 text-ink-secondary">{item.body}</p>
              </article>
            );
          })}
        </div>
      </div>
    </section>
  );
}
