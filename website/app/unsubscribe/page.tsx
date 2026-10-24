import type { Metadata } from "next";
import { SubscriptionAction } from "@/components/subscription-action";

export const metadata: Metadata = {
  title: "Unsubscribe",
  robots: { index: false, follow: false },
};

export default async function UnsubscribePage({ searchParams }: PageProps<"/unsubscribe">) {
  const { token } = await searchParams;
  return (
    <main>
      <SubscriptionAction mode="unsubscribe" token={typeof token === "string" ? token : null} />
    </main>
  );
}
