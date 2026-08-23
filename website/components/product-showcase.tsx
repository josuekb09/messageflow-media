"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { useI18n } from "@/components/language-provider";
import { Container, SectionLead, SectionTitle } from "@/components/ui";
import { cn } from "@/lib/cn";
import { type Locale } from "@/lib/i18n";
import { interfaceScreenshots } from "@/lib/media";

export function ProductShowcase() {
  const { t, locale } = useI18n();
  const [activeId, setActiveId] = useState<Locale>(locale);

  useEffect(() => {
    setActiveId(locale);
  }, [locale]);

  const active = interfaceScreenshots[activeId];
  const shots: Locale[] = ["en", "fr", "sw"];

  return (
    <section id="product" className="relative scroll-mt-20 overflow-hidden bg-slate-950 py-20 text-white sm:py-24">
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(ellipse_at_top,rgba(99,102,241,0.22),transparent_58%)]" />
      <Container className="relative">
        <SectionTitle className="text-white">{t.product.title}</SectionTitle>
        <SectionLead className="text-slate-300">{t.product.lead}</SectionLead>

        <div className="mt-12">
          <h3 className="text-sm font-semibold uppercase tracking-[0.16em] text-indigo-200">
            {t.product.videoTitle}
          </h3>
          <div className="mt-4 overflow-hidden rounded-2xl border border-white/10 bg-black shadow-[0_30px_80px_-30px_rgba(0,0,0,0.8)]">
            <video
              className="aspect-video h-auto w-full"
              controls
              playsInline
              preload="metadata"
              poster={interfaceScreenshots.en.src}
            >
              <source src="/media/demo.mp4" type="video/mp4" />
            </video>
          </div>
        </div>

        <div className="mt-16">
          <h3 className="text-sm font-semibold uppercase tracking-[0.16em] text-indigo-200">
            {t.product.screenshotsTitle}
          </h3>
          <div
            role="tablist"
            aria-label={t.product.screenshotsTitle}
            className="mt-4 inline-flex rounded-full border border-white/10 bg-white/5 p-1 text-[13px]"
          >
            {shots.map((id) => {
              const selected = id === activeId;
              const label =
                id === "en"
                  ? t.product.englishUi
                  : id === "fr"
                    ? t.product.frenchUi
                    : t.product.swahiliUi;
              return (
                <button
                  key={id}
                  type="button"
                  role="tab"
                  aria-selected={selected}
                  onClick={() => setActiveId(id)}
                  className={cn(
                    "rounded-full px-3.5 py-1.5 font-medium transition-all duration-200",
                    selected
                      ? "bg-white text-slate-950 shadow-sm"
                      : "text-slate-300 hover:text-white",
                  )}
                >
                  {label}
                </button>
              );
            })}
          </div>
          <figure className="mt-5 overflow-hidden rounded-2xl border border-white/10 bg-slate-900 shadow-[0_24px_60px_-24px_rgba(0,0,0,0.8)]">
            <Image
              src={active.src}
              alt={
                activeId === "en"
                  ? t.product.englishUi
                  : activeId === "fr"
                    ? t.product.frenchUi
                    : t.product.swahiliUi
              }
              width={active.width}
              height={active.height}
              className="h-auto w-full"
            />
          </figure>
          <div className="mt-4 grid grid-cols-3 gap-3">
            {shots.map((id) => {
              const shot = interfaceScreenshots[id];
              const label =
                id === "en"
                  ? t.product.englishUi
                  : id === "fr"
                    ? t.product.frenchUi
                    : t.product.swahiliUi;
              return (
                <button
                  key={id}
                  type="button"
                  onClick={() => setActiveId(id)}
                  className={cn(
                    "overflow-hidden rounded-xl border transition-all duration-200",
                    id === activeId
                      ? "border-indigo-400 ring-2 ring-indigo-400/40"
                      : "border-white/10 hover:border-white/30",
                  )}
                >
                  <Image
                    src={shot.src}
                    alt={label}
                    width={shot.width}
                    height={shot.height}
                    className="h-auto w-full"
                  />
                </button>
              );
            })}
          </div>
        </div>
      </Container>
    </section>
  );
}
