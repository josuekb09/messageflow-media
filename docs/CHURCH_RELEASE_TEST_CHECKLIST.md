# Church Release Test Checklist

Use this checklist on the church computer before service.

- [ ] App opens by running `MessageFlow.App.exe` without VS Code, Codex, or the source project.
- [ ] Brother Branham sermon search works.
- [ ] Sermon search for `Faith` shows readable results and scrolls smoothly.
- [ ] Project a `Faith` search result and confirm the sermon body is readable and left-aligned cleanly.
- [ ] Sermon search for `Faith Is The Substance` shows readable results and scrolls smoothly.
- [ ] Sermon search for `47-0412 4` finds the expected sermon/paragraph result.
- [ ] Search `wedd`, open `Wedding Ceremony VGR`, and confirm projected text does not contain broken words such as `T H E`, `S P OKEN`, or `W O RD`.
- [ ] Project `Wedding Ceremony VGR` and confirm the body text is aligned cleanly, with no weird spaces inside words.
- [ ] Search Library result list has a visible draggable vertical scrollbar.
- [ ] Search Library scrollbar has a full-height track like a normal app scrollbar.
- [ ] Search Library result list does not show a tiny stray grey scroll indicator inside the results.
- [ ] Top filter dropdown text is readable and not clipped, including `Brother Branham`.
- [ ] With no search text, Author `Brother Branham` and Year `1965` lists all matching sermons and scrolls.
- [ ] Switching from Bible mode back to Search mode does not leave stale Bible status text in Sermon Results.
- [ ] Bible search for `John 3:16` works.
- [ ] Bible book/chapter/verse results are readable and scroll smoothly.
- [ ] Bible Library result list has a visible draggable vertical scrollbar.
- [ ] Bible Library result list does not show a tiny stray grey scroll indicator inside the results.
- [ ] Bible search for `R` shows books such as Ruth, Romans, and Revelation.
- [ ] Selecting Ruth shows Ruth chapters clearly.
- [ ] Selecting Ruth 1 shows all Ruth 1 verses and the list scrolls correctly through every verse.
- [ ] Bible search/select Matthew 1 and confirm the visible scrollbar can be dragged through the results.
- [ ] Songs tab opens and shows local PowerPoint songs from `D:\SONG PRESENTATION` and `D:\SONG PRESENTATION\choir`.
- [ ] Songs tab does not show songs from `D:\SONG PRESENTATION\chruch service`.
- [ ] Song search for `tell`, `tell me the story`, `calvary`, `amazing love`, `great deliverer`, and `holy words` returns results.
- [ ] Songs Library result list has a visible draggable vertical scrollbar.
- [ ] Selecting a song shows ordered song sections in the center panel.
- [ ] Selecting a song section shows the lyrics in the right preview panel without stale Bible or sermon text.
- [ ] Songs preserve repeated lyric lines exactly as they appear in the PowerPoint slide.
- [ ] Search `116`, open `116. WON’T IT BE WONDERFUL`, select Slide 2, and confirm the final line is `Won’t it be wonderful there?` after `storyland,`.
- [ ] Search `110`, open `110. THEY COME...`, and compare with the original PowerPoint for line order and missing lines.
- [ ] Song sections do not miss trailing lines.
- [ ] Project, Copy, Previous Section, and Next Section work for songs.
- [ ] Song projection uses the same black background, white text, fit-to-screen sizing, and pagination as Bible/sermon projection.
- [ ] Song source filename is visible or available without showing an ugly full path in the main result card.
- [ ] Projection opens on the TV/projector.
- [ ] On a one-screen laptop, Project opens a normal windowed Projection Preview that can be moved, resized, and minimized.
- [ ] Admin > Open Projection Preview Window opens a windowed projection preview for the selected Bible verse or sermon paragraph.
- [ ] Projection uses the full TV/projector screen with only safe margins.
- [ ] Project `John 3:16`; it appears large and readable, not small in the middle of the screen.
- [ ] Project `John 3:16` and confirm no weird spaces are inserted into the KJV wording.
- [ ] Project `Romans 8:4`, `Romans 8:28`, and `Matthew 1:12`; each short verse fills the screen well and remains readable.
- [ ] Project one sermon paragraph; it is readable from the back of the room.
- [ ] Project a long sermon paragraph; it paginates instead of shrinking to tiny text.
- [ ] When a long paragraph shows `Page 1 of 2`, the operator-side Next Page button updates the TV/projector to page 2.
- [ ] Previous Page and Next Page move within the projected item; Previous Paragraph and Next Paragraph still move between paragraphs.
- [ ] Projection page navigation works with Right/Down/Space for next page and Left/Up/Backspace for previous page.
- [ ] Project a long paragraph and confirm words wrap normally, without words splitting into individual letters.
- [ ] No projected sermon, Bible, or song text shows weird spaces inside words.
- [ ] A+ increases projected text size and may create more pages for long paragraphs.
- [ ] A- decreases projected text size.
- [ ] Fit resets projected text size to the selected dropdown's automatic fit.
- [ ] Medium, Large, and Extra Large projection sizes are readable from the back of the room.
- [ ] TV/projector shows only projected text, not search boxes, result lists, Admin Tools, or operator controls.
- [ ] Favorites work.
- [ ] History works.
- [ ] Admin > Verify Production Data passes.
- [ ] `dotnet run --project tools\VerifySongData` passes with 357 active songs and zero zero-text songs.
- [ ] `dotnet run --project tools\ImportSongPresentations` runs in preview mode without database writes.
- [ ] `dotnet run --project tools\AuditSongImportAccuracy` passes and writes `D:\MessageFlow Archive\SongImportTest\song_import_accuracy_report.txt`.
- [ ] `dotnet run --project tools\AuditTextProjectionQuality` reports zero high-severity text-spacing issues and writes `D:\MessageFlow Archive\FinalQualityAudit\projection_text_quality_report.txt`.
- [ ] Rebuild the final release package with `tools\CreateChurchRelease\create_church_release.cmd`.
- [ ] Rebuild the installer after the final release package and confirm `D:\MessageFlow Release\Installer\MessageFlowMediaSetup.exe` exists.
- [ ] Admin > Test Projection Display works.
- [ ] `dotnet run --project tools\AuditBranhamImportQuality` generates `D:\MessageFlow Archive\BranhamAudit\branham_import_quality_report.txt`.
- [ ] Review the Branham audit report before any future text cleanup; do not clean or delete database content until the audit is reviewed.
- [ ] Brother Frank is not enabled unless intentionally imported later.
- [ ] The Table is not imported.

Release folder reminder:

```text
MessageFlow\
  MessageFlow.App.exe
  database\
    messageflow.db
```

Do not delete the `database` folder.
