# Source accuracy audit — 24 October 2026 (v1.0.9)

Every text in the bundled library was compared with its original source file, using readers that are
independent of the app's own importers: Poppler `pdftotext` for PDFs, `python-pptx` for PowerPoint, and
the original Bible source texts. Corrections were made only from those sources, and only where the
difference could be proven; everything else was left unchanged and is listed below.

The scripts are in [tools/SourceAccuracyAudit](../tools/SourceAccuracyAudit) and the corrections were
written by [tools/ApplySourceCorrections](../tools/ApplySourceCorrections), which refuses any correction
whose stored text differs from the analysed one and recomputes the search columns with the importers'
own normalizers.

## Sources

| Content | Source folder | Files |
|---|---|---|
| Brother Branham, English | `E:\Br William Marrion Branham\PDF\<year>` | 1,203 PDFs |
| Brother Branham, French | `E:\Brother_Branham_French_Sermons` | 384 PDFs |
| Brother Branham, Kiswahili | `E:\Brother_Branham_kiswahili_sermon` | 622 PDFs |
| Brother Frank, English | `E:\Litreture (Bro Frank)` (84) and `database\custom-library\sermons` (1) | 85 PDFs |
| English songs | `E:\SONG PRESENTATION` (and `choir\`) | 357 PPTX used |
| Kiswahili songs | `E:\swahili-pptx` (= `database\custom-library\songs\Nyimbo-za-Kiswahili.zip`) | 281 PPTX used |
| French hymns | `database\custom-library\songs\Receuil-de-cantiques-francais-TABERNACLE-DINANGA-CORRIGE.pdf` | 1 PDF, 499 hymns |
| KJV | `E:\Bible\Sources\kjv_gutenberg.txt` (Project Gutenberg) | 31,102 verses |
| Louis Segond 1910 | `E:\MessageFlow-Bible-Sources\LSG1910\fraLSG_*_read.txt` | 31,170 verses |
| SWHULB | `E:\MessageFlow-Bible-Sources\SWA\*.usfm` | 31,101 verses |

The folders `E:\bro frank transcripts` (41 PDFs), `E:\bro frank sermon 2026` and `E:\Brother frank sermon ENG`
are not in the library (they hold transcripts that were never imported), so they were not compared.

## Method

- Typography is normalised the same way on both sides before comparing (the library has always stored
  `’ “ ” — …` as `' " " - ...`). Page numbers, running heads and the copyright page are removed from the
  PDF text; nothing is removed from the library text.
- Sermons: every word of every paragraph is aligned with the PDF (Myers diff). Paragraph numbers are read
  from the PDF in sequence, so a number inside the text ("from 85 back to 100") is never taken for one.
- Songs: every slide line and every line break of each section is compared with its slide.
- French hymns: every section must occur word for word in the songbook PDF.
- Bible: every verse is compared with its source verse.

## Results

### Sermons and publications

Words of the library that are not, in order, in the source PDF:

| | Documents compared | Before | After | Identical after |
|---|---|---|---|---|
| Brother Branham, English | 1,203 / 1,203 | 19,635 | 1,216 | 99.993% of 18,239,170 words |
| Brother Branham, French | 384 / 384 | 17,019 | 1,084 | 99.985% of 7,397,361 words |
| Brother Branham, Kiswahili | 622 / 622 | 44,654 | 1,843 | 99.978% of 8,377,007 words |
| Brother Frank, English | 85 / 85 | 12,162 | 3,982 | 99.447% of 719,777 words |

Paragraph numbering identical to the numbers printed in the PDF: English 1,182 / 1,203, French 345 / 384,
Kiswahili 579 / 622. Before the audit, 234 English sermons had gaps and almost every French and Kiswahili
sermon was shifted by one or more numbers against the book. Brother Frank publications have no printed
paragraph numbers.

Defects found and corrected from the PDFs:

| Defect | Example | Corrected |
|---|---|---|
| Running page head glued into a paragraph | `...of something." TRYING TO D O GOD AS ERVICE WITHOUT IT BEING... 3` | English 1,181; Kiswahili 21,983 (`LILENENOLILILONENWA`) and French 7,930 (`LAPAROLEPARLÉE`), plus 287 more in documents whose numbers could not be read |
| Paragraph made only of a running head | `LILENENOLILILONENWA` | 251 removed (Kiswahili, French) |
| Em dash at the end of a PDF line lost, words glued | `I'mI'm`, `aa kitten`, `II've` → `I'm-I'm`, `a-a kitten`, `I-I've` | English 4,941, French 2,047, Kiswahili 5,301, Brother Frank 2 |
| Words glued or split | `Sothen`, `Whoishethathideth...`, `fi rst`, `sacrifi ced`, `M me` | English 3,954, Brother Frank 3,279, French 830, Kiswahili 220 |
| Number of the text taken for a paragraph number | `Let's take from` / `back to 100 maybe.` → `Let's take from 85 back to 100 maybe.` | English 70, French 61, Kiswahili 92 |
| Paragraph number left inside the text | `13...reasonable idea` | split at the printed number |
| Opening words of paragraph 1 dropped | `...?...Thousands and thousands of people have been attending.` | English 16, French 27, Kiswahili 32, Brother Frank 1 |
| Title glued to paragraph 1 | `PRAYER...clock of the resurrection` | English 2, French 3, Kiswahili 10 |
| Paragraphs split at page breaks / merged | 50-0714 paragraph 36 was numbered 86 | 1,262 documents re-cut on the printed numbers |

A space was added or removed only where every resulting word is a common word of the same language's
library, so a weakness of the PDF reader can never glue two real words together. A dash was restored only
where the PDF prints an em dash at that place.

Every re-cut was accepted only if the new paragraphs hold exactly the same words, in the same order, and
the numbers follow without a gap. Nine documents failed that check and were left as they were:
I Am The Resurrection And The Life, Gods Provided Way, Earnestly Contending For The Faith, The Queen Of
Sheba (English); Maswali na Majibu Juu ya Waebrania I, Uwe na Hakika na Mungu, Malkia wa Sheba, Kumtendea
Mungu Kazi Bila Mapenzi Yake (Kiswahili); Le Docteur Moïse (French).

Known remaining differences (not changed, because the source does not prove a correction):

- Brother Frank circular letters with side boxes, rotated text or tables: some paragraphs hold letters out
  of order (`tHu s sa it H t He Lo r d`), a pull quote inside a word, or a jumbled table (for example the
  2010 earthquake table in *Circular Letter - May 2010*). About 90 paragraphs; they need a manual
  re-import from the PDF.
- pdftotext loses the `ft` ligature in some Brother Frank PDFs (`a er` for `after`); there the library is
  right and was not touched.
- Question numbers of the *Questions and Answers* books (`242.`) are not in the library text.
- Lines printed as separate lines inside one numbered paragraph (dialogue, poems) are joined with spaces
  inside that paragraph, as before.
- 21 English, 39 French and 43 Kiswahili documents whose printed numbers could not be read reliably keep
  their numbering.

### Songs

| | Compared | Result after |
|---|---|---|
| English | 357 / 357 songs, 2,691 sections | slide order and line breaks identical for all; 111 sections had spaces lost between PowerPoint runs (`blessèdSaviour` → `blessèd Saviour`) and were corrected |
| Kiswahili | 281 / 281 songs | identical to the slides; 302 sections had a space inserted inside words (`Y esu`, `KW A`) and were corrected; *TARAJA YANGU* had the chorus between the two halves of verse 1 and now follows its 9 slides |
| French | 499 / 499 hymns, 3,066 sections | 3,061 sections word for word in the songbook; 53 sections corrected (letters swapped by the old import: `Deiu`, `Lumèire`, `j'aettindrai`, `ma'ttendra`; the musical key `E b` / `D b` at the start of verse 1); hymn A74 rebuilt from the book |

Line-break check (`RepairSongLineBreaks --verify`): 4,569 / 4,569 PowerPoint sections keep the line breaks of
their slides.

Remaining, faithful to the source and not changed: *HARK THE VOICE THAT DEVIDES* (choir) has its lines run
together in the PowerPoint itself (`sky!Happy`); French hymn 203 keeps a stray `µ` printed in the songbook;
hymn 97 shows its mid-verse refrain as a separate section; hymn 322 omits the voice cue `(A.T.B : Le Roi des
rois)`. The songbook has no line breaks inside its stanzas, so French lines are split by the importer, and
some French sections span two stanzas.

### Bible

| | Verses | Identical to source after |
|---|---|---|
| King James Version | 31,102 | 31,102 (Galatians 6:18 and Ephesians 6:24 had lost their final "Amen.") |
| Louis Segond 1910 | 31,170 | 31,170 |
| SWHULB | 31,101 | 31,101 (1 Chronicles 6:78 and 8:17 had a stray `-79` / `-18` from the source's verse ranges) |

Numbering is continuous in every chapter, except where the SWHULB source itself joins verses
(1 Ch 6:78-79, 8:17-18) or omits one (Luke 17:36).

### Library-wide checks

Empty texts: 0. Placeholder, sample or lorem text: 0 (the words "dummy" and "sample" occur only in real
sermon text). Glued running heads remaining: 0.

## Projection

`VerifyLibraryProjectionFit --screens 1280x720,1920x1080,3840x2160` on the corrected library (the 100
longest sermon paragraphs of each language, the 100 longest verses of each Bible, every song section, the
100 longest Brother Frank paragraphs): 95,641 checks, 0 overflows, 0 cut words, 0 unreadable sizes, 0 text
changes, 3,804 continuation pages.
