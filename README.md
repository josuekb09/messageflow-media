# MessageFlow Media

I serve on the media desk at church, and I built MessageFlow Media so that what the congregation sees on the screen is exactly what the minister is reading from the book: the same words, the same paragraph number, in English, French, or Kiswahili. No retyping during the service, no searching through files while everyone waits.

It is free Windows software for the operator desk. You search a sermon, a Bible verse, or a song, press `Ctrl+P`, and it appears on the projector. The congregation sees only the projection window; you keep full control on the computer. It works fully offline.

Current release: **v1.0.9** (October 2026). Free for church use, not for sale.

- Website: [www.messageflow.tech](https://www.messageflow.tech)
- Download: [GitHub Releases v1.0.9](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.9)
- Report a problem: [www.messageflow.tech/feedback](https://www.messageflow.tech/feedback)

## Why I care about the text

When a pastor reads paragraph 36 from the printed book, paragraph 36 on the screen has to be the same paragraph, word for word. For 1.0.9 I compared every sermon, song, and Bible verse in the library with its original source file (the PDFs, the PowerPoint slides, and the Bible source texts) and corrected the library only where the source proved it wrong. The full method and the numbers are in [docs/SOURCE_ACCURACY_AUDIT.md](docs/SOURCE_ACCURACY_AUDIT.md), including what is still not perfect.

What 1.0.9 brought:

- **Paragraph numbers match the books.** Sermon paragraphs follow the numbers printed in the English, French, and Kiswahili books.
- **Cleaner sermon text.** Page headings that had slipped into paragraphs are gone, and words that had run together read as in the book.
- **Songs word for word.** Songs match their PowerPoint slides and the French hymn book.
- **Projector plugged in or out during the service.** The projection moves to the right screen by itself; the operator controls stay on the computer.
- **KJV:** Galatians 6:18 and Ephesians 6:24 have their final "Amen." back.

Every change is listed in [RELEASE_NOTES.md](RELEASE_NOTES.md).

## What's in the library

| | English | French | Kiswahili |
|---|---|---|---|
| Brother Branham sermons | 1,203 | 384 | 622 |
| Brother Frank publications | 85 (English only) | — | — |
| Bible | King James Version (KJV) | Louis Segond 1910 (LSG) | SWHULB (Biblia Takatifu) |
| Songs | 357 | 499 French hymns | 281 Swahili hymns |

French hymns that have a refrain are shown as **verse → chorus → verse → chorus**.

## What it does

- Full-text sermon search, filtered by the selected language
- Bible lookup with KJV, Louis Segond, and SWHULB
- Song search and projection with verse and chorus sections
- Favorites and projection history
- Two screens: the operator works on the computer, the congregation sees only the projection window
- Projected text fits each screen automatically, with A- / A+ / Auto when you want to adjust the size
- An optional light theme (white and blue); dark stays the default
- Interface in English, French, and Kiswahili
- Keyboard shortcuts: `Ctrl+F` to search, `Ctrl+P` to project, the arrow keys to move
- 100% offline after installation: no account, no ads, no subscription

## Requirements

- Windows 10 or 11, 64-bit
- At least 2 GB free on the install drive (the app and library take about 1.7 GB), plus about 0.6 GB for the downloaded installer
- A projector or TV connected as a second display
- Internet only to download. After installation the app works 100% offline: no account, no sign-in, no data sent anywhere.

## How to install

1. Open the [v1.0.9 release](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.9) and download `MessageFlowMediaSetup.exe`, or use the Download button on [www.messageflow.tech/download](https://www.messageflow.tech/download).
2. Run the installer. You do not need administrator permission. If Windows says it protected your PC, choose **More info**, then **Run anyway**.
3. Keep the suggested folder or choose another local folder with at least 2 GB free. The installer suggests `MessageFlowMedia` on the local data drive with the most free space, for example `E:\MessageFlowMedia` (`%LOCALAPPDATA%\Programs\MessageFlowMedia` on a PC with only C:). Please avoid OneDrive-synchronized folders such as Documents; the installer warns you if you pick one.
4. Launch **MessageFlow Media** from the desktop shortcut or the Start menu.
5. Choose English, Français, or Kiswahili, search a sermon, verse, or hymn, and press `Ctrl+P` to project.

### Installer details

| | |
|---|---|
| Version | 1.0.9 |
| File | `MessageFlowMediaSetup.exe` |
| Size | 547,649,563 bytes (about 522 MB) |
| SHA-256 | `AFD70ED86A177D795C964E71AC743B73AEC0C2745D5F0D80D2A6C999FBDBBC91` |

To check your download, run `Get-FileHash .\MessageFlowMediaSetup.exe` in PowerShell and compare the hash. The installer is attached to GitHub Releases; it is not committed to git.

## Connecting the projector or TV

1. Connect the projector or TV to the computer (HDMI or the church display system).
2. Press `Win+P` and choose **Extend**, not Duplicate.
3. Open MessageFlow Media and project something with `Ctrl+P`.
4. Check that the projector shows only the projection window, while the operator controls stay on the computer screen.
5. If the projector is unplugged or plugged in during the service, MessageFlow moves the projection by itself: to the projector when it is connected, to a normal window on the computer when it is not.

## Free church use

I made MessageFlow Media to serve churches, so it stays free:

- Not for sale
- No paid subscription, ads, or in-app purchases
- Do not sell this software or bundled content
- Do not use the software or bundled content for fundraising

Please read [NOTICE.md](NOTICE.md) and [docs/PERMISSION_AND_CONTENT_NOTICE.md](docs/PERMISSION_AND_CONTENT_NOTICE.md) before redistributing the app or the installer.

## Content notice

MessageFlow Media is a projection and search tool. Sermon, Bible, and song content should only be distributed or imported when the church has permission or an allowed free-use basis for that content.

Bundled sermon content is included only under permission or allowed free-use conditions. Churches should respect Voice of God Recordings and all original content owners.

MessageFlow Media is not affiliated with or endorsed by Voice of God Recordings unless explicitly stated in writing.

## License

Copyright (c) 2026 MessageFlow Media project author. MessageFlow Media is distributed free of charge for church use and is not for sale; the full terms are in [NOTICE.md](NOTICE.md).

## Contact

For bugs, questions, or ideas, please write to me through the form at [www.messageflow.tech/feedback](https://www.messageflow.tech/feedback). If something went wrong at startup, include `logs\app-startup.log` from the install folder.

---

## For developers

### Build from source

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

### Project structure

```text
MessageFlow/
  src/           WPF app, core, data, search, importer
  tools/         Songbook importers, audits, installer scripts
  website/       Next.js site (ivory, ink, and gold)
  docs/          Release, store, and content notices
  database/      Local production SQLite location (not in git)
```

### Documentation

- [Source accuracy audit](docs/SOURCE_ACCURACY_AUDIT.md)
- [GitHub release guide](docs/GITHUB_RELEASE_GUIDE.md)
- [Privacy policy](docs/PRIVACY_POLICY.md)
- [Versioning](docs/VERSIONING.md)
- [Permission and content notice](docs/PERMISSION_AND_CONTENT_NOTICE.md)
- [Microsoft Store plan](docs/MICROSOFT_STORE_PLAN.md)
- [Mobile app plan](docs/MOBILE_APP_PLAN.md)
