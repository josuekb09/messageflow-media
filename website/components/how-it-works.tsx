"use client";

import type { ReactNode } from "react";
import { useI18n } from "@/components/language-provider";

const icons = [DownloadGuide, InstallGuide, ExtendGuide, ProjectGuide] as const;

export function HowItWorks() {
  const { t } = useI18n();

  return (
    <section id="install" className="bg-white">
      <div className="mx-auto max-w-6xl px-5 py-20 sm:px-8 sm:py-24">
        <h2 className="text-3xl font-semibold tracking-tight text-ink">
          {t.install.title}
        </h2>
        <p className="mt-3 max-w-2xl text-sm leading-6 text-ink-secondary">
          {t.install.lead}
        </p>
        <ol className="mt-10 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
          {t.install.steps.map((step, index) => {
            const Icon = icons[index] ?? DownloadGuide;
            return (
              <li
                key={step.n}
                className="rounded-xl border border-line bg-page p-5"
              >
                <div className="overflow-hidden rounded-lg border border-line bg-white">
                  <Icon />
                </div>
                <p className="mt-4 text-xs font-semibold tracking-wide text-brand">
                  {step.n}
                </p>
                <p className="mt-2 text-sm font-semibold text-ink">{step.title}</p>
                <p className="mt-2 text-sm leading-6 text-ink-secondary">
                  {step.body}
                </p>
              </li>
            );
          })}
        </ol>
      </div>
    </section>
  );
}

function GuideFrame({ children }: { children: ReactNode }) {
  return (
    <svg viewBox="0 0 240 140" className="h-auto w-full" aria-hidden="true">
      <rect width="240" height="140" fill="#F8FAFC" />
      {children}
    </svg>
  );
}

function DownloadGuide() {
  return (
    <GuideFrame>
      <rect x="48" y="28" width="144" height="84" rx="10" fill="#fff" stroke="#E2E8F0" />
      <rect x="64" y="44" width="72" height="8" rx="4" fill="#E2E8F0" />
      <rect x="64" y="60" width="112" height="6" rx="3" fill="#E2E8F0" />
      <rect x="86" y="86" width="68" height="16" rx="8" fill="#2563EB" />
      <path d="M120 74v22" stroke="#fff" strokeWidth="2.5" strokeLinecap="round" />
      <path
        d="M112 88l8 8 8-8"
        fill="none"
        stroke="#fff"
        strokeWidth="2.5"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </GuideFrame>
  );
}

function InstallGuide() {
  return (
    <GuideFrame>
      <rect x="52" y="24" width="136" height="92" rx="10" fill="#fff" stroke="#E2E8F0" />
      <rect x="52" y="24" width="136" height="22" rx="10" fill="#2563EB" />
      <rect x="52" y="36" width="136" height="10" fill="#2563EB" />
      <circle cx="66" cy="35" r="3" fill="#93C5FD" />
      <rect x="68" y="58" width="88" height="6" rx="3" fill="#E2E8F0" />
      <rect x="68" y="72" width="64" height="6" rx="3" fill="#E2E8F0" />
      <rect x="140" y="94" width="32" height="12" rx="6" fill="#2563EB" />
    </GuideFrame>
  );
}

function ExtendGuide() {
  return (
    <GuideFrame>
      <rect x="28" y="36" width="88" height="58" rx="8" fill="#fff" stroke="#E2E8F0" />
      <rect x="36" y="44" width="72" height="34" rx="4" fill="#0F172A" />
      <rect x="124" y="28" width="88" height="74" rx="8" fill="#fff" stroke="#2563EB" />
      <rect x="132" y="36" width="72" height="50" rx="4" fill="#2563EB" />
      <rect x="58" y="100" width="28" height="4" rx="2" fill="#CBD5E1" />
      <rect x="154" y="108" width="28" height="4" rx="2" fill="#93C5FD" />
    </GuideFrame>
  );
}

function ProjectGuide() {
  return (
    <GuideFrame>
      <rect x="22" y="30" width="110" height="80" rx="10" fill="#fff" stroke="#E2E8F0" />
      <rect x="32" y="40" width="52" height="8" rx="4" fill="#2563EB" />
      <rect x="32" y="54" width="90" height="6" rx="3" fill="#E2E8F0" />
      <rect x="32" y="66" width="78" height="6" rx="3" fill="#E2E8F0" />
      <rect x="32" y="86" width="40" height="12" rx="6" fill="#2563EB" />
      <rect x="144" y="38" width="74" height="52" rx="8" fill="#0F172A" />
      <rect x="154" y="52" width="54" height="6" rx="3" fill="#F8FAFC" />
      <rect x="160" y="64" width="42" height="6" rx="3" fill="#93C5FD" />
    </GuideFrame>
  );
}
