"use client";

import Image from "next/image";
import { Play } from "lucide-react";
import { DownloadButton } from "@/components/download-button";
import { useI18n } from "@/components/language-provider";
import { Container } from "@/components/ui";
import { interpolate } from "@/lib/i18n";
import { interfaceScreenshots } from "@/lib/media";
import { site } from "@/lib/site";

export function Hero() {
  const { t, locale } = useI18n();
  const shot = interfaceScreenshots[locale];

  return (
    <section className="mesh-hero relative overflow-hidden">
      <Container className="relative pb-16 pt-16 sm:pb-24 sm:pt-20">
        <div className="mx-auto max-w-3xl text-center">
          <p className="animate-fade-up inline-flex items-center gap-2 rounded-full border border-indigo-200/70 bg-white/80 px-3 py-1 text-[13px] font-medium text-slate-700 shadow-sm backdrop-blur">
            <span className="relative flex h-2 w-2">
              <span className="absolute inline-flex h-full w-full rounded-full bg-emerald-400 opacity-70 animate-pulse-dot" />
              <span className="relative inline-flex h-2 w-2 rounded-full bg-emerald-500" />
            </span>
            {interpolate(t.hero.badge, { version: site.version })}
          </p>
          <h1 className="animate-fade-up mt-6 text-4xl font-semibold tracking-tight text-ink sm:text-6xl sm:leading-[1.08]">
            {t.hero.title}
          </h1>
          <p className="animate-fade-up mx-auto mt-5 max-w-2xl text-lg leading-8 text-slate-600">
            {t.hero.subtitle}
          </p>
          <div className="animate-fade-up mt-9 flex flex-wrap items-center justify-center gap-3">
            <DownloadButton size="lg" />
            <a
              href="#product"
              className="inline-flex h-12 items-center gap-2 rounded-xl border border-slate-200/80 bg-white/80 px-5 text-[15px] font-semibold text-slate-700 shadow-sm backdrop-blur transition-all duration-200 hover:-translate-y-0.5 hover:border-slate-300 hover:text-ink"
            >
              <Play className="h-4 w-4" />
              {t.hero.secondaryCta}
            </a>
          </div>
        </div>

        <div className="relative mx-auto mt-16 max-w-5xl">
          <div className="hero-glow pointer-events-none absolute -inset-10 sm:-inset-16" />
          <figure className="relative overflow-hidden rounded-2xl border border-white/70 bg-slate-950 shadow-[0_40px_80px_-24px_rgba(15,23,42,0.45)] ring-1 ring-slate-900/10">
            <div className="flex h-10 items-center gap-2 border-b border-white/10 bg-[#0f172a] px-4">
              <span className="h-2.5 w-2.5 rounded-full bg-white/15" />
              <span className="h-2.5 w-2.5 rounded-full bg-white/15" />
              <span className="h-2.5 w-2.5 rounded-full bg-white/15" />
              <p className="ml-2 text-[12px] font-medium text-slate-400">{site.name}</p>
            </div>
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
      </Container>
    </section>
  );
}
