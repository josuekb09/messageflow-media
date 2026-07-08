# MessageFlow Media

MessageFlow Media is a free Windows desktop app for church media rooms. It helps an operator search and project sermons, King James Version Bible verses, and songs during church services.

The app is designed for offline use with a second screen, projector, or TV. The congregation sees only the projected text while the operator controls the service from the computer.

## Free Church Use

MessageFlow Media is distributed free of charge for church use.

- Not for sale.
- No paid subscription.
- No ads.
- No in-app purchases.
- Do not sell this software or bundled content.
- Do not use the software or bundled content for fundraising.

See [NOTICE.md](NOTICE.md) and [docs/PERMISSION_AND_CONTENT_NOTICE.md](docs/PERMISSION_AND_CONTENT_NOTICE.md) before redistributing the app or installer.

## Features

- Sermon search and projection
- King James Version Bible search and projection
- Song search and projection
- Favorites and projection history
- Second-screen projection for a projector or TV
- Offline use after installation
- Local database storage

## Install From GitHub Releases

1. Open the GitHub repository Releases page.
2. Download `MessageFlowMediaSetup.exe` from the latest release.
3. Run the installer on the church media computer.
4. Launch `MessageFlow Media` from the desktop shortcut or Start menu.

The installer is attached to GitHub Releases. It is not intended to be committed directly into normal git source history.

## Use With a Projector or TV

1. Connect the projector or TV to the Windows computer.
2. Press `Windows + P`.
3. Choose `Extend`.
4. Open MessageFlow Media.
5. Use the projection test from the app before service.
6. Confirm the projector or TV shows only the projection window, not the operator controls.

## Screenshots

Screenshots will be added here before public release:

- Search, preview, and project
- Bible projection
- Song projection
- Second-screen setup

Suggested paths:

```text
docs/screenshots/search-preview-project.png
docs/screenshots/bible-preview-project.png
docs/screenshots/song-preview-project.png
docs/screenshots/second-screen-projection.png
```

## Build From Source

Requirements:

- Windows
- .NET SDK

From the repository root:

```powershell
dotnet restore MessageFlow.sln
dotnet build MessageFlow.sln
```

Run the WPF app:

```powershell
dotnet run --project src\MessageFlow.App
```

## Project Structure

```text
MessageFlow/
  src/
    MessageFlow.App/       WPF desktop application
    MessageFlow.Core/      Domain models and interfaces
    MessageFlow.Data/      SQLite database and EF Core infrastructure
    MessageFlow.Importer/  Local PDF importer
    MessageFlow.Search/    Search services
  tools/                   Verification, audit, release, and installer tools
  docs/                    Public documentation and release planning
  database/                Local production database location
```

## Content Notice

MessageFlow Media is a projection and search tool. Sermon, Bible, and song content should only be distributed or imported when the church has permission or an allowed free-use basis for that content.

Bundled sermon content is included only under permission or allowed free-use conditions. Churches should respect Voice of God Recordings and all original content owners. If a church has concerns about bundled content, it should use its own authorized local content instead.

MessageFlow Media is not affiliated with or endorsed by Voice of God Recordings unless explicitly stated in writing.

## Known Limitations

- Windows desktop is the first supported platform.
- Microsoft Store distribution is planned later.
- Android and iOS mobile apps are planned later as separate apps.
- Mobile versions cannot be produced by uploading the current WPF app directly to mobile stores.

## Release Documentation

- [GitHub release guide](docs/GITHUB_RELEASE_GUIDE.md)
- [Microsoft Store plan](docs/MICROSOFT_STORE_PLAN.md)
- [Store listing draft](docs/STORE_LISTING_DRAFT.md)
- [Mobile app plan](docs/MOBILE_APP_PLAN.md)
- [Privacy policy](docs/PRIVACY_POLICY.md)
- [Versioning](docs/VERSIONING.md)
