# Song Presentation Inspection Prototype

`tools\InspectSongPresentations` is a read-only prototype for checking whether MessageFlow can safely read song PowerPoint files before a real Songs feature is built.

## Safety

- Does not open or modify the MessageFlow database.
- Does not import songs.
- Does not delete, move, or modify PowerPoint files.
- Does not add a Songs tab or app UI.
- Ignores `D:\SONG PRESENTATION\chruch service` for now.

## Source Folders

```text
D:\SONG PRESENTATION
D:\SONG PRESENTATION\choir
```

The tool scans for `.ppt` and `.pptx` files. Office temporary lock files beginning with `~$` are skipped.

## Output

The prototype writes inspection artifacts to:

```text
D:\MessageFlow Archive\SongImportTest
```

Files produced:

```text
song_presentation_inspection_report.txt
song_extracted_samples.csv
song_extracted_samples.json
```

## Run

From the repository root:

```powershell
dotnet run --project tools\InspectSongPresentations
```

Build without restore after restore has been run once:

```powershell
dotnet build tools\InspectSongPresentations\InspectSongPresentations.csproj --no-restore
```

## Extraction Strategy

- `.pptx` files are read directly as ZIP/Open XML packages.
- Legacy `.ppt` files use read-only PowerPoint COM automation when PowerPoint is installed.
- Extracted text is normalized for whitespace, obvious bullet/control artifacts, and clearly broken letter spacing.
- Original song wording is preserved unless a character is clearly presentation formatting noise.

Use the report and samples to decide whether it is safe to design the real Songs data model and projection flow.
