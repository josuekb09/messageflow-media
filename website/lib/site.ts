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
  installerSize: "~795 MB",
  installerSha256:
    "F600098885462530D01FE500BF3D575444BA61FD359A1ADFE9BC4ABD14478FE6",
  supportEmail: "kabuyatambwe03@gmail.com",
  ccEmail: "Paulinkabeya@gmail.com",
} as const;
