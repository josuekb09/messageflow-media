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
  githubReleaseTag: "v1.0.8",
  // The installer is deliberately served by GitHub Releases, not by the
  // website. `latest` keeps every Download for Windows button on the newest
  // published installer without a website redeploy.
  downloadHref:
    "https://github.com/josuekb09/messageflow-media/releases/latest/download/MessageFlowMediaSetup.exe",
  downloadFileName: "MessageFlowMediaSetup.exe",
  installerSize: "~587 MB",
  installerSha256:
    "163B4141CBDC7AB1166D601562575CEA1165C06B1AA288B4A191154A1861B395",
  supportEmail: "kabuyatambwe03@gmail.com",
  ccEmail: "Paulinkabeya@gmail.com",
} as const;
