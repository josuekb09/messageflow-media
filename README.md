# MessageFlow Media

Free Windows software for the church operator desk. Search and project sermons, Scripture, and hymns offline in **English**, **French**, and **Kiswahili**. The congregation sees only the projection window; the operator keeps full control on the computer.

Current public release: **v1.0.3** (August 2026).

Website: [messageflow.app](https://messageflow.app)  
Download: [GitHub Releases v1.0.3](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.3)

## Library in this build

| | English | French | Kiswahili |
|---|---|---|---|
| Sermons (William Marrion Branham) | 1,208 | 384 | 622 |
| Bibles | KJV | Louis Segond 1910 (LSG) | SWHULB (Biblia Takatifu) |
| Songs | 357 | 499 French hymns | 281 Swahili hymns |

French hymns that have a refrain use **verse → chorus → verse → chorus**.

## Features

- Full-text sermon search, filtered by the selected language
- Bible lookup with KJV, Louis Segond, and SWHULB
- Song search and projection with verse/chorus sections
- Favorites and projection history
- Dual-screen projection (operator chrome vs congregation screen)
- Optional light theme (white and blue); dark remains the default
- Keyboard shortcuts: `Ctrl+F` search, `Ctrl+P` project, arrow keys to move
- Offline use after installation — no account, no ads, no subscription

## Free church use

- Not for sale
- No paid subscription, ads, or in-app purchases
- Do not sell this software or bundled content
- Do not use the software or bundled content for fundraising

See [NOTICE.md](NOTICE.md) and [docs/PERMISSION_AND_CONTENT_NOTICE.md](docs/PERMISSION_AND_CONTENT_NOTICE.md) before redistributing the app or installer.

## Install on Windows 10 / 11 (64-bit)

1. Open the [v1.0.3 release](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.3) and download `MessageFlowMediaSetup.exe` (or use the Download button on [messageflow.app/download](https://messageflow.app/download)).
2. Run the installer. Prefer installing on drive **D:** if that is the church media disk.
3. Connect the projector or TV. Press `Win+P` and choose **Extend**.
4. Launch **MessageFlow Media** from the desktop shortcut or Start menu.
5. Choose English, Français, or Kiswahili. Search a sermon, verse, or hymn. Press `Ctrl+P` to project.

The installer is attached to GitHub Releases (or kept in `dist\` locally). It is not committed to git.

## Use with a projector or TV

1. Connect the display to the Windows computer.
2. Press `Win+P` and choose `Extend`.
3. Open MessageFlow Media.
4. Confirm the projector shows only the projection window, not the operator controls.

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

Vercel: `vercel.json` cannot set `rootDirectory` (not in the official schema). On the Vercel project, set **Root Directory** to `website` (Settings → General, or the project API). The **messageflow-media** project is already set this way. A root-level `vercel.json` fallback cannot deploy this Next.js app, because Vercel looks for `next` in the repository-root `package.json`. Production: https://messageflow-media.vercel.app

## Project structure

```text
MessageFlow/
  src/           WPF app, core, data, search, importer
  tools/         Songbook importers, audits, installer scripts
  website/       Next.js site (white-and-blue SaaS theme)
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
