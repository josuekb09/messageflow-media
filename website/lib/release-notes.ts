/**
 * Picks the user-facing part of a GitHub release body for the announcement email:
 * the "What's new" section if there is one, otherwise "Highlights". Sections such as
 * "Verified before release" or "Download" (checksums, sizes) are never included.
 */
const PREFERRED_SECTIONS = [/^what['’]?s new\b/i, /^highlights\b/i];

type Section = { heading: string; lines: string[] };

function splitSections(markdown: string): Section[] {
  const sections: Section[] = [];
  let current: Section | null = null;
  for (const line of markdown.replace(/\r\n?/g, "\n").split("\n")) {
    const heading = /^#{1,3}\s+(.+?)\s*#*\s*$/.exec(line);
    if (heading) {
      current = { heading: heading[1], lines: [] };
      sections.push(current);
    } else if (current) {
      current.lines.push(line);
    }
  }
  return sections;
}

export function extractWhatsNew(markdown: string): string {
  const sections = splitSections(markdown ?? "");
  for (const pattern of PREFERRED_SECTIONS) {
    const match = sections.find((section) => pattern.test(section.heading.trim()));
    if (match) {
      return match.lines.join("\n").trim();
    }
  }
  return "";
}

export function escapeHtml(value: string) {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function inlineHtml(text: string, linkColor: string) {
  return escapeHtml(text)
    .replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>")
    .replace(/`([^`]+)`/g, "$1")
    .replace(/\[([^\]]+)\]\((https:\/\/[^\s)]+)\)/g, `<a href="$2" style="color:${linkColor}">$1</a>`);
}

function inlineText(text: string) {
  return text
    .replace(/\*\*(.+?)\*\*/g, "$1")
    .replace(/`([^`]+)`/g, "$1")
    .replace(/\[([^\]]+)\]\((https:\/\/[^\s)]+)\)/g, "$1 ($2)");
}

type Block = { kind: "list" | "paragraph"; items: string[] };

function blocks(markdown: string): Block[] {
  const result: Block[] = [];
  let paragraph: string[] = [];
  let list: string[] | null = null;
  const flushParagraph = () => {
    if (paragraph.length) result.push({ kind: "paragraph", items: [paragraph.join(" ")] });
    paragraph = [];
  };
  const flushList = () => {
    if (list?.length) result.push({ kind: "list", items: list });
    list = null;
  };

  for (const raw of markdown.split("\n")) {
    const line = raw.trim();
    const bullet = /^[-*+]\s+(.*)$/.exec(line);
    if (bullet) {
      flushParagraph();
      list ??= [];
      list.push(bullet[1]);
    } else if (!line) {
      flushParagraph();
      flushList();
    } else if (list) {
      // A wrapped continuation of the previous bullet.
      list[list.length - 1] += ` ${line}`;
    } else {
      paragraph.push(line);
    }
  }
  flushParagraph();
  flushList();
  return result;
}

export function notesToHtml(markdown: string, linkColor: string) {
  return blocks(markdown)
    .map((block) =>
      block.kind === "list"
        ? `<ul style="margin:0 0 16px;padding-left:20px">${block.items
            .map((item) => `<li style="margin:0 0 10px">${inlineHtml(item, linkColor)}</li>`)
            .join("")}</ul>`
        : `<p style="margin:0 0 16px">${inlineHtml(block.items[0], linkColor)}</p>`,
    )
    .join("");
}

export function notesToText(markdown: string) {
  return blocks(markdown)
    .map((block) =>
      block.kind === "list"
        ? block.items.map((item) => `- ${inlineText(item)}`).join("\n")
        : inlineText(block.items[0]),
    )
    .join("\n\n");
}
