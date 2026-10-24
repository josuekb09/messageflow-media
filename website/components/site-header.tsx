"use client";

import Link from "next/link";
import { BrandLogo } from "@/components/brand-logo";
import { LanguageSwitcher } from "@/components/language-switcher";
import { GitHubIcon } from "@/components/line-icons";
import { useI18n } from "@/components/language-provider";
import { site } from "@/lib/site";

export function SiteHeader() {
  const { t } = useI18n();

  return (
    <header className="sticky top-0 z-40 border-b border-line bg-page/85 pt-[env(safe-area-inset-top)] backdrop-blur-md">
      <div className="mx-auto flex h-14 max-w-6xl items-center justify-between gap-3 px-4 sm:h-16 sm:px-8">
        <Link href="/" className="flex min-w-0 shrink items-center gap-2.5">
          <BrandLogo className="h-7 w-7 shrink-0" />
          <span className="hidden truncate font-serif text-[18px] font-medium tracking-tight text-ink min-[380px]:inline">
            {site.name}
          </span>
        </Link>
        <div className="flex shrink-0 items-center gap-1.5 sm:gap-3">
          <a
            href={`https://github.com/${site.githubRepo}`}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex h-9 items-center gap-1.5 rounded-lg px-2 text-[13px] font-medium text-ink-muted transition-colors hover:text-ink sm:px-2.5"
          >
            <GitHubIcon className="h-4 w-4" />
            <span className="hidden sm:inline">{t.header.github}</span>
            <span className="sr-only sm:hidden">{t.header.github}</span>
          </a>
          <LanguageSwitcher />
        </div>
      </div>
    </header>
  );
}
