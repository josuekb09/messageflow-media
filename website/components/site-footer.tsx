"use client";

import Link from "next/link";
import { BrandLockup } from "@/components/brand-logo";
import { useI18n } from "@/components/language-provider";
import { Container } from "@/components/ui";
import { site } from "@/lib/site";

export function SiteFooter() {
  const { t } = useI18n();

  return (
    <footer className="bg-slate-950 text-slate-300">
      <Container className="flex flex-col gap-10 py-14 md:flex-row md:justify-between">
        <div>
          <BrandLockup tone="dark" />
          <p className="mt-4 max-w-xs text-sm leading-6 text-slate-400">
            {t.footer.blurb}
          </p>
        </div>
        <div className="flex gap-16 text-sm">
          <div>
            <p className="font-semibold text-white">{t.footer.product}</p>
            <ul className="mt-3 space-y-2">
              <li>
                <Link href="/#features" className="hover:text-white">
                  {t.nav.features}
                </Link>
              </li>
              <li>
                <Link href="/#library" className="hover:text-white">
                  {t.nav.library}
                </Link>
              </li>
              <li>
                <Link href="/#install" className="hover:text-white">
                  {t.nav.install}
                </Link>
              </li>
              <li>
                <Link href="/download" className="hover:text-white">
                  {t.nav.download}
                </Link>
              </li>
              <li>
                <Link href="/feedback" className="hover:text-white">
                  {t.nav.support}
                </Link>
              </li>
            </ul>
          </div>
          <div>
            <p className="font-semibold text-white">{t.footer.release}</p>
            <ul className="mt-3 space-y-2 text-slate-400">
              <li>v{site.version}</li>
              <li>{site.releaseDate}</li>
              <li>{site.platform}</li>
            </ul>
          </div>
        </div>
      </Container>
      <div className="border-t border-white/10">
        <p className="mx-auto max-w-6xl px-5 py-5 text-sm text-slate-500 sm:px-8">
          {t.footer.copyright}
        </p>
      </div>
    </footer>
  );
}
