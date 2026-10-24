"use client";

import Link from "next/link";
import { BrandLogo } from "@/components/brand-logo";
import { useI18n } from "@/components/language-provider";
import { site } from "@/lib/site";

export function SiteFooter() {
  const { t } = useI18n();

  return (
    <footer className="border-t border-line bg-page">
      <div className="mx-auto flex max-w-6xl flex-col gap-8 px-4 py-12 sm:px-8 md:flex-row md:items-start md:justify-between">
        <div>
          <div className="flex items-center gap-2.5">
            <BrandLogo className="h-7 w-7" />
            <span className="font-serif text-[18px] font-medium tracking-tight text-ink">
              {site.name}
            </span>
          </div>
          <p className="mt-4 max-w-sm text-sm leading-6 text-ink-muted">{t.footer.blurb}</p>
        </div>
        <ul className="flex gap-7 text-sm font-medium text-ink-secondary">
          <li>
            <a
              href={`https://github.com/${site.githubRepo}`}
              target="_blank"
              rel="noopener noreferrer"
              className="transition-colors hover:text-ink"
            >
              {t.footer.github}
            </a>
          </li>
          <li>
            <Link href="/feedback" className="transition-colors hover:text-ink">
              {t.footer.contact}
            </Link>
          </li>
        </ul>
      </div>
      <div className="border-t border-line">
        <p className="mx-auto max-w-6xl px-4 py-5 text-sm leading-6 text-ink-muted sm:px-8">
          {t.footer.copyright}
        </p>
      </div>
    </footer>
  );
}
