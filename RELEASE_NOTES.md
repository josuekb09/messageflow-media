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
