"use client";

import Link from "next/link";
import { useState } from "react";
import { useI18n } from "@/components/language-provider";

type Mode = "confirm" | "unsubscribe";
type Outcome = "idle" | "working" | "done" | "expired" | "invalid" | "error";

/**
 * The confirm and unsubscribe pages. Both act only when the button is pressed,
 * because email security scanners open links in emails without a person present.
 */
export function SubscriptionAction({ mode, token }: { mode: Mode; token: string | null }) {
  const { t } = useI18n();
  const s = t.subscription;
  const [outcome, setOutcome] = useState<Outcome>(token ? "idle" : "invalid");

  async function run() {
    setOutcome("working");
    try {
      const response = await fetch(mode === "confirm" ? "/api/subscribe/confirm" : "/api/unsubscribe", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token }),
      });
      const result = (await response.json().catch(() => ({}))) as { ok?: boolean; status?: string };
      if (result.ok) {
        setOutcome("done");
      } else if (result.status === "expired") {
        setOutcome("expired");
      } else if (response.status === 400 || result.status === "invalid") {
        setOutcome("invalid");
      } else {
        setOutcome("error");
      }
    } catch {
      setOutcome("error");
    }
  }

  const confirm = mode === "confirm";
  let title = confirm ? s.confirmTitle : s.unsubscribeTitle;
  let body = confirm ? s.confirmBody : s.unsubscribeBody;
  if (outcome === "done") {
    title = confirm ? s.confirmedTitle : s.unsubscribedTitle;
    body = confirm ? s.confirmedBody : s.unsubscribedBody;
  } else if (outcome === "expired") {
    title = s.expiredTitle;
    body = s.expiredBody;
  } else if (outcome === "invalid") {
    title = confirm ? s.invalidTitle : s.unsubscribeTitle;
    body = confirm ? s.invalidBody : s.unsubscribeInvalidBody;
  }

  const showButton = outcome === "idle" || outcome === "working" || outcome === "error";

  return (
    <section className="bg-card">
      <div className="mx-auto max-w-6xl px-5 py-20 sm:px-8 sm:py-24">
        <div className="max-w-xl rounded-2xl border border-line bg-card p-6 sm:p-8" role="status">
          <h1 className="text-pretty text-2xl font-medium text-ink sm:text-3xl">{title}</h1>
          <p className="mt-3 text-[15px] leading-7 text-ink-secondary">{body}</p>
          {outcome === "error" ? (
            <p className="mt-3 text-sm text-red-600" role="alert">
              {s.error}
            </p>
          ) : null}
          <div className="mt-6 flex flex-col gap-3 sm:flex-row">
            {showButton ? (
              <button
                type="button"
                onClick={run}
                disabled={outcome === "working"}
                className="inline-flex h-11 items-center justify-center rounded-lg bg-brand px-5 text-[15px] font-semibold text-brand-ink hover:bg-brand-hover disabled:cursor-not-allowed disabled:opacity-70"
              >
                {outcome === "working"
                  ? confirm
                    ? s.confirming
                    : s.unsubscribing
                  : confirm
                    ? s.confirmButton
                    : s.unsubscribeButton}
              </button>
            ) : null}
            <Link
              href="/"
              className="inline-flex h-11 items-center justify-center rounded-lg border border-line px-5 text-[15px] font-medium text-ink-secondary hover:bg-surface hover:text-ink"
            >
              {s.home}
            </Link>
          </div>
        </div>
      </div>
    </section>
  );
}
