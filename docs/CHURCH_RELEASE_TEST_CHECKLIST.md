# Church Release Test Checklist

Use this checklist on the church computer before service.

- [ ] App opens by running `MessageFlow.App.exe` without VS Code, Codex, or the source project.
- [ ] Brother Branham sermon search works.
- [ ] Sermon search for `Faith` shows readable results and scrolls smoothly.
- [ ] Sermon result list has a visible draggable scrollbar.
- [ ] Top filter dropdown text is readable and not clipped, including `Brother Branham`.
- [ ] With no search text, Author `Brother Branham` and Year `1965` lists all matching sermons and scrolls.
- [ ] Switching from Bible mode back to Search mode does not leave stale Bible status text in Sermon Results.
- [ ] Bible search for `John 3:16` works.
- [ ] Bible book/chapter/verse results are readable and scroll smoothly.
- [ ] Bible result list has a visible draggable scrollbar.
- [ ] Bible search for `R` shows books such as Ruth, Romans, and Revelation.
- [ ] Selecting Ruth shows Ruth chapters clearly.
- [ ] Selecting Ruth 1 shows all Ruth 1 verses and the list scrolls through every verse.
- [ ] Projection opens on the TV/projector.
- [ ] Project `John 3:16` and one sermon paragraph; both are readable from the back of the room.
- [ ] Project a long sermon paragraph; it remains readable and uses page 1 / page 2 if needed.
- [ ] Extra Large projection text is readable from the back of the room.
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
