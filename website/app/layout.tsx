import type { Metadata, Viewport } from "next";
import { cookies } from "next/headers";
import { Geist } from "next/font/google";
import { LanguageProvider } from "@/components/language-provider";
import { RevealObserver } from "@/components/reveal-observer";
import { SiteFooter } from "@/components/site-footer";
import { SiteHeader } from "@/components/site-header";
import {
  LOCALE_COOKIE,
  defaultLocale,
  isLocale,
  localeHtmlLang,
} from "@/lib/i18n";
import { site } from "@/lib/site";
import "./globals.css";

const geist = Geist({
  variable: "--font-geist",
  subsets: ["latin", "latin-ext"],
});

export const metadata: Metadata = {
  metadataBase: new URL(site.url),
  title: {
    default: `${site.name} — ${site.tagline}`,
    template: `%s · ${site.name}`,
  },
  description: site.description,
  applicationName: site.name,
  icons: { icon: "/brand/app-icon.svg" },
  alternates: {
    canonical: "/",
  },
  openGraph: {
    type: "website",
    locale: "en_US",
    url: site.url,
    siteName: site.name,
    title: `${site.name} — ${site.tagline}`,
    description: site.description,
  },
  twitter: {
    card: "summary_large_image",
    title: `${site.name} — ${site.tagline}`,
    description: site.description,
  },
};

export const viewport: Viewport = {
  themeColor: [
    { media: "(prefers-color-scheme: light)", color: "#f5f8fc" },
    { media: "(prefers-color-scheme: dark)", color: "#0a1220" },
  ],
  width: "device-width",
  initialScale: 1,
  viewportFit: "cover",
};

export default async function RootLayout({ children }: LayoutProps<"/">) {
  const cookieStore = await cookies();
  const stored = cookieStore.get(LOCALE_COOKIE)?.value;
  const initialLocale = isLocale(stored) ? stored : defaultLocale;

  return (
    <html
      lang={localeHtmlLang[initialLocale]}
      className={`${geist.variable} h-full overflow-x-clip antialiased`}
      data-scroll-behavior="smooth"
      suppressHydrationWarning
    >
      <head>
        {/* Marks that JavaScript runs before the first paint, so scroll-reveal content
            is hidden only when it will also be shown again. */}
        <script dangerouslySetInnerHTML={{ __html: "document.documentElement.classList.add('js')" }} />
      </head>
      <body className="flex min-h-full flex-col overflow-x-clip bg-page font-sans text-ink">
        <LanguageProvider initialLocale={initialLocale}>
          <SiteHeader />
          <div className="flex-1">{children}</div>
          <SiteFooter />
          <RevealObserver />
        </LanguageProvider>
      </body>
    </html>
  );
}
