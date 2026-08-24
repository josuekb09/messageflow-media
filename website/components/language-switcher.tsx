"use client";

import { localeNames, locales } from "@/lib/i18n";
import { useI18n } from "@/components/language-provider";

export function LanguageSwitcher() {
  const { locale, setLocale, t } = useI18n();

  return (
    <div
      role="group"
      aria-label={t.header.languageLabel}
      className="inline-flex items-center rounded-lg border border-line bg-white p-0.5 text-[12px] sm:text-[13px]"
    >
      {locales.map((code) => {
        const active = code === locale;
        return (
          <button
            key={code}
            type="button"
            onClick={() => setLocale(code)}
            aria-pressed={active}
            aria-label={localeNames[code]}
            className={`min-h-9 min-w-9 rounded-md px-2 py-1.5 font-medium transition-colors sm:min-w-0 sm:px-2.5 ${
              active
                ? "bg-page text-ink"
                : "text-ink-muted hover:text-ink"
            }`}
          >
            <span className="md:hidden">{code.toUpperCase()}</span>
            <span className="hidden md:inline">{localeNames[code]}</span>
          </button>
        );
      })}
    </div>
  );
}
