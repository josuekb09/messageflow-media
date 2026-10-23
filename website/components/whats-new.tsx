"use client";

import { useI18n } from "@/components/language-provider";
import { interpolate } from "@/lib/i18n";
import { site } from "@/lib/site";

export function WhatsNew() {
  const { t } = useI18n();

  return (
    <section id="whats-new" className="scroll-mt-16 border-t border-line bg-page">
      <div className="mx-auto max-w-6xl px-5 py-16 sm:px-8 sm:py-20">
        <h2 className="text-balance text-3xl font-medium text-ink sm:text-4xl">
          {interpolate(t.whatsNew.title, { version: site.version })}
        </h2>
        <ul className="mt-10 grid gap-x-8 gap-y-8 sm:grid-cols-2 lg:grid-cols-4">
          {t.whatsNew.items.map((item) => (
            <li key={item.title} className="border-t-2 border-brand pt-5">
              <h3 className="text-lg font-medium text-ink">{item.title}</h3>
              <p className="mt-2 text-[15px] leading-7 text-ink-secondary">{item.body}</p>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
