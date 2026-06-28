# MessageFlow

MessageFlow is a Windows desktop application for a church media room. Its goal is to help operators quickly search sermons, books, letters, and Bible references, then project selected text onto church screens.

This repository is the project foundation for local sermon storage, PDF import, search, and projection workflows. It does not scrape websites or download copyrighted material.

## Local Content Policy

MessageFlow works only with files that already exist on this computer. The local Brother William Marrion Branham sermon PDFs are expected at:

```text
D:\Br William Marrion Branham\PDF
```

The importer must read from that local folder and its year subfolders only. It must not scrape websites or automatically download copyrighted content.

## Project Structure

```text
MessageFlow/
  src/
    MessageFlow.App/       WPF desktop application
    MessageFlow.Core/      Domain models and interfaces
    MessageFlow.Data/      SQLite database and EF Core infrastructure
    MessageFlow.Importer/  Console app for importing local PDFs
    MessageFlow.Search/    Search service
  database/                Local database location
  docs/                    Project documentation
```

## Technology

- C#
- .NET WPF desktop app
- SQLite local database
- Entity Framework Core
- PdfPig for PDF text extraction
- Clean architecture style

## Build

From the repository root:

```powershell
dotnet restore MessageFlow.sln
dotnet build MessageFlow.sln
```

## Database

MessageFlow uses SQLite through Entity Framework Core. The development database is stored at:

```text
database/messageflow.db
```

The current schema stores:

- Authors
- Content sources
- Sermons
- Sermon paragraphs
- Import logs
- Favorite paragraphs
- Projection history

The initial migration seeds this author:

```text
FullName: William Marrion Branham
DisplayName: Brother Branham
```

If `dotnet ef` is not installed, install the matching EF Core tool once:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.9
```

To create a new migration after changing entity classes or `MessageFlowDbContext`:

```powershell
dotnet ef migrations add YourMigrationName --project src\MessageFlow.Data\MessageFlow.Data.csproj --startup-project src\MessageFlow.Data\MessageFlow.Data.csproj --output-dir Migrations
```

To create or update the local SQLite database:

```powershell
dotnet ef database update --project src\MessageFlow.Data\MessageFlow.Data.csproj --startup-project src\MessageFlow.Data\MessageFlow.Data.csproj
```

## PDF Importer

The importer reads local PDF files only. It scans the given folder recursively, including year folders such as:

```text
D:\Br William Marrion Branham\PDF\1947
D:\Br William Marrion Branham\PDF\1948
D:\Br William Marrion Branham\PDF\1965
```

It extracts text with PdfPig, creates one `Sermon` record per PDF, splits the text into readable `SermonParagraph` records, and writes import status/errors to `ImportLogs`. Text extraction rebuilds page text from positioned PDF words so spaces between words are preserved more reliably than raw PDF page text.

When a paragraph starts with a real sermon paragraph number, such as `4 Well, I would say this...`, the importer stores `4` in `SermonParagraph.ParagraphNumber` and stores clean paragraph text without the leading number.

Run the importer with the default local PDF folder:

```powershell
dotnet run --project src\MessageFlow.Importer
```

Run the importer with an explicit folder:

```powershell
dotnet run --project src\MessageFlow.Importer -- "D:\Br William Marrion Branham\PDF"
```

Re-import files that already exist in the database:

```powershell
dotnet run --project src\MessageFlow.Importer -- "D:\Br William Marrion Branham\PDF" --force
```

Clear imported sermons and paragraphs, then re-import all local PDFs with the improved text extraction:

```powershell
dotnet run --project .\src\MessageFlow.Importer -- --reset "D:\Br William Marrion Branham\PDF"
```

Show importer help:

```powershell
dotnet run --project src\MessageFlow.Importer -- --help
```

The importer skips already imported PDFs unless `--force` is passed. Use `--reset` when you want to rebuild the sermon and paragraph tables from the local PDF collection. It continues after individual file errors and records those errors in `ImportLogs`.

## Search

`MessageFlow.Search` contains the sermon search system:

- `ISermonSearchService`
- `SermonSearchService`
- `SearchResult`
- `SermonSearchQuery`

The search service supports:

- Sermon title
- Sermon code
- Year
- Paragraph number
- Keyword search inside paragraph text
- Partial keyword search

SQLite FTS5 is used for paragraph keyword search through the `SermonParagraphSearch` virtual table. The service falls back to `LIKE` if the FTS table is unavailable.

Simple one-box search:

```powershell
dotnet run --project src\MessageFlow.Importer -- search "faith"
```

Search by title:

```powershell
dotnet run --project src\MessageFlow.Importer -- search --title "Seed"
```

Search by sermon code:

```powershell
dotnet run --project src\MessageFlow.Importer -- search --code 65-0429
```

Search by year and keyword:

```powershell
dotnet run --project src\MessageFlow.Importer -- search --year 1965 --keyword "rapture"
```

Search by paragraph number:

```powershell
dotnet run --project src\MessageFlow.Importer -- search --paragraph 12 --limit 10
```

Partial keyword search:

```powershell
dotnet run --project src\MessageFlow.Importer -- search --keyword fait
```

Search by sermon code or title plus paragraph number:

```powershell
dotnet run --project src\MessageFlow.Importer -- search "47-0412 4"
dotnet run --project src\MessageFlow.Importer -- search "Faith Is The Substance 4"
```

## Search Performance and Indexing

MessageFlow keeps search fast with normal SQLite indexes and optional SQLite FTS5 full-text search.

Startup database repair safely creates missing indexes with `CREATE INDEX IF NOT EXISTS`, including:

- Sermon title, sermon code, year, author, and content source indexes
- Sermon code plus year composite index
- Sermon paragraph sermon id, paragraph number, search text, and sermon id plus paragraph number indexes

If SQLite FTS5 is available, startup repair also prepares `SermonParagraphsFts` for paragraph search. The FTS table stores paragraph search text plus sermon title/code metadata and is rebuilt only when its row count does not match `SermonParagraphs`.

If FTS5 is unavailable, MessageFlow keeps working with indexed SQLite `LIKE` searches. Search status messages in the app include result count and elapsed milliseconds so operators can see when a query completed.

Quick Project ranking prioritizes:

- Exact sermon code with exact paragraph number
- Exact title phrase with exact paragraph number
- Title match with paragraph number
- Sermon code match
- Title match
- Paragraph keyword match

## WPF App

Run the desktop app:

```powershell
dotnet run --project src\MessageFlow.App
```

The first WPF screen includes:

- Top search bar with author and year filters
- Projection font-size control: Small, Medium, Large, Extra Large
- Sermon result list
- Paragraph result list
- Full paragraph preview
- Copy, project, next/previous paragraph, and favorite actions
- Borderless black projection window with title, paragraph number, and large centered text

Keyboard shortcuts:

- `Enter` in the search box projects the best matching paragraph
- `Ctrl+F` focuses the search box
- `Ctrl+P` projects the selected paragraph
- `Ctrl+C` copies the selected paragraph
- `Right Arrow` moves to the next paragraph
- `Left Arrow` moves to the previous paragraph
- In the projection window, `Right Arrow` and `Left Arrow` move between paragraphs
- In the projection window, `Esc` closes projection
- In the projection window, `F11` toggles fullscreen

## Importing a New Local PDF Source Safely

Use Tools > Sources to register and import future local PDF collections such as Ewald Frank sermons.

Safe import workflow:

1. Click `Add New Source`.
2. Enter a display name, choose `Sermon PDF Collection`, and select the local folder.
3. Click `Import Source`.
4. Review the Import Preview dialog before any database write happens.
5. Confirm the source name, folder, PDFs found, already imported files, ready-to-import files, and invalid/missing file count.
6. Click `Cancel` to stop with no database changes, or `Start Import` to import new local PDFs.

MessageFlow does not scrape websites or download content. Import Source scans the selected local folder only. Existing sermons are skipped and are not deleted.

## Non-Branham Source Metadata

Brother Branham PDFs continue to use the established Branham sermon metadata parser.

For other local PDF sources, MessageFlow uses safer filename-based metadata:

- The PDF filename becomes the main document title after removing `.pdf`, underscores, and extra spacing.
- Circular letter filenames with year/month patterns become titles such as `Circular Letter - December 2020`.
- Circular letter codes use `CL-yyyy-MM` when a month is detected, or `CL-yyyy` when only a year is detected.
- The document year comes from the filename when possible. MessageFlow does not use the current year as a fallback for non-Branham circular letters.
- If the source display name contains `Ewald Frank`, imported documents are linked to `Ewald Frank` with display name `Brother Frank`.

Ewald Frank circular letters can be registered as either `Sermon PDF Collection` for initial testing or `Circular Letter`. Both source types use the local PDF importer. When the selected Ewald Frank source is repaired, MessageFlow safely changes that source type to `Circular Letter` if circular letter filenames are detected.

If an Ewald Frank test source was imported before these rules existed, select that source in Tools > Sources and click `Repair Source Metadata`. The repair updates only the selected Ewald Frank source's title, code, year, date, author, and source type. It does not change paragraph text, favorites, projection history, or Brother Branham sermons.

Useful circular letter search examples:

```text
Circular Letter
Circular Letter April 2020
CL-2020-04
Ewald Frank
Brother Frank
```

MessageFlow keeps circular letter search fast by maintaining SQLite indexes and an optional `SermonParagraphsFts` full-text search table. The FTS table includes paragraph text plus title, code, year, author, source name, and source type. Startup repair and source import/metadata repair safely rebuild the FTS data when needed.

## Manual QA Checklist

Before using MessageFlow in a live service, run this quick manual checklist:

- Search works with title, sermon code, year, paragraph number, and keyword queries.
- Quick Project works by typing a title or code plus paragraph number and pressing `Enter`.
- Next and Previous move to the adjacent paragraph in the same sermon.
- Projection opens one borderless projection window and updates the existing window.
- Projection text is readable, centered, and split into pages when needed.
- Favorites can be added, removed, selected, and double-clicked for projection.
- History records every projected paragraph and shows the most recent item first.
- Backup Database creates a `.db` file and shows the full backup path.
- Restore Database asks for confirmation, creates a safety backup, and reloads app data.
- Tools tab shows Sources and Database sections without clipped buttons.
- Add New Source dialog shows all fields and clean Source Type labels.
- Import Source asks for confirmation and skips already imported PDFs.
- Author, Year, and Projection Font Size filters show clean labels and stay usable.

## Current Status

- Solution and projects are created.
- Project references are wired.
- EF Core SQLite, hosting, and PdfPig packages are referenced.
- Sermon database entities and interfaces are in place.
- EF Core `DbContext`, relationships, seed data, and initial migration are in place.
- Local SQLite database path is `database/messageflow.db`.
- Importer scans local PDFs, extracts text from positioned PdfPig words, creates sermons and paragraphs, skips duplicates, supports `--force` and `--reset`, prints extraction diagnostics, and logs errors.
- Search service supports title, code, year, paragraph number, keyword, and partial keyword search with SQLite FTS5.
- WPF app has a working dark-theme interface for searching, browsing paragraphs, copying text, favorites, history, backup/restore, source management, and projection.

## Next Development Step

Run the manual QA checklist on the actual media-room machine and projector before using the app in a live service.
