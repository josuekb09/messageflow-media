import type { Metadata } from "next";
import { SubscriptionAction } from "@/components/subscription-action";

export const metadata: Metadata = {
  title: "Confirm your email",
  robots: { index: false, follow: false },
};

export default async function ConfirmPage({ searchParams }: PageProps<"/subscribe/confirm">) {
  const { token } = await searchParams;
  return (
    <main>
      <SubscriptionAction mode="confirm" token={typeof token === "string" ? token : null} />
    </main>
  );
}
