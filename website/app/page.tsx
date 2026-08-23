import { DownloadCenter } from "@/components/download-center";
import { FeatureMatrix } from "@/components/feature-matrix";
import { FeedbackSection } from "@/components/feedback-section";
import { Hero } from "@/components/hero";
import { HowItWorks } from "@/components/how-it-works";
import { ProductShowcase } from "@/components/product-showcase";

export default function HomePage() {
  return (
    <main>
      <Hero />
      <FeatureMatrix />
      <ProductShowcase />
      <HowItWorks />
      <FeedbackSection variant="home" />
      <DownloadCenter />
    </main>
  );
}
