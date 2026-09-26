import { FeatureMatrix } from "@/components/feature-matrix";
import { Hero } from "@/components/hero";
import { HowItWorks } from "@/components/how-it-works";
import { ProductShowcase } from "@/components/product-showcase";

export default function HomePage() {
  return (
    <main>
      <Hero />
      <ProductShowcase />
      <FeatureMatrix />
      <HowItWorks />
    </main>
  );
}
