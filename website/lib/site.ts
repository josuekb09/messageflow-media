const githubReleaseTag = "v1.0.8";

export const site = {
  name: "MessageFlow Media",
  tagline: "Sermon, Bible, and song projection for churches",
  description:
    "Free offline Windows software for church media teams. Search and project Brother Branham's sermons, Brother Frank's publications, the Bible, and songs in English, French, and Kiswahili.",
  url: "https://www.messageflow.tech",
  version: "1.0.8",
  releaseDate: "October 2026",
  // Same date as releaseDate, for month names in French and Kiswahili.
  releaseMonth: "2026-10",
  platform: "Windows 10 / 11",
  exeName: "MessageFlow.App.exe",
  githubReleaseTag,
  // The installer is deliberately served by GitHub Releases, not by the
  // website. `latest` keeps every Download for Windows button on the newest
  // published installer without a website redeploy; update the size and
  // SHA-256 below with each release.
  downloadHref:
    "https://github.com/josuekb09/messageflow-media/releases/latest/download/MessageFlowMediaSetup.exe",
  downloadFileName: "MessageFlowMediaSetup.exe",
  installerSize: "~587 MB",
  // dist\publish for 1.0.8: 1,840,606,729 bytes (app 0.2 GB + library 1.5 GB).
  installedSize: "~1.7 GB",
  requiredFreeSpace: "2 GB",
  installerSha256:
    "163B4141CBDC7AB1166D601562575CEA1165C06B1AA288B4A191154A1861B395",
  // Counts in the bundled 1.0.8 database. Brother Frank publications exist in
  // English only; the French and Kiswahili sermons are all Brother Branham.
  library: {
    branham: { en: 1203, fr: 384, sw: 622 },
    frank: { en: 85 },
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
