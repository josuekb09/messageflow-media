"use client";

import type { ReactNode } from "react";
import Link from "next/link";
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

const textStyle = {
  fontFamily: "ui-sans-serif, system-ui, Segoe UI, sans-serif",
} as const;

export function HowItWorks() {
  const { t } = useI18n();

  return (
    <section id="install" className="scroll-mt-20 bg-white py-20 sm:py-24">
      <div className="mx-auto max-w-6xl px-5 sm:px-8">
        <h2 className="text-3xl font-semibold tracking-tight text-ink sm:text-[2.5rem] sm:leading-tight">
          {t.install.title}
        </h2>
        <p className="mt-4 max-w-2xl text-base leading-7 text-slate-600">
          {t.install.lead}
        </p>
        <ol className="mt-12 grid gap-5 sm:grid-cols-2">
          {t.install.steps.map((step, index) => {
            const Icon = icons[index] ?? DownloadGuide;
            return (
              <li
                key={step.n}
                className="rounded-3xl border border-slate-200/80 bg-slate-50 p-5 shadow-sm transition-all duration-200 hover:-translate-y-0.5 hover:shadow-lg hover:shadow-indigo-500/5 sm:p-6"
              >
                <div className="overflow-hidden rounded-2xl border border-slate-200/80 bg-white">
                  <Icon title={step.title} labels={step.labels} />
                </div>
                <p className="mt-5 text-xs font-semibold uppercase tracking-[0.18em] text-indigo-600">
                  {step.n}
                </p>
                <p className="mt-2 text-lg font-semibold tracking-tight text-ink">
                  {step.title}
                </p>
                <p className="mt-2 text-sm leading-6 text-slate-600">
                  {step.body}
                </p>
              </li>
            );
          })}
        </ol>
        <aside className="mt-10 rounded-3xl border border-indigo-100 bg-gradient-to-r from-indigo-50 to-blue-50 p-6 sm:flex sm:items-center sm:justify-between sm:gap-8">
          <div>
            <p className="text-base font-semibold tracking-tight text-ink">
              {t.feedback.calloutTitle}
            </p>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-slate-600">
              {t.feedback.calloutBody}
            </p>
          </div>
          <Link
            href="/feedback"
            className="mt-4 inline-flex h-11 shrink-0 items-center rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 px-5 text-[15px] font-semibold text-white shadow-lg shadow-indigo-500/20 transition-all duration-200 hover:-translate-y-0.5 sm:mt-0"
          >
            {t.feedback.calloutCta}
          </Link>
        </aside>
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
      viewBox="0 0 480 300"
      width="100%"
      height="300"
      className="h-[300px] w-full"
      role="img"
      aria-label={title}
      data-guide="labeled-v3"
      xmlns="http://www.w3.org/2000/svg"
    >
      <rect width="480" height="300" fill="#F8FAFC" />
      {children}
    </svg>
  );
}

function Word({
  x,
  y,
  size,
  fill,
  children,
  anchor = "middle",
}: {
  x: number;
  y: number;
  size: number;
  fill: string;
  children: ReactNode;
  anchor?: "start" | "middle" | "end";
}) {
  return (
    <text
      x={x}
      y={y}
      textAnchor={anchor}
      fontSize={size}
      fontWeight={800}
      fill={fill}
      style={{ ...textStyle, fontSize: size, fontWeight: 800, fill }}
    >
      {children}
    </text>
  );
}

function DownloadGuide({ title, labels }: GuideProps) {
  return (
    <GuideFrame title={title}>
      <rect
        x="28"
        y="22"
        width="424"
        height="256"
        rx="16"
        fill="#fff"
        stroke="#E2E8F0"
        strokeWidth="2"
      />
      <rect x="28" y="22" width="424" height="52" rx="16" fill="#2563EB" />
      <rect x="28" y="58" width="424" height="16" fill="#2563EB" />
      <Word x={240} y={56} size={22} fill="#FFFFFF">
        {labels.badge}
      </Word>
      <Word x={240} y={136} size={20} fill="#0F172A">
        MessageFlowMediaSetup.exe
      </Word>
      <Word x={240} y={172} size={20} fill="#334155">
        {labels.secondary}
      </Word>
      <rect x="130" y="210" width="220" height="48" rx="12" fill="#2563EB" />
      <Word x={240} y={242} size={24} fill="#FFFFFF">
        {labels.action}
      </Word>
    </GuideFrame>
  );
}

function InstallGuide({ title, labels }: GuideProps) {
  return (
    <GuideFrame title={title}>
      <rect
        x="28"
        y="22"
        width="424"
        height="256"
        rx="16"
        fill="#fff"
        stroke="#E2E8F0"
        strokeWidth="2"
      />
      <rect x="28" y="22" width="424" height="56" rx="16" fill="#2563EB" />
      <rect x="28" y="62" width="424" height="16" fill="#2563EB" />
      <Word x={240} y={58} size={22} fill="#FFFFFF">
        {labels.secondary}
      </Word>
      <Word x={240} y={128} size={26} fill="#0F172A">
        {labels.primary}
      </Word>
      <Word x={240} y={162} size={20} fill="#334155">
        Windows 10 / 11
      </Word>
      <rect x="56" y="196" width="160" height="48" rx="12" fill="#E2E8F0" />
      <Word x={136} y={228} size={22} fill="#0F172A">
        {labels.badge}
      </Word>
      <rect x="248" y="196" width="176" height="48" rx="12" fill="#2563EB" />
      <Word x={336} y={228} size={22} fill="#FFFFFF">
        {labels.action}
      </Word>
    </GuideFrame>
  );
}

function ExtendGuide({ title, labels }: GuideProps) {
  return (
    <GuideFrame title={title}>
      <rect
        x="28"
        y="36"
        width="176"
        height="128"
        rx="14"
        fill="#0F172A"
      />
      <Word x={116} y={100} size={24} fill="#FFFFFF">
        {labels.primary}
      </Word>
      <rect x="86" y="168" width="60" height="10" rx="3" fill="#CBD5E1" />
      <rect
        x="276"
        y="28"
        width="176"
        height="144"
        rx="14"
        fill="#2563EB"
      />
      <Word x={364} y={108} size={22} fill="#FFFFFF">
        {labels.secondary}
      </Word>
      <rect x="326" y="176" width="76" height="10" rx="3" fill="#93C5FD" />
      <rect x="176" y="80" width="128" height="40" rx="10" fill="#0F172A" />
      <Word x={240} y={107} size={20} fill="#FFFFFF">
        {labels.badge}
      </Word>
      <rect x="150" y="216" width="180" height="48" rx="12" fill="#2563EB" />
      <Word x={240} y={248} size={24} fill="#FFFFFF">
        {labels.action}
      </Word>
    </GuideFrame>
  );
}

function ProjectGuide({ title, labels }: GuideProps) {
  return (
    <GuideFrame title={title}>
      <rect
        x="24"
        y="22"
        width="200"
        height="256"
        rx="16"
        fill="#fff"
        stroke="#E2E8F0"
        strokeWidth="2"
      />
      <Word x={124} y={62} size={22} fill="#0F172A">
        {labels.primary}
      </Word>
      <rect
        x="40"
        y="80"
        width="168"
        height="44"
        rx="10"
        fill="#F8FAFC"
        stroke="#CBD5E1"
        strokeWidth="2"
      />
      <Word x={124} y={110} size={18} fill="#0F172A">
        {labels.primary}
      </Word>
      <rect x="40" y="140" width="168" height="46" rx="12" fill="#2563EB" />
      <Word x={124} y={171} size={22} fill="#FFFFFF">
        {labels.action}
      </Word>
      <rect x="52" y="202" width="144" height="40" rx="10" fill="#0F172A" />
      <Word x={124} y={229} size={20} fill="#FFFFFF">
        {labels.badge}
      </Word>
      <rect x="248" y="36" width="208" height="228" rx="16" fill="#0F172A" />
      <Word x={352} y={140} size={26} fill="#FFFFFF">
        {labels.secondary}
      </Word>
      <Word x={352} y={176} size={22} fill="#FFFFFF">
        {labels.action}
      </Word>
    </GuideFrame>
  );
}
