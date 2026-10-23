# MessageFlow Media 1.0.8

Church operator and projection software for Windows. Search and project sermons, Scripture, and hymns from the operator desk. Free for church use.

1.0.6 and 1.0.7 were never published; 1.0.8 is the first public release after 1.0.5 and includes their changes.

## Fixes in 1.0.8

- **Song lines no longer run together.** Songs whose PowerPoint slides break lines with Shift+Enter were imported as one long line, which is how a Kiswahili hymn reached the projector with all its phrases stuck together. Every PPTX reader now keeps those breaks, and 648 song sections in the bundled library (496 Kiswahili, 152 English) have their original line breaks back. Only whitespace changed: each section was verified character for character against its source slide.
- **Bible verses and song slides project reliably.** The projection window could raise an error on every Bible verse and song lyric slide (a zero header size was applied). Fixed and covered by tests.
- **A saved favorite or history item that is no longer in the library is never replaced by another paragraph.** Before, the previously selected paragraph could be projected in its place. Now nothing is projected and the status bar explains why.
- **Small high-DPI projectors.** The projection window could be taller than a 1280x720 screen at 150% scaling and lose its bottom lines. It now always matches the screen.
- **A+ / A- / Auto.** A+ stops at the largest size that fits (and is disabled there), A- stops at the smallest readable size, each press is 5%, and Auto returns to the best fit. Pressing A+ at the limit no longer makes the next A- presses look dead.
- **Never unreadable, never cut.** Text that cannot fit at the readable minimum is split into "1 of 2", "2 of 2" screens at sentence boundaries with the exact original words. A split inside a very long sentence no longer glues its last word to the next sentence.
- **Faster paragraph switching** for very long paragraphs, and an instant return to recently projected items.
- **Songs show only their own slide text** on the projector, with no extra generated title.
- **Safe install location.** The installer now defaults to a local folder (`%LOCALAPPDATA%\Programs\MessageFlowMedia`) instead of Documents, which OneDrive often synchronizes. OneDrive can make the large library slow to open; the installer warns when a OneDrive folder is chosen.
- **Errors never close the app.** An unexpected error shows one short message on the operator screen and is logged; the app and the projection keep running.
- **Every control has a tooltip** in English, French, and Kiswahili, with its keyboard shortcut.
- **Light theme contrast** raised to at least 4.5:1 for every text and background pair (projection 7:1 or more). The dark theme is unchanged.

## Download

Installer: [GitHub release v1.0.8](https://github.com/josuekb09/messageflow-media/releases/tag/v1.0.8)

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
