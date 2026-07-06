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
  tools/                  Read-only verification, release, and prototype utilities
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

## Song Presentation Inspection Prototype

`tools\InspectSongPresentations` is a read-only prototype for inspecting local song PowerPoint files before a real Songs feature is added. It does not import songs, modify PowerPoint files, or touch the MessageFlow database.

The prototype scans:

```text
D:\SONG PRESENTATION
D:\SONG PRESENTATION\choir
```

It writes report and sample extraction files to:

```text
D:\MessageFlow Archive\SongImportTest
```

Run it from the repository root:

```powershell
dotnet run --project tools\InspectSongPresentations
```

See `docs\SONG_PRESENTATION_INSPECTION.md` for details.

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
- Bible translations
- Bible books
- Bible verses

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

## Bible Module

MessageFlow includes a local Bible module for OpenLP-style preview and projection. It does not download Bible files automatically. Import uses a local CSV file selected by the operator.

Supported local CSV format:

```text
book,chapter,verse,text
Genesis,1,1,"In the beginning God created the heaven and the earth."
John,3,16,"For God so loved the world..."
Romans,1,23,"And changed the glory of the uncorruptible God..."
```

Import workflow:

1. Open `Tools`.
2. Click `Import Bible`.
3. Enter translation name, abbreviation, and language.
4. Browse to a local CSV file.
5. Click `Preview`.
6. Review verses found, invalid rows, and the first 10 sample verses.
7. Click `Start Import`.

If the selected abbreviation, such as `KJV`, already exists, MessageFlow asks before replacing that translation's Bible verses. Sermon data, source data, favorites, and projection history are not changed by Bible import.

Bible search supports references and keywords in the selected translation:

```text
John 3:16
Romans 1
Romans 1:23
Daniel 5:23
1 Corinthians 13
1 Cor 13:4
Psalm 23
love
faith
beginning
```

Bible projection workflow:

- Select the `Bible` tab.
- Search by reference or keyword.
- Select a verse to preview it.
- Click `Project` to send the verse to the existing black projection window.
- `Previous` and `Next` move within the selected chapter.
- `Copy` copies the reference, translation abbreviation, and verse text.
- Bible favorites are intentionally deferred and show `Bible favorites are coming soon.`

## Search and Bible UX Workflow

MessageFlow separates operator work into clear modes:

- `Search` is for sermons and document paragraphs.
- `Bible` is for Bible references, keyword lookup, preview, and verse projection.
- `Favorites` and `History` are sermon paragraph collections.
- `Tools` is for local sources, Bible CSV import, and database backup/restore.

The main workflow is:

```text
Search > Preview > Project
Bible  > Preview > Project
```

Search mode keeps paragraph labels, paragraph counts, and Previous/Next Paragraph controls. Bible mode keeps Bible result counts, Bible Preview text, selected translation details, and Previous/Next Verse controls. Switching modes should not visually mix sermon labels with Bible labels.

Bible import remains local CSV only. MessageFlow does not scrape websites or download Bible files automatically.

Screenshot placeholders for future documentation:

```text
docs/screenshots/search-preview-project.png
docs/screenshots/bible-preview-project.png
docs/screenshots/import-bible-preview.png
```

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
- Bible tab for local Bible search, preview, copy, projection, and verse navigation
- Borderless black projection window with title, paragraph number, and large centered text

Keyboard shortcuts:

- `Enter` in the search box projects the best matching paragraph
- `Ctrl+F` focuses the search box
- `Ctrl+P` projects the selected paragraph
- `Ctrl+C` copies the selected paragraph
- `Right Arrow` moves to the next paragraph
- `Left Arrow` moves to the previous paragraph
- In the projection window, `Right Arrow` and `Left Arrow` move between paragraphs
- When a Bible verse is selected, `Right Arrow` and `Left Arrow` move between verses in the same chapter
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

Ewald Frank sermons and circular letters should be registered as separate sources. Ewald Frank sermons use `Sermon PDF Collection`; Ewald Frank circular letters use `Circular Letter`. Both source types use the local PDF importer. When the selected Ewald Frank source is repaired, MessageFlow safely changes that source type to `Circular Letter` if circular letter filenames are detected.

If an Ewald Frank test source was imported before these rules existed, select that source in Tools > Sources and click `Repair Source Metadata`. The repair updates only the selected Ewald Frank source's title, code, year, date, author, and source type. It does not change paragraph text, favorites, projection history, or Brother Branham sermons.

## Ewald Frank Content Plan

Ewald Frank sermons and Ewald Frank circular letters are different content categories and should be imported as separate sources.

Use local PDF folders only. MessageFlow must not scrape websites or download content.

Recommended production sources:

```text
Display Name: Ewald Frank Sermons
Source Type: Sermon PDF Collection
D:\Ewald Frank\Sermons\PDF

Display Name: Ewald Frank Circular Letters
Source Type: Circular Letter
D:\Ewald Frank\Circular Letters\PDF
```

Circular Letters should be imported as Source Type: Circular Letter. Sermons, preachings, services, broadcasts, and sermon transcripts should be imported as Source Type: Sermon PDF Collection.

Always test with 2 or 3 PDFs before importing many files. Always use Import Preview before clicking Start Import, and check the sample parsed metadata for title, code, year, author, source type, and status.

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
- Bible tab opens and shows `No Bible translations imported yet.` when empty.
- Import Bible previews local CSV files before writing to the database.
- Bible search finds references such as `John 3:16` after a translation is imported.
- Bible projection, Copy, Next, and Previous work for selected Bible verses.
- Existing Quick Project sermon examples still work after Bible reference support is added.

## Current Status

- Solution and projects are created.
- Project references are wired.
- EF Core SQLite, hosting, and PdfPig packages are referenced.
- Sermon database entities and interfaces are in place.
- EF Core `DbContext`, relationships, seed data, and initial migration are in place.
- Local SQLite database path is `database/messageflow.db`.
- Importer scans local PDFs, extracts text from positioned PdfPig words, creates sermons and paragraphs, skips duplicates, supports `--force` and `--reset`, prints extraction diagnostics, and logs errors.
- Search service supports title, code, year, paragraph number, keyword, and partial keyword search with SQLite FTS5.
- Bible schema, local CSV import preview, Bible search, Bible projection, copy, and verse navigation are in place.
- WPF app has a working dark-theme interface for searching, browsing paragraphs, Bible references, copying text, favorites, history, backup/restore, source management, and projection.

## Next Development Step

Run the manual QA checklist on the actual media-room machine and projector before using the app in a live service.
