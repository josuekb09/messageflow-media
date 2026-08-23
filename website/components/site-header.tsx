"use client";

import { useState } from "react";
import Link from "next/link";
import { Menu, X } from "lucide-react";
import { BrandLockup } from "@/components/brand-logo";
import { DownloadButton } from "@/components/download-button";
import { LanguageSwitcher } from "@/components/language-switcher";
import { useI18n } from "@/components/language-provider";
import { Container } from "@/components/ui";

export function SiteHeader() {
  const { t } = useI18n();
  const [open, setOpen] = useState(false);

  const navLinks = [
    { href: "/#features", label: t.nav.features },
    { href: "/#library", label: t.nav.library },
    { href: "/#install", label: t.nav.install },
    { href: "/feedback", label: t.nav.support },
  ];

  return (
    <header className="sticky top-0 z-50 border-b border-slate-200/60 bg-white/75 backdrop-blur-xl">
      <Container className="flex h-16 items-center justify-between gap-4">
        <BrandLockup />
        <nav className="hidden items-center gap-7 text-[13.5px] font-medium text-slate-600 lg:flex">
          {navLinks.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className="transition-colors duration-200 hover:text-ink"
            >
              {link.label}
            </Link>
          ))}
        </nav>
        <div className="flex items-center gap-2.5 sm:gap-3">
          <LanguageSwitcher />
          <div className="hidden sm:block">
            <DownloadButton />
          </div>
          <button
            type="button"
            className="inline-flex h-10 w-10 items-center justify-center rounded-xl border border-slate-200 text-ink lg:hidden"
            aria-expanded={open}
            aria-label={open ? t.header.close : t.header.menu}
            onClick={() => setOpen((value) => !value)}
          >
            {open ? <X className="h-4 w-4" /> : <Menu className="h-4 w-4" />}
          </button>
        </div>
      </Container>
      {open ? (
        <div className="border-t border-slate-200/70 bg-white/95 px-5 py-4 backdrop-blur-xl lg:hidden">
          <nav className="flex flex-col gap-1">
            {navLinks.map((link) => (
              <Link
                key={link.href}
                href={link.href}
                onClick={() => setOpen(false)}
                className="rounded-lg px-3 py-2.5 text-sm font-medium text-slate-700 hover:bg-slate-50"
              >
                {link.label}
              </Link>
            ))}
          </nav>
          <div className="mt-3 sm:hidden">
            <DownloadButton className="w-full" />
          </div>
        </div>
      ) : null}
    </header>
  );
}
