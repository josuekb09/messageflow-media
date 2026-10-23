const githubReleaseTag = "v1.0.8";

export const site = {
  name: "MessageFlow Media",
  tagline: "Sermon, Bible, and song projection for churches",
  description:
    "Free offline Windows software for church media teams. Search and project Brother Branham's sermons, Brother Frank's publications, the Bible, and songs in English, French, and Kiswahili.",
  url: "https://www.messageflow.tech",
  version: "1.0.8",
  releaseDate: "October 2026",
  platform: "Windows 10 / 11",
  exeName: "MessageFlow.App.exe",
  githubReleaseTag,
  // The installer is deliberately served by GitHub Releases, not by the
  // website. The link is pinned to this release so the button always serves
  // the installer whose size and SHA-256 are shown here; bump the tag, size,
  // and hash together for the next release.
  downloadHref: `https://github.com/josuekb09/messageflow-media/releases/download/${githubReleaseTag}/MessageFlowMediaSetup.exe`,
  downloadFileName: "MessageFlowMediaSetup.exe",
  installerSize: "~587 MB",
  installerSha256:
    "163B4141CBDC7AB1166D601562575CEA1165C06B1AA288B4A191154A1861B395",
  supportEmail: "kabuyatambwe03@gmail.com",
  ccEmail: "Paulinkabeya@gmail.com",
} as const;
