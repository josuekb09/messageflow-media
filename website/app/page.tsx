import { FeatureMatrix } from "@/components/feature-matrix";
import { Hero } from "@/components/hero";
import { HowItWorks } from "@/components/how-it-works";
import { ProductShowcase } from "@/components/product-showcase";
import { WhatsNew } from "@/components/whats-new";

export default function HomePage() {
  return (
    <main>
      <Hero />
      <WhatsNew />
      <ProductShowcase />
      <FeatureMatrix />
      <HowItWorks />
    </main>
  );
}
