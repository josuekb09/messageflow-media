"use client";

import { useI18n } from "@/components/language-provider";
import { SectionHeading } from "@/components/section-heading";

export function AboutSection() {
  const { t } = useI18n();

  return (
    <section id="about" className="scroll-mt-16 border-t border-line bg-page">
      <div className="reveal mx-auto max-w-6xl px-4 py-20 sm:px-8 sm:py-24">
        <SectionHeading title={t.about.title} align="center" />
        <p className="mx-auto mt-8 max-w-3xl text-pretty text-center text-[1.2rem] leading-[1.7] text-ink-secondary sm:text-[1.4rem]">
          {t.about.body}
        </p>
      </div>
    </section>
  );
}
