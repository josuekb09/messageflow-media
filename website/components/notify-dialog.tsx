"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import { useI18n } from "@/components/language-provider";
import { isValidEmail } from "@/lib/subscription";

const ASKED_KEY = "messageflow-notify-asked";

type Step = "ask" | "email" | "done";

/** True once the visitor has answered the popup, so it is not shown on every download. */
export function wasNotifyAsked() {
  try {
    return window.localStorage.getItem(ASKED_KEY) === "1";
  } catch {
    return false;
  }
}

function rememberAsked() {
  try {
    window.localStorage.setItem(ASKED_KEY, "1");
  } catch {
    // Private windows may block storage; the popup will simply ask again next time.
  }
}

const inputClass =
  "mt-1.5 w-full rounded-lg border bg-white px-3 py-2.5 text-sm text-ink placeholder:text-ink-muted focus:border-brand focus:outline-none focus:ring-2 focus:ring-brand/20";
const primaryClass =
  "inline-flex h-11 items-center justify-center rounded-lg bg-brand px-5 text-[15px] font-semibold text-ink hover:bg-brand-hover disabled:cursor-not-allowed disabled:opacity-70";
const secondaryClass =
  "inline-flex h-11 items-center justify-center rounded-lg border border-line px-5 text-[15px] font-medium text-ink-secondary hover:bg-surface hover:text-ink";

export function NotifyDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { t, locale } = useI18n();
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [step, setStep] = useState<Step>("ask");
  const [email, setEmail] = useState("");
  const [website, setWebsite] = useState("");
  const [invalid, setInvalid] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (open && !dialog.open) {
      setStep("ask");
      setFailed(false);
      setInvalid(false);
      dialog.showModal();
    } else if (!open && dialog.open) {
      dialog.close();
    }
  }, [open]);

  function close() {
    rememberAsked();
    onClose();
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!isValidEmail(email)) {
      setInvalid(true);
      return;
    }
    setInvalid(false);
    setFailed(false);
    setSubmitting(true);
    try {
      const response = await fetch("/api/subscribe", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, language: locale, website }),
      });
      if (response.status === 400) {
        setInvalid(true);
      } else if (response.ok) {
        rememberAsked();
        setStep("done");
      } else {
        setFailed(true);
      }
    } catch {
      setFailed(true);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <dialog
      ref={dialogRef}
      aria-labelledby="notify-title"
      onClose={close}
      onClick={(event) => {
        // A click on the dimmed backdrop lands on the dialog element itself.
        if (event.target === event.currentTarget) close();
      }}
      className="m-auto w-[calc(100%-2rem)] max-w-md rounded-2xl border border-line bg-white p-0 text-ink shadow-[0_20px_50px_rgba(10,10,10,0.15)] backdrop:bg-ink/40"
    >
      <div className="relative p-6 sm:p-8">
        <button
          type="button"
          onClick={close}
          aria-label={t.notify.close}
          className="absolute right-3 top-3 inline-flex h-9 w-9 items-center justify-center rounded-lg text-ink-muted hover:bg-surface hover:text-ink"
        >
          <svg viewBox="0 0 20 20" className="h-4 w-4" aria-hidden="true">
            <path d="M5 5l10 10M15 5L5 15" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
          </svg>
        </button>

        {step === "done" ? (
          <div role="status">
            <h2 id="notify-title" className="pr-8 text-xl font-medium text-ink">
              {t.notify.successTitle}
            </h2>
            <p className="mt-3 text-[15px] leading-7 text-ink-secondary">{t.notify.successBody}</p>
            <button type="button" onClick={close} className={`mt-6 w-full sm:w-auto ${primaryClass}`}>
              {t.notify.close}
            </button>
          </div>
        ) : (
          <>
            <p className="text-sm text-ink-muted">{t.notify.downloadStarted}</p>
            <h2 id="notify-title" className="mt-2 pr-8 text-xl font-medium leading-snug text-ink">
              {t.notify.title}
            </h2>
            <p className="mt-3 text-[15px] leading-7 text-ink-secondary">{t.notify.body}</p>

            {step === "ask" ? (
              <div className="mt-6 flex flex-col gap-3 sm:flex-row">
                <button type="button" onClick={() => setStep("email")} className={primaryClass} autoFocus>
                  {t.notify.accept}
                </button>
                <button type="button" onClick={close} className={secondaryClass}>
                  {t.notify.decline}
                </button>
              </div>
            ) : (
              <form onSubmit={onSubmit} noValidate className="mt-5">
                <div className="hidden" aria-hidden="true">
                  <input
                    type="text"
                    name="website"
                    tabIndex={-1}
                    autoComplete="off"
                    value={website}
                    onChange={(event) => setWebsite(event.target.value)}
                  />
                </div>
                <label htmlFor="notify-email" className="text-sm font-medium text-ink">
                  {t.notify.emailLabel}
                </label>
                <input
                  id="notify-email"
                  type="email"
                  inputMode="email"
                  autoComplete="email"
                  autoFocus
                  required
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  placeholder={t.notify.emailPlaceholder}
                  aria-invalid={invalid}
                  aria-describedby={invalid ? "notify-email-error" : undefined}
                  className={`${inputClass} ${invalid ? "border-red-400" : "border-line"}`}
                />
                {invalid ? (
                  <p id="notify-email-error" className="mt-1.5 text-sm text-red-600">
                    {t.notify.emailInvalid}
                  </p>
                ) : null}
                {failed ? (
                  <p className="mt-3 text-sm text-red-600" role="alert">
                    {t.notify.error}
                  </p>
                ) : null}
                <div className="mt-5 flex flex-col gap-3 sm:flex-row">
                  <button type="submit" disabled={submitting} className={primaryClass}>
                    {submitting ? t.notify.submitting : t.notify.submit}
                  </button>
                  <button type="button" onClick={close} className={secondaryClass}>
                    {t.notify.decline}
                  </button>
                </div>
              </form>
            )}

            <p className="mt-5 text-[13px] leading-5 text-ink-muted">{t.notify.privacy}</p>
          </>
        )}
      </div>
    </dialog>
  );
}
