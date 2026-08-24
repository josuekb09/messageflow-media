"use client";

import { useEffect, useState } from "react";
import Image from "next/image";
import { useI18n } from "@/components/language-provider";
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
    <section id="product" className="bg-white">
      <div className="mx-auto max-w-6xl px-5 py-20 sm:px-8 sm:py-24">
        <h2 className="text-pretty text-2xl font-semibold tracking-tight text-ink sm:text-3xl md:text-4xl">
          {t.product.title}
        </h2>

        <div className="mt-10">
          <h3 className="text-lg font-semibold tracking-tight text-ink">{t.product.videoTitle}</h3>
          <div className="mt-4 overflow-hidden rounded-2xl border border-line bg-ink shadow-[0_24px_64px_rgba(10,10,10,0.08)]">
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
          <h3 className="text-lg font-semibold text-ink">
            {t.product.screenshotsTitle}
          </h3>
          <div
            role="tablist"
            aria-label={t.product.screenshotsTitle}
            className="mt-4 flex w-full flex-wrap rounded-lg border border-line bg-page p-0.5 text-[13px] sm:inline-flex sm:w-auto"
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
                  className={`min-h-9 flex-1 rounded-md px-3 py-1.5 font-medium sm:flex-none ${
                    selected
                      ? "bg-white text-ink shadow-sm"
                      : "text-ink-muted hover:text-ink"
                  }`}
                >
                  {label}
                </button>
              );
            })}
          </div>
          <figure className="mt-5 overflow-hidden rounded-2xl border border-line bg-white shadow-[0_16px_40px_rgba(10,10,10,0.08)]">
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
                  className={`overflow-hidden rounded-lg border ${
                    id === activeId
                      ? "border-brand ring-1 ring-brand"
                      : "border-line hover:border-ink-muted"
                  }`}
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
      </div>
    </section>
  );
}
