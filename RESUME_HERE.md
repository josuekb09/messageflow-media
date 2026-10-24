# Resume here: MessageFlow Media 1.0.9 release

State on 2026-10-24 (evening, before Sunday 25 October):

- Code commit `d3b0302` ("Release 1.0.9 ..."), then a docs/website commit with the installer size and SHA-256.
  Both pushed to `origin/main`; Vercel redeploys the website (only `website\` changes trigger a build).
- `dist\MessageFlowMediaSetup.exe` is built from `d3b0302` with the corrected database: 547,649,563 bytes,
  SHA-256 `AFD70ED86A177D795C964E71AC743B73AEC0C2745D5F0D80D2A6C999FBDBBC91`. Before that build the two
  full-text indexes were compacted (`INSERT INTO ...(...) VALUES('optimize')`, text checked unchanged by hash),
  which brought the installer from 659 MB down to 522 MB. Clean install to `E:\MF_Install_Test3`: version
  1.0.9+d3b0302, 0 favorites/history, integrity ok; launches 9.1 s (first, antivirus scan of the new database),
  then 1.1 s and 1.6 s to the main window, 2.1 s and 2.6 s to search ready. It is NOT uploaded:
  the owner uploads it by hand to a new GitHub release `v1.0.9`, published as **Latest**. Until then the
  website says 1.0.9 but its Download button (`releases/latest/download/...`) still serves 1.0.8.

## What changed in the library (database is not in git)

- Whole library compared with the original sources: see `docs\SOURCE_ACCURACY_AUDIT.md`.
- Backup of the database before the corrections: `database\backups\messageflow_before_source_corrections_20261024.db`.
- Corrections were applied with `tools\ApplySourceCorrections` (checks the stored text of every row first).
  The analysis scripts are in `tools\SourceAccuracyAudit`. The database still stores `D:\...` source paths;
  the sources are now on `E:\` (the scripts map `D:` to `E:`).

## Checks run on the corrected database

- `dotnet test tests\MessageFlow.Tests -c Release`: 22/22.
- VerifyProductionData: all PASS. VerifyBrotherFrankLibrary: 32/32 (English Branham paragraph baseline is
  now 210,073). VerifyChurchUxHotfix: 14/14. RepairSongLineBreaks `--verify`: 4,569/4,569.
- VerifyLibraryProjectionFit `--only sermons,bible,songs,othersongs,frank --frank-top 100
  --screens 1280x720,1920x1080,3840x2160 --dpi 100`: 95,641 checks, 0 failures.

## Still to do after Sunday

- Test the projector hot-plug (`MainWindow.HandleDisplayLayoutChanged`) with a real projector: plug in and
  unplug while projecting; check `logs\app-startup.log` for "Display layout changed".
- Brother Frank circular letters with side boxes / tables: ~90 paragraphs with letters out of order need a
  manual re-import from the PDF (listed by `repair_sermons.py` as DIFF in Brother Frank documents).
- Nine documents whose printed numbering was ambiguous were left as they were (listed in the audit).
- Kiswahili translation of the Admin and import windows (they fall back to English by design).
- The full projection matrix with no options (~2 h).
