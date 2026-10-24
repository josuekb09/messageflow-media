# Source accuracy audit scripts

Compare the bundled library with the original source files, independently of the app's importers.
Results of the October 2026 run: [docs/SOURCE_ACCURACY_AUDIT.md](../../docs/SOURCE_ACCURACY_AUDIT.md).

Needs Python 3 with `python-pptx`, Poppler `pdftotext` and `git` on the PATH, and the source folders listed
in the audit document. Every script reads the database named by `MF_DB` (default
`database\messageflow.db`) and never writes to it.

| Script | What it does |
|---|---|
| `build_vocab.py` | Word list per language, used to refuse a spacing fix that would create an unknown word. Run first. |
| `repair_sermons.py <out.jsonl> "<SQL where>"` | Aligns every sermon with its PDF; writes the fixes it can prove and the differences left for review. |
| `export_sermons.py` | Turns `plan_all.jsonl` into corrections for `ApplySourceCorrections`. |
| `cmp_bible.py` | Every verse against the KJV, LSG 1910 and SWHULB source texts. |
| `cmp_songs.py en\|sw <source folder> <out.jsonl>` | Every song section, line and line break against its PowerPoint slides. |
| `export_songs_bible.py` | Corrections for the Bible verses and English/Kiswahili songs. |
| `cmp_sermons.py` | First, slower word-level comparison (difflib); also provides the tokenizer used by `cmp_fr_songs2.py`. |
| `cmp_fr_songs2.py`, `export_fr_songs.py` | French hymns against the songbook PDF, and their explicit corrections. |
| `check_library.py` | Library-wide checks: empty text, placeholders, glued running heads, numbering gaps. |
| `metrics.py` | Share of library words found, in order, in the source PDFs. |

Apply corrections to a copy first, then run every check again on it:

```powershell
dotnet run --project tools\ApplySourceCorrections -c Release -- --database <copy.db> --corrections corrections.jsonl          # check only
dotnet run --project tools\ApplySourceCorrections -c Release -- --database <copy.db> --corrections corrections.jsonl --apply
```
