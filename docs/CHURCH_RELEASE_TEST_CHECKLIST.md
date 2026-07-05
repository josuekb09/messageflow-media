# Church Release Test Checklist

Use this checklist on the church computer before service.

- [ ] App opens by running `MessageFlow.App.exe` without VS Code, Codex, or the source project.
- [ ] Brother Branham sermon search works.
- [ ] Sermon search for `Faith` shows readable results and scrolls smoothly.
- [ ] Sermon search for `Faith Is The Substance` shows readable results and scrolls smoothly.
- [ ] Sermon search for `47-0412 4` finds the expected sermon/paragraph result.
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
- [ ] Projection opens on the TV/projector.
- [ ] Projection uses the full TV/projector screen with only safe margins.
- [ ] Project `John 3:16`; it appears large and readable, not small in the middle of the screen.
- [ ] Project `Romans 8:4`, `Romans 8:28`, and `Matthew 1:12`; each short verse fills the screen well and remains readable.
- [ ] Project one sermon paragraph; it is readable from the back of the room.
- [ ] Project a long sermon paragraph; it paginates instead of shrinking to tiny text.
- [ ] Projection page navigation works with Right/Down/Space for next page and Left/Up/Backspace for previous page.
- [ ] A+ increases projected text size and may create more pages for long paragraphs.
- [ ] A- decreases projected text size.
- [ ] Fit resets projected text size to the selected dropdown's automatic fit.
- [ ] Medium, Large, and Extra Large projection sizes are readable from the back of the room.
- [ ] TV/projector shows only projected text, not search boxes, result lists, Admin Tools, or operator controls.
- [ ] Favorites work.
- [ ] History works.
- [ ] Admin > Verify Production Data passes.
- [ ] Admin > Test Projection Display works.
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
