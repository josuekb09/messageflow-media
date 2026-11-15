"use client";

import Image from "next/image";
import { Cross, Music, Pause, Play, Projector, Sun, type LucideIcon } from "lucide-react";
import { useCallback, useEffect, useRef, useState, type CSSProperties, type KeyboardEvent } from "react";
import { useI18n } from "@/components/language-provider";
import { SectionHeading } from "@/components/section-heading";
import { screens, tourOrder, type ScreenKey } from "@/lib/media";

const SLIDE_MS = 6000;

const icons: Record<ScreenKey, LucideIcon> = {
  bible: Cross,
  songs: Music,
  projection: Projector,
  light: Sun,
};

/**
 * Slideshow of the app sections (Bible, Songs, Projection, Light theme)
 * with a cross-fade. It advances on its own while in view, pauses on hover, on
 * keyboard focus, when the tab is hidden, or with the pause button, and never
 * advances on its own for visitors who ask for reduced motion.
 */
export function AppTour() {
  const { t } = useI18n();
  const tour = t.product.tour;
  const [active, setActive] = useState(0);
  const [userPaused, setUserPaused] = useState(false);
  const [hovered, setHovered] = useState(false);
  const [inView, setInView] = useState(false);
  const [reducedMotion, setReducedMotion] = useState(false);
  const [pageVisible, setPageVisible] = useState(true);
  const rootRef = useRef<HTMLDivElement>(null);
  const tabRefs = useRef<(HTMLButtonElement | null)[]>([]);

  useEffect(() => {
    const query = window.matchMedia("(prefers-reduced-motion: reduce)");
    const update = () => setReducedMotion(query.matches);
    update();
    query.addEventListener("change", update);
    const onVisibility = () => setPageVisible(document.visibilityState === "visible");
    document.addEventListener("visibilitychange", onVisibility);
    const element = rootRef.current;
    const observer = element
      ? new IntersectionObserver(([entry]) => setInView(entry.isIntersecting), { threshold: 0.35 })
      : null;
    if (element && observer) observer.observe(element);
    return () => {
      query.removeEventListener("change", update);
      document.removeEventListener("visibilitychange", onVisibility);
      observer?.disconnect();
    };
  }, []);

  const running = inView && pageVisible && !hovered && !userPaused && !reducedMotion;

  useEffect(() => {
    if (!running) return;
    const timer = window.setTimeout(() => setActive((index) => (index + 1) % tourOrder.length), SLIDE_MS);
    return () => window.clearTimeout(timer);
  }, [running, active]);

  const select = useCallback((index: number, focus = false) => {
    const next = (index + tourOrder.length) % tourOrder.length;
    setActive(next);
    if (focus) tabRefs.current[next]?.focus();
  }, []);

  const onTabKey = (event: KeyboardEvent<HTMLButtonElement>) => {
    const moves: Record<string, number> = { ArrowRight: active + 1, ArrowLeft: active - 1, Home: 0, End: tourOrder.length - 1 };
    if (event.key in moves) {
      event.preventDefault();
      select(moves[event.key], true);
    }
  };

  const activeKey = tourOrder[active];

  return (
    <section id="product" className="relative scroll-mt-16 overflow-hidden border-t border-line bg-page">
      <div
        aria-hidden="true"
        className="glow-orb pointer-events-none absolute left-1/2 top-1/3 -z-0 h-[30rem] w-[56rem] max-w-[180vw] -translate-x-1/2 rounded-full opacity-70"
      />
      <div className="relative mx-auto max-w-6xl px-4 py-20 sm:px-8 sm:py-24">
        <div className="reveal">
          <SectionHeading title={t.product.title} lead={t.product.lead} align="center" />
        </div>

        <div
          ref={rootRef}
          className="reveal mt-12"
          style={{ "--reveal-delay": "120ms" } as CSSProperties}
          onMouseEnter={() => setHovered(true)}
          onMouseLeave={() => setHovered(false)}
          onFocusCapture={() => setHovered(true)}
          onBlurCapture={(event) => {
            if (!event.currentTarget.contains(event.relatedTarget as Node | null)) setHovered(false);
          }}
        >
          <div className="flex flex-wrap items-center justify-center gap-2">
            <div
              role="tablist"
              aria-label={tour.label}
              className="flex max-w-full gap-1 overflow-x-auto rounded-2xl border border-line bg-card p-1 shadow-card [scrollbar-width:none]"
            >
              {tourOrder.map((key, index) => {
                const Icon = icons[key];
                const selected = index === active;
                return (
                  <button
                    key={key}
                    ref={(node) => {
                      tabRefs.current[index] = node;
                    }}
                    type="button"
                    role="tab"
                    id={`tour-tab-${key}`}
                    aria-selected={selected}
                    aria-controls="tour-panel"
                    tabIndex={selected ? 0 : -1}
                    onClick={() => select(index)}
                    onKeyDown={onTabKey}
                    className={`relative flex shrink-0 items-center gap-2 overflow-hidden rounded-xl px-3 py-2 text-[13px] font-medium transition-colors duration-300 sm:px-3.5 sm:text-sm ${
                      selected ? "bg-accent-soft text-accent-text" : "text-ink-muted hover:bg-surface hover:text-ink"
                    }`}
                  >
                    <Icon aria-hidden="true" className="h-4 w-4" />
                    {/* On phones only the selected tab shows its name, so the row fits. */}
                    <span className={selected ? "" : "sr-only sm:not-sr-only"}>{tour.slides[key].tab}</span>
                    {selected ? (
                      <span
                        key={`${active}-${running}`}
                        aria-hidden="true"
                        data-running={running ? "" : undefined}
                        style={{ "--tour-ms": `${SLIDE_MS}ms` } as CSSProperties}
                        className="tour-progress absolute inset-x-2 bottom-0.5 h-0.5 origin-left scale-x-0 rounded-full bg-accent"
                      />
                    ) : null}
                  </button>
                );
              })}
            </div>
            {reducedMotion ? null : (
              <button
                type="button"
                onClick={() => setUserPaused((paused) => !paused)}
                aria-label={userPaused ? tour.play : tour.pause}
                title={userPaused ? tour.play : tour.pause}
                className="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-line bg-card text-ink-muted shadow-card transition-colors hover:border-accent hover:text-accent-text"
              >
                {userPaused ? <Play aria-hidden="true" className="h-4 w-4" /> : <Pause aria-hidden="true" className="h-4 w-4" />}
              </button>
            )}
          </div>

          <div
            id="tour-panel"
            role="tabpanel"
            aria-labelledby={`tour-tab-${activeKey}`}
            className="mx-auto mt-8 max-w-[1000px]"
          >
            <div className="device">
              {/* All slides share one box (sized by the tallest, 1456 x 764) and cross-fade. */}
              <div className="device-screen relative aspect-[1456/764]">
                {tourOrder.map((key, index) => {
                  const shot = screens[key];
                  const shown = index === active;
                  return (
                    <Image
                      key={key}
                      src={shot.src}
                      alt={shown ? tour.slides[key].alt : ""}
                      aria-hidden={shown ? undefined : true}
                      width={shot.width}
                      height={shot.height}
                      sizes="(min-width: 1064px) 988px, calc(100vw - 44px)"
                      quality={85}
                      loading="lazy"
                      className={`tour-slide absolute inset-0 h-full w-full object-contain ${
                        shown ? "scale-100 opacity-100" : "scale-[1.015] opacity-0"
                      }`}
                    />
                  );
                })}
              </div>
            </div>
            <p aria-live={running ? "off" : "polite"} className="mx-auto mt-5 max-w-2xl text-pretty text-center text-[15px] leading-7 text-ink-secondary">
              {tour.slides[activeKey].caption}
            </p>
          </div>
        </div>
      </div>
    </section>
  );
}
