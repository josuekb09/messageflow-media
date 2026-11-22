"use client";

import Image from "next/image";
import { ArrowRight, ShieldCheck } from "lucide-react";
import type { CSSProperties } from "react";
import { DownloadButton } from "@/components/download-button";
import { useI18n } from "@/components/language-provider";
import { interpolate, localizedSize } from "@/lib/i18n";
import { screens } from "@/lib/media";
import { site } from "@/lib/site";

/** Order of each hero line in the entrance animation. */
const order = (i: number) => ({ "--i": i }) as CSSProperties;

export function Hero() {
  const { t, locale } = useI18n();
  const shot = screens.bible;

  return (
    <section className="relative isolate overflow-hidden bg-page">
      <div className="mx-auto grid max-w-6xl items-center gap-14 px-4 pb-20 pt-14 sm:px-8 sm:pb-24 sm:pt-20 lg:grid-cols-[1.02fr_1.18fr] lg:gap-12 lg:pb-28 lg:pt-24">
        <div className="text-center lg:text-left">
          <p
            style={order(0)}
            className="hero-in inline-flex items-center gap-2 rounded-full border border-line bg-card px-3.5 py-1.5 text-[12px] font-medium text-ink-secondary sm:text-[13px]"
          >
            <ShieldCheck aria-hidden="true" className="h-4 w-4 text-accent-text" />
            {t.hero.eyebrow}
          </p>
          <h1
            style={order(1)}
            className="hero-in mt-6 text-balance text-[2.25rem] font-semibold leading-[1.1] text-ink sm:text-[2.9rem] lg:text-[3.1rem]"
          >
            {t.hero.title}
          </h1>
          <p
            style={order(2)}
            className="hero-in mx-auto mt-6 max-w-xl text-pretty text-[17px] leading-8 text-ink-secondary lg:mx-0"
          >
            {t.hero.subtitle}
          </p>
          <div
            style={order(3)}
            className="hero-in mt-9 flex flex-col items-stretch gap-3 sm:flex-row sm:items-start sm:justify-center lg:justify-start"
          >
            <div className="flex flex-col items-stretch sm:items-center lg:items-start">
              <DownloadButton size="lg" className="w-full sm:w-auto" />
              <p className="mt-2.5 text-[13px] text-ink-muted">
                {interpolate(t.hero.downloadMeta, {
                  version: site.version,
                  size: localizedSize(locale, site.installerSize),
                })}
              </p>
              <p className="mt-1 max-w-xs text-pretty text-[13px] leading-5 text-ink-secondary sm:text-center lg:text-left">
                {interpolate(t.hero.whatsNew, { version: site.version })}
              </p>
            </div>
            <a
              href="#product"
              className="group inline-flex h-12 w-full items-center justify-center gap-2 whitespace-nowrap rounded-xl border border-line bg-card px-6 text-[15px] font-medium text-ink transition-colors hover:border-accent hover:text-accent-text sm:w-auto"
            >
              {t.hero.secondaryCta}
              <ArrowRight
                aria-hidden="true"
                className="h-4 w-4 transition-transform duration-300 group-hover:translate-x-1 motion-reduce:transition-none motion-reduce:group-hover:translate-x-0"
              />
            </a>
          </div>
        </div>

        <div className="frame-in relative mx-auto w-full max-w-[640px] lg:max-w-none">
          <div className="device">
              <div className="device-screen">
                <Image
                  src={shot.src}
                  alt={t.product.tour.slides.bible.alt}
                  width={shot.width}
                  height={shot.height}
                  sizes="(min-width: 1152px) 600px, (min-width: 1024px) 52vw, (min-width: 640px) 640px, 100vw"
                  quality={85}
                  loading="eager"
                  fetchPriority="high"
                  className="block h-auto w-full"
                />
              </div>
          </div>
        </div>
      </div>
    </section>
  );
}
