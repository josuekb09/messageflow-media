"use client";

import { BookCheck, Timer, WifiOff } from "lucide-react";
import { IconBadge } from "@/components/icon-badge";
import { useI18n } from "@/components/language-provider";
import { revealDelay } from "@/components/reveal-delay";
import { SectionHeading } from "@/components/section-heading";

// Same order as the dictionary items: faithful, ready, free and offline.
const icons = [BookCheck, Timer, WifiOff] as const;

export function SolveSection() {
  const { t } = useI18n();

  return (
    <section id="solve" className="scroll-mt-16 border-t border-line bg-surface">
      <div className="mx-auto max-w-6xl px-4 py-20 sm:px-8 sm:py-24">
        <div className="reveal">
          <SectionHeading title={t.solve.title} />
        </div>
        <div className="mt-12 grid gap-5 md:grid-cols-3">
          {t.solve.items.map((item, index) => (
            <article
              key={item.title}
              style={revealDelay(index)}
              className="reveal card-lift flex flex-col rounded-2xl border border-line bg-card p-6 shadow-card sm:p-8"
            >
              <IconBadge icon={icons[index] ?? BookCheck} />
              <h3 className="mt-6 text-[1.3rem] font-semibold leading-tight text-ink">{item.title}</h3>
              <p className="mt-3 flex-1 text-[15px] leading-7 text-ink-secondary">{item.body}</p>
            </article>
          ))}
        </div>
      </div>
    </section>
  );
}
