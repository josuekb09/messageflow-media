# MessageFlow Media

Free Windows software for the church operator desk. Search and project sermons, Scripture, and hymns offline in **English**, **French**, and **Kiswahili**. The congregation sees only the projection window; the operator keeps full control on the computer.

Current public release: **v1.0.8** (October 2026). Free for church use, not for sale.

Website: [www.messageflow.tech](https://www.messageflow.tech)  
Download: [GitHub Releases v1.0.8](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.8)  
Report a problem: [www.messageflow.tech/feedback](https://www.messageflow.tech/feedback)

## What's new in 1.0.8

- **Instant startup.** The app no longer freezes while it opens: the main window appears in about 1–2.5 seconds and search is ready in 2–4 seconds.
- **Projected text always fits the screen.** Every song section, verse, and paragraph is sized for the projector on its own, so A-/A+ on one screen no longer shrinks the next, and no word is ever cut.
- **Swahili line-break fix.** Hymn lines appear on the projector exactly as on their PowerPoint slides, instead of running together.
- **New light theme** in clean white and blue. Dark remains the default.

See [RELEASE_NOTES.md](RELEASE_NOTES.md) for every change.

## Requirements

- Windows 10 or 11, 64-bit
- At least 2 GB free on the install drive (the app and library take about 1.7 GB), plus about 0.6 GB for the downloaded installer
- A projector or TV connected as a second display
- Internet only to download. After installation the app works 100% offline: no account, no sign-in, no data sent anywhere.

## Library in this build

| | English | French | Kiswahili |
|---|---|---|---|
| Sermons and publications | 1,288 (1,203 Brother Branham + 85 Brother Frank) | 384 Brother Branham | 622 Brother Branham |
| Bibles | KJV | Louis Segond 1910 (LSG) | SWHULB (Biblia Takatifu) |
| Songs | 357 | 499 French hymns | 281 Swahili hymns |

French hymns that have a refrain use **verse → chorus → verse → chorus**.

## Features

- Full-text sermon search, filtered by the selected language
- Bible lookup with KJV, Louis Segond, and SWHULB
- Song search and projection with verse/chorus sections
- Favorites and projection history
- Dual-screen projection (operator chrome vs congregation screen)
- Projected text fits each screen automatically, with A- / A+ / Auto for manual size
- Optional light theme (white and blue); dark remains the default
- Interface in English, French, and Kiswahili
- Keyboard shortcuts: `Ctrl+F` search, `Ctrl+P` project, arrow keys to move
- 100% offline after installation: no account, no ads, no subscription

## Free church use

- Not for sale
- No paid subscription, ads, or in-app purchases
- Do not sell this software or bundled content
- Do not use the software or bundled content for fundraising

See [NOTICE.md](NOTICE.md) and [docs/PERMISSION_AND_CONTENT_NOTICE.md](docs/PERMISSION_AND_CONTENT_NOTICE.md) before redistributing the app or installer.

## Install on Windows 10 / 11 (64-bit)

1. Open the [v1.0.8 release](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.8) and download `MessageFlowMediaSetup.exe` (or use the Download button on [www.messageflow.tech/download](https://www.messageflow.tech/download)).
   - Size: about 587 MB (615,471,332 bytes)
   - SHA-256: `163B4141CBDC7AB1166D601562575CEA1165C06B1AA288B4A191154A1861B395`. To check it, run `Get-FileHash .\MessageFlowMediaSetup.exe` in PowerShell.
2. Run the installer. No administrator permission is needed. If Windows says it protected your PC, choose **More info**, then **Run anyway**.
3. Keep the suggested folder or choose another local folder with at least 2 GB free. The installer suggests `MessageFlowMedia` on the local data drive with the most free space, for example `E:\MessageFlowMedia` (`%LOCALAPPDATA%\Programs\MessageFlowMedia` on a PC with only C:). Avoid OneDrive-synchronized folders such as Documents; the installer warns if one is chosen.
4. Launch **MessageFlow Media** from the desktop shortcut or Start menu.
5. Choose English, Français, or Kiswahili. Search a sermon, verse, or hymn. Press `Ctrl+P` to project.

The installer is attached to GitHub Releases (or kept in `dist\` locally). It is not committed to git.

## Connect a projector or TV

1. Connect the projector or TV to the computer (HDMI or the church display system).
2. Press `Win+P` and choose **Extend**, not Duplicate.
3. Open MessageFlow Media and project something with `Ctrl+P`.
4. Confirm the projector shows only the projection window, while the operator controls stay on the computer screen.

## Report a problem

Use the form at [www.messageflow.tech/feedback](https://www.messageflow.tech/feedback) for bugs, questions, and feature requests. If something went wrong at startup, include `logs\app-startup.log` from the install folder.

## Build from source

Requirements: Windows, .NET SDK.

```powershell
dotnet restore MessageFlow.sln
dotnet build MessageFlow.sln -c Release
dotnet run --project src\MessageFlow.App
```

Website (from `website/`):

```powershell
npm install
npm run build
npm run dev
```

Vercel: `vercel.json` cannot set `rootDirectory` (not in the official schema). On the Vercel project, set **Root Directory** to `website` (Settings → General, or the project API). The **messageflow-media** project is already set this way. A root-level `vercel.json` fallback cannot deploy this Next.js app, because Vercel looks for `next` in the repository-root `package.json`. Production: https://www.messageflow.tech — HTTPS on that hostname needs the apex A record `76.76.21.21` and `www` CNAME `cname.vercel-dns.com` (see `website/README.md`).

## Project structure

```text
MessageFlow/
  src/           WPF app, core, data, search, importer
  tools/         Songbook importers, audits, installer scripts
  website/       Next.js site (black, white, and gold)
  docs/          Release, store, and content notices
  database/      Local production SQLite location (not in git)
```

## Content notice

MessageFlow Media is a projection and search tool. Sermon, Bible, and song content should only be distributed or imported when the church has permission or an allowed free-use basis for that content.

Bundled sermon content is included only under permission or allowed free-use conditions. Churches should respect Voice of God Recordings and all original content owners.

MessageFlow Media is not affiliated with or endorsed by Voice of God Recordings unless explicitly stated in writing.

## Documentation

- [GitHub release guide](docs/GITHUB_RELEASE_GUIDE.md)
- [Privacy policy](docs/PRIVACY_POLICY.md)
- [Versioning](docs/VERSIONING.md)
- [Permission and content notice](docs/PERMISSION_AND_CONTENT_NOTICE.md)
- [Microsoft Store plan](docs/MICROSOFT_STORE_PLAN.md)
- [Mobile app plan](docs/MOBILE_APP_PLAN.md)
