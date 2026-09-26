"use client";

import Image from "next/image";
import { useI18n } from "@/components/language-provider";
import { themeScreenshots } from "@/lib/media";

export function ProductShowcase() {
  const { t } = useI18n();
  const shots = [
    { ...themeScreenshots.dark, label: t.product.darkLabel },
    { ...themeScreenshots.light, label: t.product.lightLabel },
  ];

  return (
    <section id="product" className="scroll-mt-16 border-y border-line bg-surface">
      <div className="mx-auto max-w-6xl px-5 py-20 sm:px-8 sm:py-24">
        <div className="max-w-2xl">
          <h2 className="text-balance text-3xl font-medium text-ink sm:text-4xl">
            {t.product.title}
          </h2>
          <p className="mt-4 text-pretty text-[15px] leading-7 text-ink-secondary sm:text-base">
            {t.product.lead}
          </p>
        </div>
        <div className="mt-12 grid gap-6 lg:grid-cols-2">
          {shots.map((shot, index) => (
            <figure key={shot.src}>
              <div className="overflow-hidden rounded-xl border border-line bg-page shadow-[0_20px_50px_rgba(10,10,10,0.08)]">
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
              <figcaption className="mt-3 text-sm text-ink-muted">{shot.label}</figcaption>
            </figure>
          ))}
        </div>
      </div>
    </section>
  );
}
