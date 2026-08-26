import type { Metadata } from "next";
import { FeedbackSection } from "@/components/feedback-section";

export const metadata: Metadata = {
  title: "Feedback",
  description:
    "Send comments, feature requests, or bug reports for MessageFlow Media.",
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
