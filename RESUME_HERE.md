# Resume here: MessageFlow Media 1.0.8 release

Last code commit before this note: `bd57d5a` (this file is committed right after it; run `git log -n 3`).
Nothing has been pushed yet: local `main` is ahead of `origin/main`. No tag or GitHub release exists yet.

## Done (committed)

- Song line breaks: every PPTX reader keeps Shift+Enter (`<a:br/>`) breaks; 648 sections repaired in
  `database\messageflow.db` (496 Kiswahili, 152 English), changing only whitespace and checked against every
  source slide. Backup taken first: `database\backups\messageflow_before_song_linebreaks_20261023.db`.
- Projection: crash on Bible verses and song slides fixed; A-/A+ reset to auto-fit on every new section, verse,
  paragraph and continuation page; a word wider than the screen is never cut; text is never shown below the readable
  minimum; window no longer taller than small high-DPI projectors.
- A missing favorite or history item is never replaced by another paragraph; empty text is never projected.
- Installer: suggests the data drive with the most free space (E:\MessageFlowMedia here); warns on OneDrive folders;
  ships the database without the build machine's favorites and history.
- A fresh install opens in English (the old `%LOCALAPPDATA%\MessageFlow\ui-language.txt` is no longer read).
- Tooltips on every control (EN/FR/SW), light-theme contrast fixes, recoverable error dialog, version 1.0.8.
- Tests: `dotnet test tests\MessageFlow.Tests -c Release` → 19/19 passed.
- Already verified: VerifyProductionData 26/26, VerifyBrotherFrankLibrary 32/32, VerifyChurchUxHotfix 14/14,
  integrity_check = ok on the shipped snapshot, shipped content identical to the working DB
  (only favorites/history emptied), song report for hymns 144 / 48 / 255 → 0 problems after the fix.

## Still running when stopped (must be re-run)

- Installer build from `bd57d5a` (was compressing; no installer exists in `dist\`).
- Full `VerifyLibraryProjectionFit` with the new checks (had passed sermons; stopped during Bible verses).
- Cold/warm launch timings were measured only on an older build and under load: redo them.

## Accident to know about

- A test installer overwrote the old September test install at `E:\MessageFlowMedia` (its database is now a 5 KB
  stub). Reinstall the final installer there, or delete that folder. `E:\MF_Install_Test` holds an older 1.0.8 build;
  ignore it and use `E:\MF_Install_Test2`.

## Do NOT commit

`src\main.py` contains a plaintext Manifest/OpenAI API key (revoke it). Also untracked and unrelated:
`src\index.js`, `package.json`, `package-lock.json`, `node_modules\`, `scratch\`.

## Steps to resume (PowerShell, from the repo root `E:\My Projects\MessageFlow`)

```powershell
# 1. Tests
dotnet test tests\MessageFlow.Tests -c Release

# 2. Full projection verification (~2 h; 0 failures required)
dotnet build tools\VerifyLibraryProjectionFit -c Release
dotnet tools\VerifyLibraryProjectionFit\bin\Release\net10.0-windows\VerifyLibraryProjectionFit.dll --database database\messageflow.db
#    quick song report for the hymns from the live incident:
dotnet tools\VerifyLibraryProjectionFit\bin\Release\net10.0-windows\VerifyLibraryProjectionFit.dll --database database\messageflow.db --song-report 50,176,138,2008,1904,2124

# 3. Rebuild the one installer (~25 min; snapshot, clear favorites/history, integrity_check, publish, ISCC)
Remove-Item -Recurse -Force .\dist\publish, .\dist\MessageFlowMediaSetup.exe -ErrorAction SilentlyContinue
powershell -ExecutionPolicy Bypass -File tools\Installer\build-installer.ps1

# 4. Size and SHA-256
Get-Item dist\MessageFlowMediaSetup.exe | Select Length
Get-FileHash dist\MessageFlowMediaSetup.exe -Algorithm SHA256

# 5. Clean install into a NEW folder on E:
.\dist\MessageFlowMediaSetup.exe /VERYSILENT /SUPPRESSMSGBOXES /CURRENTUSER /DIR=E:\MF_Install_Test2 /TASKS= /LOG=E:\mf-install.log

# 6. Launch, check it opens in English with no favorites/history; then 3 cold + 3 warm timings.
#    Timing script (copy it into the repo if the chat scratchpad is gone):
#    E:\My Projects\UseAiToCite\.tmp\claude\e--My-Projects-MessageFlow\b5c78e8f-d94c-43ad-87f8-1fb8cdbc700b\scratchpad\measure-launch.ps1
#    targets: main window < 3 s, search ready < 15 s first launch, < 5 s later.
#    Startup details: E:\MF_Install_Test2\logs\app-startup.log

# 7. Swahili line-break confirmation on the shipped database (needs the extracted Nyimbo-za-Kiswahili.zip)
Expand-Archive database\custom-library\songs\Nyimbo-za-Kiswahili.zip -DestinationPath E:\swahili-pptx -Force
dotnet build tools\RepairSongLineBreaks -c Release
dotnet tools\RepairSongLineBreaks\bin\Release\net10.0\RepairSongLineBreaks.dll --database E:\MF_Install_Test2\database\messageflow.db --english-source "E:\SONG PRESENTATION" --swahili-source E:\swahili-pptx --verify
#    then project a few Swahili songs (e.g. 372 YESU RAFIKI) and compare with the PowerPoint.

# 8. Website: put the new size and SHA-256 into website\lib\site.ts (installerSize, installerSha256), commit.

# 9. Push and release (installer goes to GitHub Releases, never into git)
git push origin main
gh release create v1.0.8 dist\MessageFlowMediaSetup.exe --title "MessageFlow Media 1.0.8" --notes-file RELEASE_NOTES.md --latest
#    The website's download button uses releases/latest, so it serves 1.0.8 once this release exists.
#    Vercel redeploys on the push (it builds only when website\ changed).
```
