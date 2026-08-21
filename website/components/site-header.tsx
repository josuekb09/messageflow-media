"use client";

import Link from "next/link";
import { BrandLogo } from "@/components/brand-logo";
import { DownloadButton } from "@/components/download-button";
import { LanguageSwitcher } from "@/components/language-switcher";
import { useI18n } from "@/components/language-provider";
import { site } from "@/lib/site";

export function SiteHeader() {
  const { t } = useI18n();
  const navLinks = [
    { href: "/#features", label: t.nav.features },
    { href: "/#product", label: t.nav.product },
    { href: "/#install", label: t.nav.install },
    { href: "/download", label: t.nav.download },
  ];

  return (
    <header className="sticky top-0 z-40 border-b border-line bg-white/90 backdrop-blur-sm">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between gap-4 px-5 sm:px-8">
        <Link href="/" className="flex shrink-0 items-center gap-2.5">
          <BrandLogo className="h-8 w-8" />
          <span className="text-[15px] font-semibold tracking-tight text-ink whitespace-nowrap">
            {site.name}
          </span>
        </Link>
        <nav className="hidden items-center gap-7 text-[14px] text-ink-secondary lg:flex">
          {navLinks.map((link) => (
            <Link key={link.href} href={link.href} className="hover:text-ink">
              {link.label}
            </Link>
          ))}
        </nav>
        <div className="flex items-center gap-3">
          <LanguageSwitcher />
          <div className="hidden sm:block">
            <DownloadButton />
          </div>
        </div>
      </div>
    </header>
  );
}
