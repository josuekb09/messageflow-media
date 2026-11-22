# MessageFlow Media 1.0.11

Church operator and projection software for Windows. Search and project sermons, Scripture, and hymns from the operator desk. Free for church use.

## What's new
- **New Brother Frank broadcast transcriptions: 798 documents.** 66 in English and 732 in French (560 broadcasts and 172 monthly meetings). Search, open and project them like any other sermon.
- **A real French library for Brother Frank.** French sermons now number 1,116 (384 before) and English 1,354 (1,288 before). French documents only appear when French is selected and never mix into English search.
- **Brother Branham sermons that are broadcast again** (3 in English, 32 in French) are kept with the Brother Branham library, apart from Brother Frank's own words.
- **The text is exactly the source text.** Not a word or a character was changed, and the paragraph numbers printed in the documents are kept.
- **French typography is respected on screen.** A line never breaks between a word and the : ; ! ? » that follows it, or after «, on the operator screen and on the projector. The stored text is not touched.
- **Checked before release:** 67 automated tests pass. Every paragraph of the new documents (80,608) passes the projection checks at 1280x720 and 1920x1080 with 0 overflows, 0 cut words and 0 text changes.

## Verified before release

- 67 automated tests pass, including new checks on counts per language and source, no duplicate document, no empty paragraph, no replacement character, continuous paragraph numbers, no untitled document, no trailing whitespace, and the French typography rules.
- New documents, all 80,608 paragraphs, 1280x720 and 1920x1080: 161,216 layout configurations, 212,310 real-window page checks, 0 failures.
- 20 paragraphs picked at random (10 English, 10 French) are identical to the source PDF text.
- Bible, songs and the earlier sermons are unchanged: 357 English, 499 French and 281 Kiswahili songs, three Bibles, the 1,203 English, 384 French and 622 Kiswahili Brother Branham sermons and the 85 earlier Brother Frank documents.
- Search index rebuilt and checked; the database inside the installer passes the SQLite integrity check and the full-text index check.
- Clean install tested on Windows 11: version 1.0.11, database integrity ok, full-text index ok, no favorites or history, search finds the new English and French documents.

## Download

Installer: `MessageFlowMediaSetup.exe` (731,196,615 bytes, about 697 MB), attached to the GitHub release v1.0.11.

SHA-256: `623A5A7D4D61C61BC69731818E1C88D2A88620DC2CFC4635FCF6F1B3186FB254`

---

# MessageFlow Media 1.0.10

Church operator and projection software for Windows. Search and project sermons, Scripture, and hymns from the operator desk. Free for church use.

## What's new
- **Bible search is smoother while you type.** Results appear once you pause, and replace the list in a single step.
- **The Bible list goes to the verse you asked for.** If you asked for a chapter or a book, it opens at the top of it.
- **Auto fit centres sermon and Bible text horizontally** on the projector. The words, pages and text size stay the same.
- **Checked before release:** 40 automated tests pass, and projection checks on songs, the longest sermon paragraphs, the longest Bible verses and all Brother Frank paragraphs found 0 overflows, 0 cut words and 0 text changes.

## Verified before release

- 40 automated tests pass.
- Songs: 129,168 layout configurations, 32,293 real-window page checks, 0 failures (612 s).
- Sermons, Bible and Brother Frank, at 1280x720 and 1920x1080: 73,296 layout configurations, 103,229 real-window page checks, 0 failures (2,354 s).
- Clean install tested on Windows 11: version 1.0.10, no favorites or history, database integrity ok.

## Download

Installer: [GitHub release v1.0.10](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.10), `MessageFlowMediaSetup.exe` (547,653,029 bytes, about 522 MB).

SHA-256: `B2F9A2E1FA50F1B0234A5E8532567AEFA5FE8F34CDFF04F71B412C2C87F2BB51`

---

# MessageFlow Media 1.0.9

Church operator and projection software for Windows. Search and project sermons, Scripture, and hymns from the operator desk. Free for church use.

## Highlights

- **Paragraph numbers match the books.** Sermon paragraphs now follow the numbers printed in the books in English, French, and Kiswahili, so "paragraph 36" in the app is paragraph 36 in the book.
- **Cleaner sermon text.** Page headings that had slipped into paragraphs ("THE SPOKEN WORD", "LILE NENO LILILONENWA", "LA PAROLE PARLÉE", the sermon title with a page number) are gone, and words that had run together ("I'mI'm", "Sothen", "fi rst") read as in the book.
- **Songs word for word.** English and Kiswahili songs match their PowerPoint slides, spaces included, and the French hymns match the hymn book.
- **Projector plugged in or out during the service.** If the projector or TV is connected or disconnected while you project, MessageFlow moves the projection to the right screen by itself and keeps your controls on the computer screen.
- **Bible.** Galatians 6:18 and Ephesians 6:24 (KJV) have their final "Amen." back.

## Also fixed

- Importing a PDF no longer loses a dash at the end of a line.
- The projection display selector and the backup buttons in Admin tools have tooltips, and the display names are in Kiswahili too.

## Verified before release

- Every sermon and publication (2,294 PDFs), song (1,137), and Bible verse (93,373) was compared with its original source file. Details: [docs/SOURCE_ACCURACY_AUDIT.md](docs/SOURCE_ACCURACY_AUDIT.md).
- Projection checked at 1280x720, 1920x1080, and 4K: the 100 longest sermon paragraphs of each language, the 100 longest verses of each Bible, every song section, and the longest Brother Frank paragraphs. 95,641 checks, 0 overflows, 0 cut words, 0 text changes.

## Download

Installer: [GitHub release v1.0.9](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.9), `MessageFlowMediaSetup.exe` (547,649,563 bytes, about 522 MB).

SHA-256: `AFD70ED86A177D795C964E71AC743B73AEC0C2745D5F0D80D2A6C999FBDBBC91`

---

# MessageFlow Media 1.0.8

Church operator and projection software for Windows. Search and project sermons, Scripture, and hymns from the operator desk. Free for church use.

1.0.6 and 1.0.7 were never published; 1.0.8 is the first public release after 1.0.5 and includes their changes.

## Highlights

- **Fast, reliable startup.** Two names for the same Brother Frank library made every launch rebuild the whole search index, which locked the database for 40 seconds to several minutes and made clicks fail silently. A normal launch now does no rebuild: the main window opens in about 1–2.5 seconds and search is ready in 2–4 seconds. The window stays behind the splash until startup has truly finished, and a language switch either completes or returns fully to the previous language, never a mix.
- **Every projected screen fits on its own.** Each new song section, verse, paragraph, and continuation page starts at its own best fit, so an A- on one section no longer shrinks the next one. A+ stops at the largest size that fits, A- stops at the smallest readable size, each press is 5%, and Auto returns to the best fit. A word is never cut in two or half-hidden, and text that cannot fit at a readable size is split into "1 of 2", "2 of 2" screens with the exact original words.
- **Song lines no longer run together.** Songs whose PowerPoint slides break lines with Shift+Enter were imported as one long line, which is how a Kiswahili hymn reached the projector with all its phrases stuck together. 648 song sections (496 Kiswahili, 152 English) have their original line breaks back. Only whitespace changed, and all 4,572 PowerPoint song sections were checked against their source slides.
- **Light theme.** Contrast is at least 4.5:1 for every text and background pair, and 7:1 or more on the projector. The dark theme is unchanged.
- **Only real library text is ever projected.** A saved favorite or history item that is no longer in the library is never replaced by another paragraph, and empty text is never sent to the projector; the status bar explains what happened instead. A fresh install opens in English with no favorites or history from anyone else's computer.

## Also fixed

- Bible verses and song slides no longer raise an error in the projection window.
- On small high-DPI projectors (for example 1280x720 at 150%) the projection window always matches the screen and keeps its bottom lines.
- Songs show only their own slide text on the projector, with no extra generated title.
- Faster switching between very long paragraphs, and an instant return to recently projected items.
- The installer suggests `MessageFlowMedia` on the local data drive with the most free space instead of Documents, and warns when a OneDrive folder is chosen. Settings and logs stay inside the install folder.
- An unexpected error shows one short message on the operator screen and is logged; the app and the projection keep running.
- Every control has a tooltip in English, French, and Kiswahili, with its keyboard shortcut.

## Verified before release

- Projection checked at 1280x720, 1920x1080, and 4K: the 100 longest sermon paragraphs, Brother Frank paragraphs, and Bible verses, plus every English, Kiswahili, and French song section. 85,300 checks, 0 overflows, 0 cut words, 0 text changes.
- Clean install tested on Windows 11.

## Download

Installer: [GitHub release v1.0.8](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.8), `MessageFlowMediaSetup.exe` (615,471,332 bytes, about 587 MB).

SHA-256: `163B4141CBDC7AB1166D601562575CEA1165C06B1AA288B4A191154A1861B395`

---

# MessageFlow Media 1.0.7

Church operator and projection software for Windows. Search and project sermons, Scripture, and hymns from the operator desk. Free for church use.

## Highlights in 1.0.7

- **Zero-Overflow Projection Auto-Fit:** Every projected item (sermon paragraphs, Bible verses, song sections, and Brother Frank publications) automatically fits the projection stage with safe margins and zero cut-off text across 720p, 1366x768, 1080p, and 4K displays at 100%, 125%, 150%, and 200% DPI. Operator A+ scaling is strictly clamped to the screen fit limit to prevent accidental overflow.
- **Intelligent Sentence-Boundary Pagination:** Long paragraphs split into continuation screens ("1 of 2", "2 of 2") preserving 100% of the original text. Continuation screens navigate smoothly with Left/Right arrow keys before moving to the adjacent paragraph.
- **Professional Light Theme Redesign:** Brand new, clean light theme (#F5F7FA background, #FFFFFF cards, #111827 primary text, #2563EB accent, #1E3A8A reference emphasis) with contrast exceeding 7:1 on projection and 4.5:1 across the application. Dark mode remains completely identical.
- **Strict Content Authenticity:** All projection pathways audited to ensure only real, bundled library text is displayed. In case of load errors, error notices appear only on the operator desk while projection remains clean.
- **Strict Bible Translation Isolation:** Selecting an interface language (English, French, Kiswahili) strictly binds the Bible chapter and verse view to its native translation (KJV, Louis Segond 1910, SWHULB).

## Download

Installer: [GitHub release v1.0.7](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.7)

---

# MessageFlow Media 1.0.6

Church operator and projection software for Windows. Search and project sermons, Scripture, and hymns from the operator desk. Free for church use.

## Highlights in 1.0.6

- **Fast & Responsive Startup:** The main window now opens in under 2 seconds (previously up to 10-15 seconds on cold launch). Heavy database verification runs non-blockingly in the background with zero "Not Responding" freezing. Search is instantly responsive once warm cache is loaded.
- **Universal Projection Auto-Fit:** All projected content (Sermons, Bible, and Songs) now automatically fits the projection stage with safe margins and zero text clipping across 720p, 1080p, and 4K screens. Long sermon paragraphs paginate cleanly at natural sentence boundaries.
- **Precision Text Size Controls (A- / Auto / A+):** Font size adjustments now scale smoothly in 5% increments, and clicking "Auto" instantly resets projection to the mathematically optimal auto-fit size.
- **Window Placement Memory:** The operator window remembers its exact position, size, and maximized state across restarts.
- **Native PerMonitorV2 High DPI Awareness:** Crisp fonts on multi-monitor systems with differing display scaling.

## Download

Installer: [GitHub release v1.0.6](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.6)

---

# MessageFlow Media 1.0.5

Church operator and projection software for Windows. Search and project sermons, Scripture, and hymns from the operator desk. Free for church use.

## Library in this build

- **Sermons:** 1,288 English (1,203 Brother Branham and 85 Brother Frank Publications), 384 French, 622 Swahili
- **Bibles:** KJV, Louis Segond (LSG), SWHULB
- **Songs:** 357 English, 499 French hymns (verse then chorus after each couplet), and 281 Swahili hymns

Every interface language has its matching sermons, Bible, and songbook.

## Fixes

- The Brother Frank library is repaired. It was split across duplicate and test content sources, two circular letters were missing, and selecting Brother Frank sometimes returned no search results. It is now one complete library of 85 documents, which raises the English sermon count from 1,208 to 1,288
- Previous and Next Paragraph navigation now works for Brother Frank documents; the Bible and Brother Branham sermons already had it
- Bible projection text no longer runs off the screen on some verses
- Switching between sermon library filters with no search text no longer takes several seconds
- Removed a duplicate Source and Year filter control that appeared in two places in the interface and could show two different states at once

## Desktop app

- Optional light theme (white and blue); dark remains the default
- Language switch for English, French, and Kiswahili
- Favorites, history, Bible lookup, and dual-screen projection
- Shortcuts: Ctrl+F search, Ctrl+P project, arrow keys to move

## Download

Installer: [GitHub release v1.0.5](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.5)

The installer is too large for the git tree. Attach only `MessageFlowMediaSetup.exe` to the GitHub Release; the website Download button uses that release asset.
