"use client";

import Image from "next/image";
import { DownloadButton } from "@/components/download-button";
import { useI18n } from "@/components/language-provider";
import { interpolate } from "@/lib/i18n";
import { interfaceScreenshots } from "@/lib/media";
import { site } from "@/lib/site";

export function Hero() {
  const { t, locale } = useI18n();
  const shot = interfaceScreenshots[locale];

  return (
    <section className="bg-white">
      <div className="mx-auto max-w-6xl px-5 pb-16 pt-16 sm:px-8 sm:pb-20 sm:pt-20">
        <div className="mx-auto max-w-3xl text-center">
          <p className="text-sm font-medium text-brand">
            {interpolate(t.hero.eyebrow, {
              version: site.version,
              date: site.releaseDate,
              platform: site.platform,
            })}
          </p>
          <h1 className="mt-5 text-4xl font-semibold tracking-tight text-ink sm:text-5xl sm:leading-[1.15]">
            {t.hero.title}
          </h1>
          <p className="mx-auto mt-5 max-w-2xl text-lg leading-7 text-ink-secondary">
            {t.hero.subtitle}
          </p>
          <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
            <DownloadButton size="lg" />
            <a
              href="#install"
              className="inline-flex h-11 items-center rounded-lg border border-line px-5 text-[15px] font-medium text-ink-secondary hover:bg-page hover:text-ink"
            >
              {t.hero.secondaryCta}
            </a>
          </div>
        </div>
        <div className="mx-auto mt-14 max-w-5xl">
          <figure className="overflow-hidden rounded-xl border border-line bg-white shadow-[0_24px_48px_rgba(15,23,42,0.08)]">
            <Image
              src={shot.src}
              alt={t.hero.title}
              width={shot.width}
              height={shot.height}
              className="h-auto w-full"
              priority
            />
          </figure>
        </div>
      </div>
    </section>
  );
}
