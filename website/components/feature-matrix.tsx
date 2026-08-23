"use client";

import { Keyboard, Languages, ShieldCheck } from "lucide-react";
import { useI18n } from "@/components/language-provider";
import { Container, Kbd, SectionLead, SectionTitle } from "@/components/ui";

const icons = [ShieldCheck, Languages, Keyboard];

export function FeatureMatrix() {
  const { t } = useI18n();
  const [offline, languages, workflow] = t.features.items;

  return (
    <>
      <section id="features" className="scroll-mt-20 bg-white py-20 sm:py-24">
        <Container>
          <SectionTitle>{t.features.title}</SectionTitle>
          <SectionLead>{t.features.lead}</SectionLead>

          <div className="mt-12 grid gap-4 lg:grid-cols-5">
            <article className="relative overflow-hidden rounded-3xl border border-slate-200/80 bg-gradient-to-br from-slate-950 via-slate-900 to-indigo-950 p-7 text-white shadow-xl shadow-slate-900/10 lg:col-span-3">
              <div className="pointer-events-none absolute -right-16 -top-16 h-56 w-56 rounded-full bg-indigo-500/20 blur-3xl" />
              <span className="inline-flex h-11 w-11 items-center justify-center rounded-2xl bg-white/10 text-indigo-200 ring-1 ring-white/10">
                <ShieldCheck className="h-5 w-5" />
              </span>
              <h3 className="mt-6 text-2xl font-semibold tracking-tight">{offline.title}</h3>
              <p className="mt-3 max-w-md text-sm leading-7 text-slate-300">{offline.body}</p>
              <p className="mt-8 text-xs font-semibold uppercase tracking-[0.18em] text-indigo-200">
                Local PC · Zero cloud · Zero ads
              </p>
            </article>

            <div className="grid gap-4 lg:col-span-2">
              {[languages, workflow].map((item, index) => {
                const Icon = icons[index + 1];
                return (
                  <article
                    key={item.title}
                    className="rounded-3xl border border-slate-200/80 bg-white p-6 shadow-sm transition-all duration-200 hover:-translate-y-0.5 hover:shadow-lg hover:shadow-indigo-500/5"
                  >
                    <span className="inline-flex h-10 w-10 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600">
                      <Icon className="h-5 w-5" />
                    </span>
                    <h3 className="mt-4 text-lg font-semibold tracking-tight text-ink">
                      {item.title}
                    </h3>
                    <p className="mt-2 text-sm leading-6 text-slate-600">{item.body}</p>
                    {index === 1 ? (
                      <div className="mt-4 flex flex-wrap gap-2">
                        <Kbd>Ctrl+F</Kbd>
                        <Kbd>Ctrl+P</Kbd>
                        <Kbd>↑ ↓</Kbd>
                      </div>
                    ) : null}
                  </article>
                );
              })}
            </div>
          </div>
        </Container>
      </section>

      <section id="library" className="scroll-mt-20 border-y border-slate-200/80 bg-slate-50 py-20 sm:py-24">
        <Container>
          <SectionTitle>{t.library.title}</SectionTitle>
          <SectionLead>{t.library.lead}</SectionLead>
          <div className="mt-12 grid gap-4 md:grid-cols-3">
            {t.library.items.map((item) => (
              <article
                key={item.title}
                className="rounded-3xl border border-slate-200/80 bg-white p-6 shadow-sm transition-all duration-200 hover:-translate-y-0.5 hover:shadow-lg hover:shadow-blue-500/5"
              >
                <p className="text-sm font-semibold uppercase tracking-[0.16em] text-indigo-600">
                  {item.title}
                </p>
                <p className="mt-4 text-4xl font-semibold tracking-tight text-ink">
                  {item.sermons}
                </p>
                <p className="mt-1 text-sm text-slate-500">{item.sermonsLabel}</p>
                <div className="mt-5 space-y-2 border-t border-slate-100 pt-5 text-sm text-slate-600">
                  <p>{item.songs}</p>
                  <p>{item.bible}</p>
                </div>
              </article>
            ))}
          </div>
        </Container>
      </section>
    </>
  );
}
