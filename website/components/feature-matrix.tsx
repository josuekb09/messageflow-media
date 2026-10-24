"use client";

import { useI18n } from "@/components/language-provider";
import { BookIcon, CrossIcon, MusicNoteIcon } from "@/components/line-icons";
import { SectionHeading } from "@/components/section-heading";
import { localeHtmlLang, type Locale } from "@/lib/i18n";
import { site } from "@/lib/site";

// Same order as the dictionary items: sermons, Bible, songs.
const icons = [BookIcon, CrossIcon, MusicNoteIcon] as const;
const languages = ["en", "fr", "sw"] as const;

export function FeatureMatrix() {
  const { t, locale } = useI18n();
  const table = t.features.table;
  const count = (value: number) => new Intl.NumberFormat(localeHtmlLang[locale as Locale]).format(value);
  const { library } = site;
  const rows: { label: string; note?: string; cells: (string | null)[] }[] = [
    { label: table.branham, cells: languages.map((lang) => count(library.branham[lang])) },
    { label: table.frank, note: table.englishOnly, cells: [count(library.frank.en), null, null] },
    { label: table.bible, cells: languages.map((lang) => library.bibles[lang]) },
    { label: table.songs, cells: languages.map((lang) => count(library.songs[lang])) },
  ];

  return (
    <section id="features" className="scroll-mt-16 border-t border-line bg-surface">
      <div className="mx-auto max-w-6xl px-4 py-20 sm:px-8 sm:py-24">
        <div className="reveal">
          <SectionHeading title={t.features.title} lead={t.features.lead} />
        </div>
        <div className="mt-12 grid gap-5 md:grid-cols-3">
          {t.features.items.map((feature, index) => {
            const Icon = icons[index] ?? BookIcon;
            return (
              <article
                key={feature.title}
                className="reveal flex flex-col rounded-2xl border border-line bg-card p-6 shadow-card sm:p-8"
              >
                <Icon className="h-7 w-7 text-brand" />
                <h3 className="mt-6 text-[1.55rem] font-medium leading-tight text-ink">{feature.title}</h3>
                <p className="mt-3 flex-1 text-[15px] leading-7 text-ink-secondary">{feature.body}</p>
              </article>
            );
          })}
        </div>
        <div className="reveal mt-10 overflow-x-auto rounded-2xl border border-line bg-card shadow-card">
          <table className="w-full min-w-[34rem] border-collapse text-left text-[15px]">
            <caption className="sr-only">{table.caption}</caption>
            <thead>
              <tr className="border-b border-line">
                <th scope="col" className="px-5 py-4 font-medium text-ink-muted">
                  <span className="sr-only">{table.caption}</span>
                </th>
                {languages.map((lang) => (
                  <th key={lang} scope="col" className="px-5 py-4 font-serif text-[1.15rem] font-medium text-ink">
                    {table.columns[lang]}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => (
                <tr key={row.label} className="border-b border-line last:border-b-0">
                  <th scope="row" className="px-5 py-4 font-medium text-ink">
                    {row.label}
                    {row.note ? (
                      <span className="block text-sm font-normal text-ink-muted">{row.note}</span>
                    ) : null}
                  </th>
                  {row.cells.map((cell, index) => (
                    <td key={languages[index]} className="px-5 py-4 tabular-nums text-ink-secondary">
                      {cell ?? (
                        <>
                          <span aria-hidden="true" className="text-ink-muted">
                            —
                          </span>
                          <span className="sr-only">{table.notAvailable}</span>
                        </>
                      )}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </section>
  );
}
