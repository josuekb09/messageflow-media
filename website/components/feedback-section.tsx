"use client";

import { FeedbackForm } from "@/components/feedback-form";
import { useI18n } from "@/components/language-provider";

export function FeedbackSection({ variant }: { variant: "home" | "page" }) {
  const { t } = useI18n();
  const Heading = variant === "page" ? "h1" : "h2";

  return (
    <section
      id="feedback"
      className={
        variant === "page"
          ? "bg-white"
          : "border-t border-line bg-page"
      }
    >
      <div
        className={`mx-auto max-w-6xl px-5 sm:px-8 ${
          variant === "page" ? "py-16 sm:py-20" : "py-20 sm:py-24"
        }`}
      >
        <Heading className="text-3xl font-semibold tracking-tight text-ink sm:text-4xl">
          {t.feedback.title}
        </Heading>
        <p className="mt-3 max-w-2xl text-sm leading-6 text-ink-secondary sm:text-base">
          {variant === "page" ? t.feedback.pageSubtitle : t.feedback.lead}
        </p>
        <div className="mt-10 max-w-xl">
          <FeedbackForm />
        </div>
      </div>
    </section>
  );
}
