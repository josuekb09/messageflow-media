import { BrandLogo } from "@/components/brand-logo";

function WindowsCaptionButtons() {
  return (
    <div className="ml-auto flex h-10 text-ink-muted">
      <span className="flex w-11 items-center justify-center" aria-hidden>
        <svg width="10" height="1" viewBox="0 0 10 1">
          <rect width="10" height="1" fill="currentColor" />
        </svg>
      </span>
      <span className="flex w-11 items-center justify-center" aria-hidden>
        <svg width="10" height="10" viewBox="0 0 10 10" fill="none">
          <rect x="0.5" y="0.5" width="9" height="9" stroke="currentColor" />
        </svg>
      </span>
      <span className="flex w-11 items-center justify-center" aria-hidden>
        <svg width="10" height="10" viewBox="0 0 10 10">
          <path d="M1 1 L9 9 M9 1 L1 9" stroke="currentColor" strokeWidth="1.2" />
        </svg>
      </span>
    </div>
  );
}

export function ProductWindow() {
  return (
    <figure className="w-full">
      <div className="overflow-hidden rounded-lg border border-line shadow-[0_16px_40px_rgba(15,23,42,0.08)]">
        <div className="flex h-10 items-center border-b border-line bg-[#f3f3f3]">
          <div className="flex items-center gap-2 pl-3">
            <BrandLogo variant="icon" className="h-4 w-4" />
            <p className="text-[12px] text-ink-secondary">MessageFlow</p>
          </div>
          <WindowsCaptionButtons />
        </div>

        <div className="bg-app text-app-text">
          <div className="flex items-center gap-2.5 border-b border-app-line px-4 py-3">
            <BrandLogo className="h-7 w-7" />
            <p className="text-[17px] font-semibold">MessageFlow</p>
            <div className="ml-auto hidden items-center gap-2 sm:flex">
              <div className="h-8 w-52 rounded border border-app-line bg-[#1e2633] px-3 text-[12px] leading-8 text-app-muted">
                Genesis 1:1
              </div>
              <span className="rounded bg-brand px-3 py-1.5 text-[12px] font-medium text-white">
                Project
              </span>
            </div>
          </div>

          <div className="flex gap-6 border-b border-app-line px-4 text-[13px]">
            <span className="border-b-2 border-app-sky py-2.5 text-app-sky">Bible</span>
            <span className="py-2.5 text-app-muted">Sermons</span>
            <span className="py-2.5 text-app-muted">Songs</span>
          </div>

          <div className="grid lg:grid-cols-2">
            <div className="space-y-2 p-3">
              <div className="rounded border border-[#38bdf8]/35 bg-[#123a55] p-3">
                <p className="text-[11px] font-medium text-app-sky">Genesis 1:1 · KJV</p>
                <p className="mt-1 text-[13px] leading-5 text-slate-200">
                  In the beginning God created the heaven and the earth.
                </p>
              </div>
              <div className="rounded border border-app-line bg-app-card p-3">
                <p className="text-[11px] font-medium text-slate-300">Genèse 1:1 · LSG</p>
                <p className="mt-1 text-[13px] leading-5 text-app-muted">
                  Au commencement, Dieu créa les cieux et la terre.
                </p>
              </div>
              <div className="rounded border border-app-line bg-app-card p-3">
                <p className="text-[11px] font-medium text-slate-300">
                  Mwanzo 1:1 · Biblia Takatifu
                </p>
                <p className="mt-1 text-[13px] leading-5 text-app-muted">
                  Hapo mwanzo Mungu aliumba mbingu na nchi.
                </p>
              </div>
            </div>
            <div className="flex min-h-[220px] flex-col border-t border-app-line bg-app-panel p-6 lg:border-l lg:border-t-0">
              <p className="text-[10px] uppercase tracking-[0.14em] text-app-muted">
                Projection
              </p>
              <p className="mt-5 text-[22px] font-semibold leading-snug">
                In the beginning God created the heaven and the earth.
              </p>
              <p className="mt-auto pt-8 text-sm text-app-sky">
                Genesis 1:1 · King James Version
              </p>
            </div>
          </div>
        </div>
      </div>
      <figcaption className="sr-only">
        MessageFlow Windows application showing Bible search and the projection pane.
      </figcaption>
    </figure>
  );
}
