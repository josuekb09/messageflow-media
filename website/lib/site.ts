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
  installerSize: "~628 MB",
  installerSha256:
    "10A2F080BCC27BA1629EC172DAF83597BD665CAB03FDA64A206FCD26E3B53FCB",
  supportEmail: "kabuyatambwe03@gmail.com",
  ccEmail: "Paulinkabeya@gmail.com",
} as const;
