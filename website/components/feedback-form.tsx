"use client";

import { FormEvent, useState } from "react";
import { useI18n } from "@/components/language-provider";
import {
  FEEDBACK_CATEGORIES,
  FEEDBACK_LIMITS,
  FORMSUBMIT_AJAX_URL,
  FORMSUBMIT_SUBJECT,
  buildFormSubmitPayload,
  isFormSubmitAccepted,
  parseFeedbackPayload,
  type FeedbackCategory,
  type FeedbackFieldKey,
  type FeedbackFields,
} from "@/lib/feedback";

type Status = "idle" | "submitting" | "success" | "error";
type ErrorCode = "validation" | "upstream" | "invalid_json";

const inputClass =
  "mt-1.5 w-full rounded-xl border bg-white px-3.5 py-2.5 text-sm text-ink shadow-sm placeholder:text-slate-400 transition-all duration-200 focus:border-indigo-500 focus:outline-none focus:ring-2 focus:ring-indigo-500/20";

function fieldClass(invalid: boolean) {
  return `${inputClass} ${invalid ? "border-red-400" : "border-line"}`;
}

export function FeedbackForm() {
  const { t } = useI18n();
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [category, setCategory] = useState<FeedbackCategory>("comment");
  const [message, setMessage] = useState("");
  const [status, setStatus] = useState<Status>("idle");
  const [errorCode, setErrorCode] = useState<ErrorCode | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Partial<Record<FeedbackFieldKey, true>>>({});

  const categoryLabels = {
    comment: t.feedback.categories.comment,
    feature: t.feedback.categories.feature,
    bug: t.feedback.categories.bug,
  };

  function resetForm() {
    setName("");
    setEmail("");
    setCategory("comment");
    setMessage("");
    setStatus("idle");
    setErrorCode(null);
    setFieldErrors({});
  }

  function errorMessage() {
    if (errorCode === "validation") return t.feedback.errorValidation;
    return t.feedback.error;
  }

  async function sendViaFormSubmit(data: FeedbackFields): Promise<boolean> {
    const response = await fetch(FORMSUBMIT_AJAX_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify(buildFormSubmitPayload(data)),
    });
    const result = await response.json().catch(() => null);
    return isFormSubmitAccepted(response.ok, result);
  }

  async function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorCode(null);

    const formData = new FormData(event.currentTarget);
    const parsed = parseFeedbackPayload({
      name,
      email,
      category,
      message,
      botcheck: formData.get("botcheck"),
    });

    if (!parsed.ok) {
      setFieldErrors(parsed.fields);
      setErrorCode("validation");
      setStatus("error");
      return;
    }

    setFieldErrors({});
    setStatus("submitting");

    if (parsed.honeypot) {
      setStatus("success");
      return;
    }

    try {
      if (await sendViaFormSubmit(parsed.data)) {
        setStatus("success");
        return;
      }

      const response = await fetch("/api/feedback", {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify({
          name: parsed.data.name,
          email: parsed.data.email,
          category: parsed.data.category,
          message: parsed.data.message,
          botcheck: parsed.honeypot,
        }),
      });

      const result = (await response.json().catch(() => null)) as
        | { ok?: boolean; code?: ErrorCode; fields?: Partial<Record<FeedbackFieldKey, true>> }
        | null;

      if (response.ok && result?.ok) {
        setStatus("success");
        return;
      }

      if (result?.code === "validation") {
        setFieldErrors(result.fields ?? {});
        setErrorCode("validation");
      } else {
        setErrorCode("upstream");
      }
      setStatus("error");
    } catch {
      setErrorCode("upstream");
      setStatus("error");
    }
  }

  if (status === "success") {
    return (
      <div
        className="rounded-3xl border border-slate-200/80 bg-white p-6 shadow-xl shadow-indigo-500/5 sm:p-8"
        role="status"
        aria-live="polite"
      >
        <div className="animate-check-pop flex h-12 w-12 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
          <svg viewBox="0 0 20 20" className="h-6 w-6" fill="none" aria-hidden>
            <path
              d="M4.5 10.5 8 14l7.5-8"
              stroke="currentColor"
              strokeWidth="2.2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </div>
        <p className="mt-4 text-base font-semibold tracking-tight text-ink">{t.feedback.success}</p>
        <p className="mt-2 text-sm leading-6 text-ink-secondary">{t.feedback.successDetail}</p>
        <button
          type="button"
          onClick={resetForm}
          className="mt-5 inline-flex h-11 items-center rounded-xl border border-slate-200 px-5 text-[15px] font-medium text-slate-600 transition-all duration-200 hover:border-slate-300 hover:bg-slate-50 hover:text-ink"
        >
          {t.feedback.again}
        </button>
      </div>
    );
  }

  return (
    <form
      onSubmit={onSubmit}
      className="rounded-3xl border border-slate-200/80 bg-white p-6 shadow-xl shadow-slate-900/5 sm:p-8"
      noValidate
    >
      <input type="hidden" name="_subject" value={FORMSUBMIT_SUBJECT} />
      <input type="hidden" name="_template" value="table" />
      <input type="hidden" name="_captcha" value="false" />
      <input type="hidden" name="_replyto" value={email} />

      <div className="hidden" aria-hidden="true">
        <label>
          botcheck
          <input type="checkbox" name="botcheck" tabIndex={-1} autoComplete="off" />
        </label>
      </div>

      <div>
        <label htmlFor="feedback-name" className="text-sm font-medium text-ink">
          {t.feedback.nameLabel}{" "}
          <span className="font-normal text-ink-muted">{t.feedback.nameOptional}</span>
        </label>
        <input
          id="feedback-name"
          name="Name"
          type="text"
          maxLength={FEEDBACK_LIMITS.name}
          autoComplete="name"
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder={t.feedback.namePlaceholder}
          className={fieldClass(false)}
        />
      </div>

      <div className="mt-5">
        <label htmlFor="feedback-email" className="text-sm font-medium text-ink">
          {t.feedback.emailLabel}
        </label>
        <input
          id="feedback-email"
          name="Email"
          type="email"
          required
          maxLength={FEEDBACK_LIMITS.email}
          autoComplete="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          placeholder={t.feedback.emailPlaceholder}
          aria-invalid={fieldErrors.email ? true : undefined}
          className={fieldClass(Boolean(fieldErrors.email))}
        />
        {fieldErrors.email ? (
          <p className="mt-1.5 text-sm text-red-600">{t.feedback.emailInvalid}</p>
        ) : null}
      </div>

      <fieldset className="mt-5">
        <legend className="text-sm font-medium text-ink">{t.feedback.categoryLabel}</legend>
        <div className="mt-2 flex flex-wrap gap-2">
          {FEEDBACK_CATEGORIES.map((value) => {
            const selected = category === value;
            return (
              <label
                key={value}
                className={`inline-flex cursor-pointer items-center rounded-full border px-3.5 py-1.5 text-sm font-medium transition-all duration-200 has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-indigo-500/30 ${
                  selected
                    ? "border-indigo-600 bg-indigo-600 text-white shadow-sm"
                    : "border-slate-200 bg-slate-50 text-slate-600 hover:border-slate-300 hover:text-ink"
                }`}
              >
                <input
                  type="radio"
                  name="Category"
                  value={value}
                  checked={selected}
                  onChange={() => setCategory(value)}
                  className="sr-only"
                />
                {categoryLabels[value]}
              </label>
            );
          })}
        </div>
      </fieldset>

      <div className="mt-5">
        <label htmlFor="feedback-message" className="text-sm font-medium text-ink">
          {t.feedback.messageLabel}
        </label>
        <textarea
          id="feedback-message"
          name="Message"
          required
          rows={6}
          minLength={FEEDBACK_LIMITS.messageMin}
          maxLength={FEEDBACK_LIMITS.message}
          value={message}
          onChange={(event) => setMessage(event.target.value)}
          placeholder={t.feedback.messagePlaceholder}
          aria-invalid={fieldErrors.message ? true : undefined}
          className={`${fieldClass(Boolean(fieldErrors.message))} resize-y`}
        />
        {fieldErrors.message ? (
          <p className="mt-1.5 text-sm text-red-600">{t.feedback.messageRequired}</p>
        ) : null}
      </div>

      {status === "error" ? (
        <p className="mt-5 text-sm text-red-600" role="alert">
          {errorMessage()}
        </p>
      ) : null}

      <button
        type="submit"
        disabled={status === "submitting"}
        className="mt-6 inline-flex h-11 items-center justify-center rounded-xl bg-gradient-to-r from-blue-600 to-indigo-600 px-5 text-[15px] font-semibold text-white shadow-lg shadow-indigo-500/20 transition-all duration-200 hover:-translate-y-0.5 disabled:cursor-not-allowed disabled:opacity-70 disabled:hover:translate-y-0"
      >
        {status === "submitting" ? t.feedback.submitting : t.feedback.submit}
      </button>
    </form>
  );
}
