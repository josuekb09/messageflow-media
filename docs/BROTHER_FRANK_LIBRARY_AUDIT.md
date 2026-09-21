# Brother Frank library — audit and repair record

Date: 2026-09-21
Starting commit: `4071456`
Database: `E:\My Projects\MessageFlow\database\messageflow.db`
Recovery copy: `E:\My Projects\MessageFlow\database\backups\messageflow_before_frank_library_20260921T095628Z.db`
(1,866,035,200 bytes, `PRAGMA quick_check` = ok, 2,297 documents / 474,375 paragraphs)

The backup is never overwritten. No destructive git command was used at any point.

---

## 1. State found

Production data was healthy and matched every historical baseline:

| Dataset | Expected | Found |
|---|---|---|
| Branham English documents | 1,203 | 1,203 |
| Branham English paragraphs | 210,061 | 210,061 |
| Bible KJV / LSG / SWHULB | 31,102 / — / — | 31,102 / 31,170 / 31,101 |
| Bible books | 66 | 66 |
| English songs | 357 | 357 |
| Frank "Revelation" paragraphs | ~1,071 | 1,071 |

Already working, and therefore not rebuilt: source and language filtering in SQL, in-document
search with previous/next match, and projection from an immutable snapshot of the original
paragraph text.

### Defects identified

1. **The Brother Frank library was split across four content sources** — `brother_frank` (83
   documents), `brother_frank_custom` (1 document, the Revelation book), and two abandoned test
   sources pointing at a `D:` drive that no longer exists. Two carried the identical display name
   "Brother Frank Publications", so the Sources filter showed two indistinguishable entries and
   neither covered the whole library.

2. **Two documents were missing.** `202004-Circular_Letter-english.pdf` and
   `202012-Circular_Letter-english.pdf` were skipped at import. Evidence: `ImportLogs` rows 8318
   and 8319 — *"Duplicate publication code CL-2020-04 (already imported from en-RB-2020-04.pdf)"*.
   The blocking documents belonged to the hidden test source, because the duplicate-code check in
   `PdfSermonImporter` was not scoped to the content source.

3. **One duplicate document.** Documents 5891 (`CL-2014-03`) and 5892 (`CL-2014-03-04`) came from
   byte-identical PDFs (both 257,415 bytes) and held identical 121-paragraph content. Two filename
   styles produced two different codes, so the code check never saw them as the same publication.

4. **Source filtering ran after `LIMIT`.** Hidden sources were removed from the result list in C#
   after the query had already truncated the page, silently shrinking it.

5. **Search starvation when a library was selected.** The source predicate lived only in the outer
   `WHERE`, so the ranked full-text page filled with the larger collection before the filter
   applied. Measured on the real database: searching *Jesus* scoped to Brother Frank returned
   **0 rows**; *God* returned **1**.

6. **Content type belonged to the source, not the document**, so one library could not hold
   circular letters, meetings and books at once — which is exactly what this library contains.

7. **Publication codes were not searchable.** Normalisation turns punctuation into spaces, so
   `47-0412` became `47 0412` and was read as "the word 47, paragraph 412", returning nothing for
   any document with fewer than 412 paragraphs.

8. **The Revelation book was degraded.** 150 page-number-only paragraphs and 169 very short
   fragments out of 1,071, plus one paragraph from page 95 sitting at position 1. It had been
   imported through the older library-import path, which does not apply the quality filter.
   Its `SourceFilePath` also still named a `D:` drive.

---

## 2. Repairs applied

Through `tools/RepairBrotherFrankLibrary` (dry run by default, `--apply` to write) and a normal
import run. Every step is idempotent.

- Folded `brother_frank_custom` into `brother_frank`; deleted the two test sources
  (4 documents / 526 paragraphs) and the orphaned `Ewald Frank Test` author.
- Deleted duplicate document 5892, identified by comparing paragraph content rather than filenames.
- Repointed the Revelation record from its stale `D:` path to the file's real location.
- Set the library folder to `E:\Litreture (Bro Frank)` so Admin → Import can reach it.
- Classified every document with a `ContentType`.
- Imported the two missing 2020 circular letters (135 and 94 paragraphs).
- Re-imported the Revelation book through the current pipeline.

### Revelation re-import, verified against the backup

| Measure | Before | After |
|---|---|---|
| Paragraphs | 1,071 | 855 |
| Words | 57,346 | 57,296 |
| Page-order inversions | 1 | 0 |
| Page-number-only paragraphs | 150 | 0 |

The 50 lost words are fully accounted for: `Chapter` ×42 (running headers), plus `appendix`,
`Foreword`, `epilogue` and four title-page fragments. No body text was lost and no word was
duplicated. The page-95 paragraph now sits at position 498, between the page-94 text that
introduces it and the paragraph that refers back to "this six-fold combination".

---

## 3. State after repair

```
id 1  brother_branham  Brother Branham               SermonPdfCollection  2,209 docs  464,225 paragraphs
id 6  brother_frank    Brother Frank Publications    CircularLetter          85 docs    9,516 paragraphs
```

Brother Frank by document type: 75 circular letters, 6 meetings, 4 books. All English.

Protected baselines re-verified unchanged: Branham English 1,203 / 210,061; Bible 93,373 verses
and 66 books; English songs 357. Search index in sync at 473,741 rows.

---

## 4. Known limitations

- **Paragraphs can still end mid-sentence.** `ParagraphSplitter` now rejoins a paragraph cut by a
  page break, but existing records were not re-extracted with that fix. Measured on
  `1989-02-Circular_Letter-english.pdf`: 58 paragraphs → 57 with the fix. The remaining
  mid-sentence endings are mostly headings, which legitimately have no closing punctuation, plus
  within-page blank-line splits — a different and more ambiguous cause. Re-extracting all 85
  documents would renumber every paragraph in the library and should be an explicit decision.
- **No column detection** in `PdfTextExtractor`. Acceptable for this corpus — all 85 documents
  read in strict page order — but a risk for any future multi-column source.
- **Two databases exist on this machine.** Development uses
  `E:\My Projects\MessageFlow\database\messageflow.db`; an installed build resolves to
  `E:\MessageFlowMedia\database\messageflow.db` (1,576,574,976 bytes, last written 2026-08-27),
  which does **not** carry these repairs.
- **Two pre-existing verifier failures** in `VerifyChurchUxHotfix`, both confirmed to fail
  identically at commit `4071456` with untouched code: operator search highlighting of an unquoted
  contiguous phrase, and John 4 loading fewer than its 54 verses. The Bible data itself is correct
  — all three translations hold 54 verses for John 4 — so that one is a defect in the chapter
  loading path, not in the data.
