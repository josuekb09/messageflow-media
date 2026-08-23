"use client";

import type { ReactNode } from "react";
import { useI18n } from "@/components/language-provider";

const icons = [DownloadGuide, InstallGuide, ExtendGuide, ProjectGuide] as const;

type GuideLabels = {
  primary: string;
  secondary: string;
  action: string;
  badge: string;
};

type GuideProps = {
  title: string;
  labels: GuideLabels;
};

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
        <ol className="mt-10 grid gap-6 sm:grid-cols-2">
          {t.install.steps.map((step, index) => {
            const Icon = icons[index] ?? DownloadGuide;
            return (
              <li
                key={step.n}
                className="rounded-xl border border-line bg-page p-5"
              >
                <div className="overflow-hidden rounded-lg border border-line bg-white">
                  <Icon title={step.title} labels={step.labels} />
                </div>
                <p className="mt-4 text-xs font-semibold tracking-wide text-brand">
                  {step.n}
                </p>
                <p className="mt-2 text-base font-semibold text-ink">
                  {step.title}
                </p>
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

function GuideFrame({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <svg
      viewBox="0 0 320 240"
      className="h-auto w-full min-h-[200px] sm:min-h-[220px]"
      role="img"
      aria-label={title}
    >
      <rect width="320" height="240" fill="#F8FAFC" />
      <g
        fontFamily="ui-sans-serif, system-ui, Segoe UI, sans-serif"
        letterSpacing="-0.01em"
      >
        {children}
      </g>
    </svg>
  );
}

function DownloadGuide({ title, labels }: GuideProps) {
  return (
    <GuideFrame title={title}>
      <rect
        x="24"
        y="20"
        width="272"
        height="200"
        rx="14"
        fill="#fff"
        stroke="#E2E8F0"
        strokeWidth="2"
      />
      <rect x="24" y="20" width="272" height="40" rx="14" fill="#fff" />
      <rect x="24" y="44" width="272" height="16" fill="#fff" />
      <circle cx="44" cy="40" r="5" fill="#CBD5E1" />
      <circle cx="60" cy="40" r="5" fill="#CBD5E1" />
      <circle cx="76" cy="40" r="5" fill="#CBD5E1" />
      <text
        x="160"
        y="45"
        textAnchor="middle"
        fontSize="15"
        fontWeight="700"
        fill="#334155"
      >
        {labels.badge}
      </text>
      <text
        x="160"
        y="92"
        textAnchor="middle"
        fontSize="16"
        fontWeight="700"
        fill="#0F172A"
      >
        MessageFlowMedia
      </text>
      <text
        x="160"
        y="114"
        textAnchor="middle"
        fontSize="16"
        fontWeight="700"
        fill="#0F172A"
      >
        Setup.exe
      </text>
      <text
        x="160"
        y="140"
        textAnchor="middle"
        fontSize="14"
        fontWeight="600"
        fill="#334155"
      >
        {labels.secondary}
      </text>
      <rect x="70" y="160" width="180" height="40" rx="10" fill="#2563EB" />
      <text
        x="160"
        y="186"
        textAnchor="middle"
        fontSize="18"
        fontWeight="700"
        fill="#fff"
      >
        {labels.action}
      </text>
    </GuideFrame>
  );
}

function InstallGuide({ title, labels }: GuideProps) {
  return (
    <GuideFrame title={title}>
      <rect
        x="24"
        y="20"
        width="272"
        height="200"
        rx="14"
        fill="#fff"
        stroke="#E2E8F0"
        strokeWidth="2"
      />
      <rect x="24" y="20" width="272" height="44" rx="14" fill="#2563EB" />
      <rect x="24" y="48" width="272" height="16" fill="#2563EB" />
      <circle cx="44" cy="42" r="5" fill="#93C5FD" />
      <text
        x="160"
        y="48"
        textAnchor="middle"
        fontSize="16"
        fontWeight="700"
        fill="#fff"
      >
        {labels.secondary}
      </text>
      <text
        x="160"
        y="100"
        textAnchor="middle"
        fontSize="18"
        fontWeight="700"
        fill="#0F172A"
      >
        {labels.primary}
      </text>
      <text
        x="160"
        y="126"
        textAnchor="middle"
        fontSize="14"
        fontWeight="600"
        fill="#334155"
      >
        Windows 10 / 11
      </text>
      <rect x="40" y="148" width="108" height="36" rx="10" fill="#E2E8F0" />
      <text
        x="94"
        y="172"
        textAnchor="middle"
        fontSize="15"
        fontWeight="700"
        fill="#0F172A"
      >
        {labels.badge}
      </text>
      <rect x="160" y="148" width="120" height="36" rx="10" fill="#2563EB" />
      <text
        x="220"
        y="172"
        textAnchor="middle"
        fontSize="15"
        fontWeight="700"
        fill="#fff"
      >
        {labels.action}
      </text>
    </GuideFrame>
  );
}

function ExtendGuide({ title, labels }: GuideProps) {
  return (
    <GuideFrame title={title}>
      <rect
        x="20"
        y="52"
        width="118"
        height="78"
        rx="10"
        fill="#fff"
        stroke="#E2E8F0"
        strokeWidth="2"
      />
      <rect x="30" y="62" width="98" height="50" rx="6" fill="#0F172A" />
      <rect x="58" y="130" width="42" height="8" rx="2" fill="#CBD5E1" />
      <text
        x="79"
        y="164"
        textAnchor="middle"
        fontSize="16"
        fontWeight="700"
        fill="#0F172A"
      >
        {labels.primary}
      </text>
      <rect
        x="182"
        y="36"
        width="118"
        height="94"
        rx="10"
        fill="#fff"
        stroke="#2563EB"
        strokeWidth="3"
      />
      <rect x="192" y="46" width="98" height="66" rx="6" fill="#2563EB" />
      <rect x="220" y="130" width="42" height="8" rx="2" fill="#93C5FD" />
      <text
        x="241"
        y="164"
        textAnchor="middle"
        fontSize="16"
        fontWeight="700"
        fill="#0F172A"
      >
        {labels.secondary}
      </text>
      <rect x="118" y="78" width="84" height="28" rx="8" fill="#0F172A" />
      <text
        x="160"
        y="97"
        textAnchor="middle"
        fontSize="14"
        fontWeight="700"
        fill="#fff"
      >
        {labels.badge}
      </text>
      <rect x="94" y="184" width="132" height="36" rx="10" fill="#2563EB" />
      <text
        x="160"
        y="208"
        textAnchor="middle"
        fontSize="18"
        fontWeight="700"
        fill="#fff"
      >
        {labels.action}
      </text>
    </GuideFrame>
  );
}

function ProjectGuide({ title, labels }: GuideProps) {
  return (
    <GuideFrame title={title}>
      <rect
        x="16"
        y="20"
        width="148"
        height="200"
        rx="14"
        fill="#fff"
        stroke="#E2E8F0"
        strokeWidth="2"
      />
      <text
        x="90"
        y="52"
        textAnchor="middle"
        fontSize="16"
        fontWeight="700"
        fill="#0F172A"
      >
        {labels.primary}
      </text>
      <rect
        x="30"
        y="66"
        width="120"
        height="32"
        rx="8"
        fill="#F8FAFC"
        stroke="#E2E8F0"
        strokeWidth="2"
      />
      <text
        x="90"
        y="87"
        textAnchor="middle"
        fontSize="13"
        fontWeight="600"
        fill="#64748B"
      >
        {labels.primary}
      </text>
      <rect x="30" y="112" width="120" height="36" rx="10" fill="#2563EB" />
      <text
        x="90"
        y="136"
        textAnchor="middle"
        fontSize="15"
        fontWeight="700"
        fill="#fff"
      >
        {labels.action}
      </text>
      <rect x="42" y="164" width="96" height="28" rx="8" fill="#0F172A" />
      <text
        x="90"
        y="183"
        textAnchor="middle"
        fontSize="14"
        fontWeight="700"
        fill="#fff"
      >
        {labels.badge}
      </text>
      <rect
        x="176"
        y="36"
        width="128"
        height="168"
        rx="14"
        fill="#0F172A"
      />
      <text
        x="240"
        y="118"
        textAnchor="middle"
        fontSize="18"
        fontWeight="700"
        fill="#F8FAFC"
      >
        {labels.secondary}
      </text>
      <text
        x="240"
        y="144"
        textAnchor="middle"
        fontSize="13"
        fontWeight="600"
        fill="#93C5FD"
      >
        {labels.action}
      </text>
    </GuideFrame>
  );
}
