# Church Release Test Checklist

Use this checklist on the church computer before service.

- [ ] App opens by running `MessageFlow.App.exe` without VS Code, Codex, or the source project.
- [ ] Brother Branham sermon search works.
- [ ] Bible search for `John 3:16` works.
- [ ] Bible book/chapter/verse results are readable and scroll smoothly.
- [ ] Projection opens on the TV/projector.
- [ ] Project `John 3:16` and one sermon paragraph; both are readable from the back of the room.
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
