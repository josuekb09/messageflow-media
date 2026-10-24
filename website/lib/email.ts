import "server-only";

import { interpolate, type Locale } from "@/lib/i18n";
import { escapeHtml, notesToHtml, notesToText } from "@/lib/release-notes";
import { site } from "@/lib/site";

const RESEND_API = "https://api.resend.com";
const BRAND = "#b8791e";
const INK = "#0a0a0a";
const MUTED = "#737373";
const LINE = "#e5e5e5";

export class ResendNotConfiguredError extends Error {}

/** Thrown when Resend answered and refused the request, so nothing was sent. */
export class ResendRejectedError extends Error {}

export type Email = {
  to: string;
  subject: string;
  html: string;
  text: string;
  headers?: Record<string, string>;
};

/** Base URL for links in emails. SITE_URL lets a local or preview run link to itself. */
export function siteUrl() {
  return (process.env.SITE_URL?.trim() || site.url).replace(/\/+$/, "");
}

function resendConfig() {
  const key = process.env.RESEND_API_KEY?.trim();
  const from = process.env.RESEND_FROM?.trim();
  if (!key || !from) {
    throw new ResendNotConfiguredError("RESEND_API_KEY and RESEND_FROM must be set.");
  }
  return { key, from };
}

async function resendPost(path: string, body: unknown, idempotencyKey?: string) {
  const { key } = resendConfig();
  const response = await fetch(`${RESEND_API}${path}`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${key}`,
      "Content-Type": "application/json",
      ...(idempotencyKey ? { "Idempotency-Key": idempotencyKey } : {}),
    },
    body: JSON.stringify(body),
    cache: "no-store",
  });
  if (!response.ok) {
    const detail = await response.text().catch(() => "");
    const message = `Resend ${path} failed (${response.status}): ${detail.slice(0, 300)}`;
    // A 5xx may still have sent the mail; only a 4xx proves it was refused.
    throw response.status < 500 ? new ResendRejectedError(message) : new Error(message);
  }
}

export async function sendEmail(email: Email) {
  const { from } = resendConfig();
  await resendPost("/emails", { from, ...email });
}

/** Up to 100 emails in one request. */
export async function sendBatch(emails: Email[], idempotencyKey: string) {
  const { from } = resendConfig();
  await resendPost(
    "/emails/batch",
    emails.map((email) => ({ from, ...email })),
    idempotencyKey,
  );
}

type EmailCopy = {
  privacy: string;
  unsubscribe: string;
  confirmSubject: string;
  confirmTitle: string;
  confirmBody: string;
  confirmButton: string;
  confirmIgnore: string;
  announceSubject: string;
  announceTitle: string;
  announceBody: string;
  whatsNew: string;
  notesInEnglish: string;
  downloadButton: string;
};

const copy: Record<Locale, EmailCopy> = {
  en: {
    privacy:
      "We only use your email to tell you about new versions of MessageFlow Media. We never sell it, and you can unsubscribe at any time.",
    unsubscribe: "Unsubscribe",
    confirmSubject: "Please confirm your email for MessageFlow Media",
    confirmTitle: "One last step",
    confirmBody:
      "Thank you for asking to hear about new versions of MessageFlow Media. Please confirm your email address with the button below.",
    confirmButton: "Confirm my email",
    confirmIgnore:
      "If you did not ask for this, you can ignore this email. You will not receive anything else.",
    announceSubject: "MessageFlow Media {version} is ready",
    announceTitle: "A new version is ready",
    announceBody: "MessageFlow Media {version} is now available to download for Windows.",
    whatsNew: "What's new",
    notesInEnglish: "",
    downloadButton: "Download for Windows",
  },
  fr: {
    privacy:
      "Nous utilisons votre adresse e-mail uniquement pour vous annoncer les nouvelles versions de MessageFlow Media. Nous ne la vendons jamais, et vous pouvez vous désabonner à tout moment.",
    unsubscribe: "Se désabonner",
    confirmSubject: "Veuillez confirmer votre adresse e-mail pour MessageFlow Media",
    confirmTitle: "Une dernière étape",
    confirmBody:
      "Merci de vouloir être informé des nouvelles versions de MessageFlow Media. Veuillez confirmer votre adresse e-mail avec le bouton ci-dessous.",
    confirmButton: "Confirmer mon adresse",
    confirmIgnore:
      "Si vous n'avez rien demandé, ignorez simplement cet e-mail. Vous ne recevrez rien d'autre.",
    announceSubject: "MessageFlow Media {version} est disponible",
    announceTitle: "Une nouvelle version est disponible",
    announceBody: "MessageFlow Media {version} peut maintenant être téléchargé pour Windows.",
    whatsNew: "Nouveautés",
    notesInEnglish: "Les notes de version sont en anglais.",
    downloadButton: "Télécharger pour Windows",
  },
  sw: {
    privacy:
      "Tunatumia barua pepe yako tu kukujulisha kuhusu matoleo mapya ya MessageFlow Media. Hatuiuzi kamwe, na unaweza kujiondoa wakati wowote.",
    unsubscribe: "Jiondoe",
    confirmSubject: "Tafadhali thibitisha barua pepe yako kwa MessageFlow Media",
    confirmTitle: "Hatua moja ya mwisho",
    confirmBody:
      "Asante kwa kuomba kujulishwa kuhusu matoleo mapya ya MessageFlow Media. Tafadhali thibitisha barua pepe yako kwa kubonyeza kitufe hapa chini.",
    confirmButton: "Thibitisha barua pepe yangu",
    confirmIgnore:
      "Ikiwa hukuomba hili, puuza barua pepe hii. Hutapokea kitu kingine chochote.",
    announceSubject: "MessageFlow Media {version} iko tayari",
    announceTitle: "Toleo jipya liko tayari",
    announceBody: "MessageFlow Media {version} sasa inapatikana kupakuliwa kwa Windows.",
    whatsNew: "Mapya",
    notesInEnglish: "Maelezo ya toleo yako kwa Kiingereza.",
    downloadButton: "Pakua kwa Windows",
  },
};

function button(href: string, label: string) {
  return `<a href="${escapeHtml(href)}" style="display:inline-block;background:${BRAND};color:${INK};font-weight:600;font-size:15px;text-decoration:none;padding:12px 22px;border-radius:8px">${escapeHtml(label)}</a>`;
}

function layout(locale: Locale, title: string, bodyHtml: string, unsubscribeUrl: string) {
  const c = copy[locale];
  return `<!doctype html>
<html lang="${locale}">
<head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>${escapeHtml(title)}</title></head>
<body style="margin:0;padding:0;background:#f5f5f5">
<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f5f5f5;padding:24px 12px">
<tr><td align="center">
<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background:#ffffff;border:1px solid ${LINE};border-radius:16px;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:${INK};font-size:15px;line-height:1.6">
<tr><td style="padding:28px 28px 0;font-weight:600;font-size:15px"><span style="color:${BRAND}">&#9679;</span> ${escapeHtml(site.name)}</td></tr>
<tr><td style="padding:20px 28px 8px">
<h1 style="margin:0 0 16px;font-size:22px;font-weight:600;line-height:1.3">${escapeHtml(title)}</h1>
${bodyHtml}
</td></tr>
<tr><td style="padding:16px 28px 28px;border-top:1px solid ${LINE};color:${MUTED};font-size:13px;line-height:1.5">
${escapeHtml(c.privacy)}<br>
<a href="${escapeHtml(unsubscribeUrl)}" style="color:${MUTED}">${escapeHtml(c.unsubscribe)}</a>
</td></tr>
</table>
</td></tr>
</table>
</body>
</html>`;
}

function unsubscribeUrl(token: string) {
  return `${siteUrl()}/unsubscribe?token=${encodeURIComponent(token)}`;
}

/** Headers that let Gmail and Outlook show their own one-click "Unsubscribe" button. */
function unsubscribeHeaders(token: string) {
  return {
    "List-Unsubscribe": `<${siteUrl()}/api/unsubscribe?token=${encodeURIComponent(token)}>`,
    "List-Unsubscribe-Post": "List-Unsubscribe=One-Click",
  };
}

export function confirmationEmail(
  locale: Locale,
  to: string,
  confirmToken: string,
  unsubscribeToken: string,
): Email {
  const c = copy[locale];
  const confirmUrl = `${siteUrl()}/subscribe/confirm?token=${encodeURIComponent(confirmToken)}`;
  const leave = unsubscribeUrl(unsubscribeToken);
  const body = `<p style="margin:0 0 20px">${escapeHtml(c.confirmBody)}</p>
<p style="margin:0 0 20px">${button(confirmUrl, c.confirmButton)}</p>
<p style="margin:0 0 16px;color:${MUTED};font-size:13px">${escapeHtml(c.confirmIgnore)}</p>`;

  return {
    to,
    subject: c.confirmSubject,
    html: layout(locale, c.confirmTitle, body, leave),
    text: [c.confirmTitle, "", c.confirmBody, "", `${c.confirmButton}: ${confirmUrl}`, "", c.confirmIgnore, "", "--", c.privacy, `${c.unsubscribe}: ${leave}`].join("\n"),
    headers: unsubscribeHeaders(unsubscribeToken),
  };
}

export function announcementEmail(
  locale: Locale,
  to: string,
  version: string,
  notesMarkdown: string,
  unsubscribeToken: string,
): Email {
  const c = copy[locale];
  const downloadUrl = `${siteUrl()}/download`;
  const leave = unsubscribeUrl(unsubscribeToken);
  const intro = interpolate(c.announceBody, { version });
  const notes = notesMarkdown
    ? `<h2 style="margin:8px 0 12px;font-size:17px;font-weight:600">${escapeHtml(c.whatsNew)}</h2>
${c.notesInEnglish ? `<p style="margin:0 0 12px;color:${MUTED};font-size:13px">${escapeHtml(c.notesInEnglish)}</p>` : ""}
${notesToHtml(notesMarkdown, BRAND)}`
    : "";
  const body = `<p style="margin:0 0 20px">${escapeHtml(intro)}</p>
${notes}
<p style="margin:8px 0 20px">${button(downloadUrl, c.downloadButton)}</p>`;

  const text = [
    c.announceTitle,
    "",
    intro,
    ...(notesMarkdown ? ["", c.whatsNew, ...(c.notesInEnglish ? [c.notesInEnglish] : []), "", notesToText(notesMarkdown)] : []),
    "",
    `${c.downloadButton}: ${downloadUrl}`,
    "",
    "--",
    c.privacy,
    `${c.unsubscribe}: ${leave}`,
  ].join("\n");

  return {
    to,
    subject: interpolate(c.announceSubject, { version }),
    html: layout(locale, c.announceTitle, body, leave),
    text,
    headers: unsubscribeHeaders(unsubscribeToken),
  };
}
