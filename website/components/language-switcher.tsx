"use client";

import { localeNames, locales } from "@/lib/i18n";
import { useI18n } from "@/components/language-provider";
import { cn } from "@/lib/cn";

export function LanguageSwitcher() {
  const { locale, setLocale, t } = useI18n();

  return (
    <div
      role="group"
      aria-label={t.header.languageLabel}
      className="inline-flex items-center rounded-full border border-slate-200/80 bg-white/70 p-0.5 text-[12px] shadow-sm backdrop-blur"
    >
      {locales.map((code) => {
        const active = code === locale;
        return (
          <button
            key={code}
            type="button"
            onClick={() => setLocale(code)}
            aria-pressed={active}
            className={cn(
              "rounded-full px-2.5 py-1 font-semibold tracking-wide uppercase transition-all duration-200",
              active
                ? "bg-slate-900 text-white shadow-sm"
                : "text-slate-500 hover:text-slate-900",
            )}
          >
            {code}
            <span className="sr-only">{localeNames[code]}</span>
          </button>
        );
      })}
    </div>
  );
}
