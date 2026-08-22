import type { Metadata } from "next";
import { DownloadCenter, DownloadHero } from "@/components/download-center";
import { HowItWorks } from "@/components/how-it-works";
import { site } from "@/lib/site";

export const metadata: Metadata = {
  title: "Download",
  description: `Download MessageFlow Media ${site.version} for Windows. English, French, and Kiswahili sermons, Bibles, and songbooks.`,
};

export default function DownloadPage() {
  return (
    <main>
      <DownloadHero />
      <DownloadCenter />
      <HowItWorks />
    </main>
  );
}
