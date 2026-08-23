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
  "mt-1.5 w-full rounded-lg border bg-white px-3 py-2.5 text-sm text-ink placeholder:text-ink-muted focus:border-brand focus:outline-none focus:ring-2 focus:ring-brand/20";

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
        className="rounded-xl border border-line bg-white p-6 sm:p-7"
        role="status"
        aria-live="polite"
      >
        <p className="text-base font-semibold text-ink">{t.feedback.success}</p>
        <p className="mt-2 text-sm leading-6 text-ink-secondary">{t.feedback.successDetail}</p>
        <button
          type="button"
          onClick={resetForm}
          className="mt-5 inline-flex h-11 items-center rounded-lg border border-line px-5 text-[15px] font-medium text-ink-secondary hover:bg-page hover:text-ink"
        >
          {t.feedback.again}
        </button>
      </div>
    );
  }

  return (
    <form
      onSubmit={onSubmit}
      className="rounded-xl border border-line bg-white p-6 sm:p-7"
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
                className={`inline-flex cursor-pointer items-center rounded-lg border px-3 py-2 text-sm font-medium transition-colors has-[:focus-visible]:ring-2 has-[:focus-visible]:ring-brand/30 ${
                  selected
                    ? "border-brand bg-brand text-white"
                    : "border-line bg-page text-ink-secondary hover:text-ink"
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
        className="mt-6 inline-flex h-11 items-center justify-center rounded-lg bg-brand px-5 text-[15px] font-medium text-white hover:bg-brand-hover disabled:cursor-not-allowed disabled:opacity-70"
      >
        {status === "submitting" ? t.feedback.submitting : t.feedback.submit}
      </button>
    </form>
  );
}
