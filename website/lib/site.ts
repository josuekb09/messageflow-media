const githubReleaseTag = "v1.0.11";

export const site = {
  name: "MessageFlow Media",
  tagline: "Sermon, Bible, and song projection for churches",
  description:
    "Free offline Windows software for church media teams. Search and project Brother Branham's sermons, Brother Frank's documents (English and French), the Bible, and songs in English, French, and Kiswahili.",
  url: "https://www.messageflow.tech",
  version: "1.0.11",
  releaseDate: "November 2026",
  // Same date as releaseDate, for month names in French and Kiswahili.
  releaseMonth: "2026-11",
  platform: "Windows 10 / 11",
  exeName: "MessageFlow.App.exe",
  githubReleaseTag,
  githubRepo: "josuekb09/messageflow-media",
  // The installer is deliberately served by GitHub Releases, not by the
  // website. Every Download for Windows button points to the installer of
  // this exact release tag; update the tag, the version and the size below
  // with each release. The SHA-256 and exact byte counts live only in the
  // GitHub release notes and the README, never on the public site.
  downloadHref: `https://github.com/josuekb09/messageflow-media/releases/download/${githubReleaseTag}/MessageFlowMediaSetup.exe`,
  installerSize: "697 MB",
  requiredFreeSpace: "3 GB",
  // Counts in the bundled 1.0.11 database. Brother Frank documents exist in
  // English (148, of which 63 are new broadcast transcriptions) and French (700,
  // all new broadcast transcriptions); there are none in Kiswahili yet. The
  // Brother Branham counts include 3 English and 32 French broadcasts of his sermons.
  library: {
    branham: { en: 1206, fr: 416, sw: 622 },
    frank: { en: 148, fr: 700 },
    bibles: {
      en: "King James Version (KJV)",
      fr: "Louis Segond 1910 (LSG)",
      sw: "Biblia Takatifu (SWHULB)",
    },
    songs: { en: 357, fr: 499, sw: 281 },
  },
  supportEmail: "kabuyatambwe03@gmail.com",
  ccEmail: "Paulinkabeya@gmail.com",
} as const;
