"use client";

import { FeedbackForm } from "@/components/feedback-form";
import { useI18n } from "@/components/language-provider";
import { Container } from "@/components/ui";

export function FeedbackSection({ variant }: { variant: "home" | "page" }) {
  const { t } = useI18n();
  const Heading = variant === "page" ? "h1" : "h2";

  return (
    <section
      id="feedback"
      className={
        variant === "page"
          ? "mesh-hero"
          : "border-t border-slate-200/80 bg-slate-50"
      }
    >
      <Container className={variant === "page" ? "py-16 sm:py-20" : "py-20 sm:py-24"}>
        <Heading className="text-3xl font-semibold tracking-tight text-ink sm:text-[2.5rem]">
          {t.feedback.title}
        </Heading>
        <p className="mt-4 max-w-2xl text-base leading-7 text-slate-600">
          {variant === "page" ? t.feedback.pageSubtitle : t.feedback.lead}
        </p>
        <div className="mt-10 max-w-xl">
          <FeedbackForm />
        </div>
      </Container>
    </section>
  );
}
