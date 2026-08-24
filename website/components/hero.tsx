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
      <div className="mx-auto max-w-6xl px-5 pb-16 pt-16 sm:px-8 sm:pb-24 sm:pt-24">
        <div className="mx-auto max-w-3xl text-center">
          <p className="inline-flex max-w-full text-pretty rounded-full border border-line bg-page px-3 py-1 text-left text-[13px] font-medium text-ink-secondary">
            {interpolate(t.hero.eyebrow, {
              version: site.version,
              date: site.releaseDate,
              platform: site.platform,
            })}
          </p>
          <h1 className="mt-6 text-pretty text-[1.75rem] font-semibold leading-tight tracking-tight text-ink sm:text-4xl sm:leading-[1.12] md:text-[3.25rem]">
            {t.hero.title}
          </h1>
          <p className="mx-auto mt-5 max-w-2xl text-pretty text-base leading-7 text-ink-secondary sm:text-lg sm:leading-8">
            {t.hero.subtitle}
          </p>
          <div className="mt-9 flex flex-col items-stretch gap-3 sm:flex-row sm:flex-wrap sm:items-center sm:justify-center">
            <DownloadButton size="lg" className="w-full sm:w-auto" />
            <a
              href="#product"
              className="inline-flex h-11 w-full items-center justify-center rounded-lg border border-line px-5 text-[15px] font-medium text-ink-secondary transition-colors hover:bg-page hover:text-ink sm:w-auto"
            >
              {t.hero.secondaryCta}
            </a>
          </div>
        </div>
        <div className="mx-auto mt-16 max-w-5xl">
          <figure className="overflow-hidden rounded-2xl border border-line bg-white shadow-[0_24px_64px_rgba(10,10,10,0.08)]">
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
