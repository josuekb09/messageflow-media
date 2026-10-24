# MessageFlow Media

Free Windows software for the church operator desk. Search and project sermons, Scripture, and hymns offline in **English**, **French**, and **Kiswahili**. The congregation sees only the projection window; the operator keeps full control on the computer.

Current public release: **v1.0.9** (October 2026). Free for church use, not for sale.

Website: [www.messageflow.tech](https://www.messageflow.tech)  
Download: [GitHub Releases v1.0.9](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.9)  
Report a problem: [www.messageflow.tech/feedback](https://www.messageflow.tech/feedback)

## What's new in 1.0.9

- **Paragraph numbers match the books.** Sermon paragraphs follow the numbers printed in the English, French, and Kiswahili books.
- **Cleaner sermon text.** Page headings that had slipped into paragraphs are gone, and words that had run together read as in the book.
- **Songs word for word.** Songs match their PowerPoint slides and the French hymn book.
- **Projector plugged in or out during the service.** The projection moves to the right screen by itself; the operator controls stay on the computer.
- **KJV:** Galatians 6:18 and Ephesians 6:24 have their final "Amen." back.

Every sermon, song, and Bible verse was compared with its original source file: see [docs/SOURCE_ACCURACY_AUDIT.md](docs/SOURCE_ACCURACY_AUDIT.md).

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

1. Open the [v1.0.9 release](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.9) and download `MessageFlowMediaSetup.exe` (or use the Download button on [www.messageflow.tech/download](https://www.messageflow.tech/download)).
   - Size: about 522 MB (547,649,563 bytes)
   - SHA-256: `AFD70ED86A177D795C964E71AC743B73AEC0C2745D5F0D80D2A6C999FBDBBC91`. To check it, run `Get-FileHash .\MessageFlowMediaSetup.exe` in PowerShell.
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
5. If the projector is unplugged or plugged in during the service, MessageFlow moves the projection by itself: to the projector when it is connected, to a normal window on the computer when it is not.

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
