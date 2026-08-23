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
