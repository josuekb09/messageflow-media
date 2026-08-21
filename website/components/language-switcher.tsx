"use client";

import { localeNames, locales } from "@/lib/i18n";
import { useI18n } from "@/components/language-provider";

export function LanguageSwitcher() {
  const { locale, setLocale, t } = useI18n();

  return (
    <div
      role="group"
      aria-label={t.header.languageLabel}
      className="inline-flex items-center rounded-lg border border-line bg-white p-0.5 text-[13px]"
    >
      {locales.map((code) => {
        const active = code === locale;
        return (
          <button
            key={code}
            type="button"
            onClick={() => setLocale(code)}
            aria-pressed={active}
            className={`rounded-md px-2.5 py-1.5 font-medium transition-colors ${
              active
                ? "bg-page text-ink"
                : "text-ink-muted hover:text-ink"
            }`}
          >
            {localeNames[code]}
          </button>
        );
      })}
    </div>
  );
}
