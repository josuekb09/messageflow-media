"use client";

import Image from "next/image";
import { useI18n } from "@/components/language-provider";
import { SectionHeading } from "@/components/section-heading";
import { themeScreenshots } from "@/lib/media";

export function ProductShowcase() {
  const { t } = useI18n();
  const shots = [
    { ...themeScreenshots.light, label: t.product.lightLabel },
    { ...themeScreenshots.dark, label: t.product.darkLabel },
  ];

  return (
    <section id="product" className="scroll-mt-16 border-t border-line bg-page">
      <div className="mx-auto max-w-6xl px-4 py-20 sm:px-8 sm:py-24">
        <div className="reveal">
          <SectionHeading title={t.product.title} lead={t.product.lead} />
        </div>
        <div className="mt-12 grid gap-8 lg:grid-cols-2">
          {shots.map((shot, index) => (
            <figure key={shot.src} className="reveal">
              <div className="overflow-hidden rounded-2xl border border-line bg-card shadow-card">
                <Image
                  src={shot.src}
                  alt={shot.label}
                  width={shot.width}
                  height={shot.height}
                  sizes="(min-width: 1024px) 560px, 100vw"
                  className="h-auto w-full"
                  priority={index === 0}
                />
              </div>
              <figcaption className="mt-4 flex items-center gap-3 text-sm font-medium text-ink-secondary">
                <span aria-hidden="true" className="h-px w-6 bg-brand" />
                {shot.label}
              </figcaption>
            </figure>
          ))}
        </div>
      </div>
    </section>
  );
}
