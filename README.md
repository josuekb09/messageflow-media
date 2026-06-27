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
- Sermons
- Sermon paragraphs
- Import logs

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

- `Ctrl+Enter` projects the selected paragraph
- `Ctrl+C` copies the selected paragraph
- `Alt+Right` moves to the next paragraph
- `Alt+Left` moves to the previous paragraph
- In the projection window, `Right Arrow` and `Left Arrow` move between paragraphs
- In the projection window, `Esc` closes projection

## Current Status

- Solution and projects are created.
- Project references are wired.
- EF Core SQLite, hosting, and PdfPig packages are referenced.
- Sermon database entities and interfaces are in place.
- EF Core `DbContext`, relationships, seed data, and initial migration are in place.
- Local SQLite database path is `database/messageflow.db`.
- Importer scans local PDFs, extracts text from positioned PdfPig words, creates sermons and paragraphs, skips duplicates, supports `--force` and `--reset`, prints extraction diagnostics, and logs errors.
- Search service supports title, code, year, paragraph number, keyword, and partial keyword search with SQLite FTS5.
- WPF app has a working dark-theme MVP interface for searching, browsing paragraphs, copying text, and opening a projection window.

## Next Development Step

Re-run the importer with `--reset` so existing paragraphs are rebuilt with the improved spacing and paragraph cleanup.
