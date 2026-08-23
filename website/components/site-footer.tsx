"use client";

import Link from "next/link";
import { BrandLogo } from "@/components/brand-logo";
import { useI18n } from "@/components/language-provider";
import { site } from "@/lib/site";

export function SiteFooter() {
  const { t } = useI18n();

  return (
    <footer className="border-t border-line bg-white">
      <div className="mx-auto flex max-w-6xl flex-col gap-12 px-5 py-14 sm:px-8 md:flex-row md:justify-between">
        <div>
          <div className="flex items-center gap-2.5">
            <BrandLogo className="h-7 w-7" />
            <span className="text-sm font-semibold tracking-tight text-ink">
              {site.name}
            </span>
          </div>
          <p className="mt-4 max-w-xs text-sm leading-6 text-ink-muted">
            {t.footer.blurb}
          </p>
        </div>
        <div className="flex gap-16 text-sm">
          <div>
            <p className="text-[13px] font-medium tracking-wide text-ink">
              {t.footer.product}
            </p>
            <ul className="mt-4 space-y-2.5 text-ink-muted">
              <li>
                <Link href="/#features" className="transition-colors hover:text-ink">
                  {t.nav.features}
                </Link>
              </li>
              <li>
                <Link href="/#product" className="transition-colors hover:text-ink">
                  {t.nav.product}
                </Link>
              </li>
              <li>
                <Link href="/#install" className="transition-colors hover:text-ink">
                  {t.nav.install}
                </Link>
              </li>
              <li>
                <Link href="/download" className="transition-colors hover:text-ink">
                  {t.nav.download}
                </Link>
              </li>
              <li>
                <Link href="/feedback" className="transition-colors hover:text-ink">
                  {t.nav.feedback}
                </Link>
              </li>
            </ul>
          </div>
          <div>
            <p className="text-[13px] font-medium tracking-wide text-ink">
              {t.footer.release}
            </p>
            <ul className="mt-4 space-y-2.5 text-ink-muted">
              <li>v{site.version}</li>
              <li>{site.releaseDate}</li>
              <li>{site.platform}</li>
            </ul>
          </div>
        </div>
      </div>
      <div className="border-t border-line">
        <p className="mx-auto max-w-6xl px-5 py-5 text-sm text-ink-muted sm:px-8">
          {t.footer.copyright}
        </p>
      </div>
    </footer>
  );
}
