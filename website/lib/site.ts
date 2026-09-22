export const site = {
  name: "MessageFlow Media",
  tagline: "The modern standard for church media projection",
  description:
    "Lightning-fast offline Windows software to search and project sermons, Bibles, and multilingual songbooks in English, French, and Kiswahili — no internet required.",
  url: "https://www.messageflow.tech",
  version: "1.0.5",
  releaseDate: "September 2026",
  platform: "Windows 10 / 11",
  exeName: "MessageFlow.App.exe",
  githubReleaseTag: "v1.0.5",
  // The installer is deliberately served by GitHub Releases, not by the
  // website. `latest` keeps every Download for Windows button on the newest
  // published installer without a website redeploy.
  downloadHref:
    "https://github.com/josuekb09/messageflow-media/releases/latest/download/MessageFlowMediaSetup.exe",
  downloadFileName: "MessageFlowMediaSetup.exe",
  installerSize: "~744 MB",
  installerSha256:
    "B580EA3E9861A72EFA7D326641C6F805D225FAAF7B4E6C57A0DCC8EFF828A2AA",
  supportEmail: "kabuyatambwe03@gmail.com",
  ccEmail: "Paulinkabeya@gmail.com",
} as const;
