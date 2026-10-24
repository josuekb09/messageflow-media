import { AboutSection } from "@/components/about-section";
import { CtaBand } from "@/components/cta-band";
import { FeatureMatrix } from "@/components/feature-matrix";
import { Hero } from "@/components/hero";
import { HowItWorks } from "@/components/how-it-works";
import { ProductShowcase } from "@/components/product-showcase";
import { SolveSection } from "@/components/solve-section";

export default function HomePage() {
  return (
    <main>
      <Hero />
      <AboutSection />
      <SolveSection />
      <ProductShowcase />
      <FeatureMatrix />
      <HowItWorks />
      <CtaBand />
    </main>
  );
}
