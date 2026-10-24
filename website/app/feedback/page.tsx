import type { Metadata } from "next";
import { FeedbackSection } from "@/components/feedback-section";

export const metadata: Metadata = {
  title: "Feedback",
  description:
    "Send a comment, an idea, or tell us about a problem with MessageFlow Media.",
  alternates: {
    canonical: "/feedback",
  },
  openGraph: {
    url: "/feedback",
  },
};

export default function FeedbackPage() {
  return (
    <main>
      <FeedbackSection variant="page" />
    </main>
  );
}
