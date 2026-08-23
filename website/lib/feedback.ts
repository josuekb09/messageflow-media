import { site } from "@/lib/site";

export const FEEDBACK_CATEGORIES = ["comment", "feature", "bug"] as const;

export type FeedbackCategory = (typeof FEEDBACK_CATEGORIES)[number];

export const FEEDBACK_LIMITS = {
  name: 120,
  email: 254,
  message: 5000,
  messageMin: 4,
} as const;

export const CATEGORY_SUBJECT: Record<FeedbackCategory, string> = {
  comment: "Comment",
  feature: "Feature request",
  bug: "Bug report",
};

export const FORMSUBMIT_AJAX_URL = `https://formsubmit.co/ajax/${site.supportEmail}`;

export const FORMSUBMIT_SUBJECT = "New Feedback from MessageFlow User";

export const FORMSUBMIT_CC = site.ccEmail;

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export type FeedbackFields = {
  name: string;
  email: string;
  category: FeedbackCategory;
  message: string;
};

export type FeedbackFieldKey = keyof FeedbackFields;

export type FeedbackParseResult =
  | { ok: true; data: FeedbackFields; honeypot: boolean }
  | { ok: false; fields: Partial<Record<FeedbackFieldKey, true>> };

function readString(value: unknown): string {
  return typeof value === "string" ? value.trim() : "";
}

export function isFeedbackCategory(value: unknown): value is FeedbackCategory {
  return (
    value === "comment" || value === "feature" || value === "bug"
  );
}

export function parseFeedbackPayload(body: unknown): FeedbackParseResult {
  if (!body || typeof body !== "object") {
    return {
      ok: false,
      fields: { email: true, category: true, message: true },
    };
  }

  const raw = body as Record<string, unknown>;
  const name = readString(raw.name).slice(0, FEEDBACK_LIMITS.name);
  const email = readString(raw.email);
  const message = readString(raw.message);
  const category = raw.category;
  const honeypot =
    raw.botcheck === true ||
    raw.botcheck === "true" ||
    raw.botcheck === "on" ||
    raw.botcheck === "1";

  const fields: Partial<Record<FeedbackFieldKey, true>> = {};

  if (email.length === 0 || email.length > FEEDBACK_LIMITS.email || !EMAIL_PATTERN.test(email)) {
    fields.email = true;
  }

  if (!isFeedbackCategory(category)) {
    fields.category = true;
  }

  if (
    message.length < FEEDBACK_LIMITS.messageMin ||
    message.length > FEEDBACK_LIMITS.message
  ) {
    fields.message = true;
  }

  if (Object.keys(fields).length > 0) {
    return { ok: false, fields };
  }

  return {
    ok: true,
    honeypot,
    data: {
      name,
      email,
      category: category as FeedbackCategory,
      message,
    },
  };
}

export function buildFeedbackMail(data: FeedbackFields) {
  const categoryLabel = CATEGORY_SUBJECT[data.category];
  const displayName = data.name || "Anonymous";
  const subject = `[MessageFlow] ${categoryLabel}`;
  const body = [
    `To: ${site.supportEmail}`,
    `Cc: ${site.ccEmail}`,
    `Category: ${categoryLabel}`,
    `Name: ${displayName}`,
    `Email: ${data.email}`,
    "",
    data.message,
  ].join("\n");
  return { categoryLabel, displayName, subject, body };
}

export function buildFormSubmitPayload(data: FeedbackFields): Record<string, string> {
  const mail = buildFeedbackMail(data);
  return {
    Name: mail.displayName,
    Email: data.email,
    _replyto: data.email,
    Category: mail.categoryLabel,
    Message: data.message,
    _subject: FORMSUBMIT_SUBJECT,
    _cc: FORMSUBMIT_CC,
    _template: "table",
    _captcha: "false",
  };
}

function isSuccessFlag(value: unknown): boolean {
  return value === true || value === "true";
}

function isFailureFlag(value: unknown): boolean {
  return value === false || value === "false";
}

function looksLikeActivation(message: unknown): boolean {
  if (typeof message !== "string") return false;
  const text = message.toLowerCase();
  return (
    text.includes("activat") ||
    text.includes("confirm") ||
    text.includes("check your email")
  );
}

export function isFormSubmitAccepted(httpOk: boolean, result: unknown): boolean {
  if (!result || typeof result !== "object") return httpOk;
  const record = result as { success?: unknown; message?: unknown };
  if (isSuccessFlag(record.success)) return true;
  if (isFailureFlag(record.success)) return looksLikeActivation(record.message);
  return httpOk;
}
