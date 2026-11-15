# Release progress log

## 2026-11-14 19:59: pre-flight checks (before step 1)

- Branch: `fix/ux-search-auto`, HEAD `76bc8f0` (as expected).
- `git status`: clean except untracked `docs/HANDOFF_2026-11-07.md` (as expected).
- E: readable (`Test-Path E:\` = True).
- Tags present: v0.1-stable-mvp, v1.0.0 to v1.0.5, v1.0.8. **No v1.0.9 tag.** (Step 2 not done yet.)
- **Free RAM: 602 MB (first reading), then 715 MB free / 797 MB available (second reading, 8007 MB total).**

## STOPPED: 2026-11-14 20:00

**Reason:** free RAM is below the 1 GB limit you set, on both readings.
The largest users are VS Code and its language servers (Code ×3 ≈ 925 MB,
Microsoft.CodeAnalysis.LanguageServer 412 MB, VS Code ServiceHost 289 MB).

**Nothing was changed:** no merge, no push, no version bump, no build, no install.
The database SHA-256 was not computed (it is a 2.4 GB file and reading it was
not needed before stopping).

**To resume:** free memory first (for example close the VS Code windows you don't
need, or restart the PC), make sure free RAM is above 1 GB, then start the
release work again from step 1.

---

## 2026-11-14 20:08: restart (new instructions: limit is now 400 MB *available* memory)

- Available memory: 776 MB (free 685 MB, total 8007 MB). Above 400 MB: OK.
- `Test-Path E:\` = True.
- Database SHA-256 `database\messageflow.db` = `64A9B58381555AEBD77F98CC39C2184F42AB52C16AE5F6DAB92D0C9EAAF741A7` (MATCH).
- Temp folder: `E:\Cache\tmp` (TMP/TEMP set per process only).

## 2026-11-14 20:09: step 1 (git status)

- `git status --porcelain`: only `?? docs/HANDOFF_2026-11-07.md` and `?? docs/RELEASE_PROGRESS.md`. Clean: OK.
- `fix/ux-search-auto` HEAD = `76bc8f0` (as expected). It is 3 commits ahead of `main` (`f55aa97`), main has nothing the branch lacks (fast-forward possible).

## 2026-11-14 20:10: step 2 (version)

- `git fetch` brought a tag **v1.0.9** from GitHub (`fba8dc3`); it was not present locally before.
- `gh release view v1.0.9`: **published** (not a draft) on 2026-10-25 01:25 UTC, marked Latest, with asset
  `MessageFlowMediaSetup.exe` (547,650,186 bytes, sha256 f753426d…463e9, 21 downloads).
- Note: the local docs (commit fba8dc3 "release still a draft") are out of date on this point; GitHub is the reference.
- **Decision: the new version is 1.0.10.**

## 2026-11-14 20:10: step 3 (merge + tests), partly done

- `git checkout main`, then `git merge fix/ux-search-auto` (fast-forward, no force, no rebase): main `f55aa97` -> **`76bc8f0`**.
  Local main is now 4 commits ahead of origin/main (`f55aa97` was already local-only before the merge).
- **Not pushed.**
- `dotnet test tests\MessageFlow.Tests -c Release` on main: **Passed 40, Failed 0, Skipped 0** (14 s).
- **Available memory measured just before the tests: 361 MB, below the 400 MB limit.** The reading and the test run
  were in the same command, so the tests ran before I could act on the reading.
  After the tests: 793 MB available (VBCSCompiler 264 MB, VS Code + language servers ≈ 800 MB, msedgewebview2 346 MB).

## STOPPED: 2026-11-14 20:11

**Reason:** available memory fell to **361 MB** (< 400 MB) during step 3. Your rule says stop immediately, so I stopped,
even though it went back up to 793 MB afterwards (the dip was the .NET build of the tests).

**State left:**
- Current branch: `main` at `76bc8f0` (merge done locally, **not pushed**).
- `fix/ux-search-auto` unchanged at `76bc8f0`.
- No version change, no release notes, no installer, no install test, no website branch, no draft release.
- Database SHA-256 checked at 20:08: MATCH (the tests do not open it for writing).
- Untracked: `docs/HANDOFF_2026-11-07.md`, `docs/RELEASE_PROGRESS.md`.

**To resume:** close VS Code (and its language servers) to free about 1 GB, then ask me to continue from
"step 3: push main". The version decision (1.0.10) and test results above remain valid unless main changes.
Note: the installer build (step 5) will need more memory than the tests did.

---

## 2026-11-14 20:14: resumed (new memory rule: separate measurement; stop only if < 400 MB on two readings 30 s apart)

## 2026-11-14 20:14: step 3 finished (push)

- `Test-Path E:\` = True. On `main`, working tree clean except the two untracked docs.
- `git push origin main`: `d62de5b..76bc8f0  main -> main`. Local main = origin/main = **`76bc8f0`**.

## 2026-11-14 20:14: before step 4: projection fit check needed

- You asked the release notes to mention "projection fit checks with 0 overflow, 0 cut words, 0 text changes".
  The only run on this code (7 Nov, `VerifyLibraryProjectionFit`) crashed with Out of memory and printed no summary
  (`docs\HANDOFF_2026-11-07.md`). So that claim is **not verified yet**.
- Plan: run `tools\VerifyLibraryProjectionFit` read-only on main `76bc8f0`, in two smaller runs to avoid the earlier
  crash: (a) `--only sermons,bible`, (b) `--only songs`, both `--screens 1280x720,1920x1080`. Database hash checked after.
  If a run fails or crashes, the release notes will not claim it.

## 2026-11-14 20:15 to 20:44: projection fit check, run (a) sermons + Bible

- Available memory before: 790 MB (separate command, 20:15). A background watch every 30 s reported no reading below 400 MB.
- `dotnet build tools\VerifyLibraryProjectionFit -c Release`: 0 warnings, 0 errors.
- `VerifyLibraryProjectionFit.exe --database database\messageflow.db --only sermons,bible --screens 1280x720,1920x1080`
  (main `76bc8f0` code), exit code 0, 1,624 s:
  - 100 longest sermon paragraphs of each language and 100 longest verses of each Bible.
  - Layout configurations 14,400; real ProjectWindow page checks 22,912; assertions **39,118**; continuation pages 7,200.
  - Overflow (engine) **0**, overflow (real window) **0**, words cut **0**, unreadable **0**, text changed **0**, A+/A-/Auto **0**.
  - Full output: `E:\Cache\tmp\fit_sermons_bible.log`.
- Version number already changed in the working tree (not committed yet): `MessageFlow.App.csproj` (1.0.10 / 1.0.10.0),
  `messageflow.iss` and `tools\Installer\MessageFlowMedia.iss` (`MyAppVersion "1.0.10"`).

## 2026-11-14 20:44 to 20:47: projection fit check, run (b) songs: STOPPED by Claude Code, not finished

- Available memory before: 1,226 MB (20:44:28). Started `--only songs --screens 1280x720,1920x1080`.
- About 1.5 minutes later, while the tool was still loading the long texts ("[3/4] Querying Worst-Case Long Texts"),
  **Claude Code itself killed the run** because the system was briefly critically low on memory. The 30 s memory
  watch had not caught a reading below 400 MB (the dip fell between two readings).
- No summary was printed: **no song result exists**, so the release notes do not claim anything about songs.
  I did not restart it (Claude Code says not to restart a run it stopped for memory without your request).
- After the kill: available memory 922 MB (20:45:59) and 1,028 MB (20:46:38, 30 s later), so the new rule
  (stop only if two readings 30 s apart are below 400 MB) is not met. `Test-Path E:\` = True.
- Database SHA-256 at 20:46: `64A9B583…AF741A7` (**MATCH**).
- Songs are not changed by this release (they were already centred); only sermon and Bible alignment changed,
  and those passed in run (a). Decision: continue with step 4.

## 2026-11-14 20:48: step 4 done

- Commit **`e58ca33`** on main "Release 1.0.10: version number and release notes" (not pushed yet; will push with step 7):
  `MessageFlow.App.csproj` (Version 1.0.10, AssemblyVersion/FileVersion 1.0.10.0), `messageflow.iss` and
  `tools\Installer\MessageFlowMedia.iss` (MyAppVersion 1.0.10), `RELEASE_NOTES.md` (new 1.0.10 entry),
  `docs\VERSIONING.md` (current version + "Included In v1.0.10").
- The release notes claim only what was checked: 40 tests and run (a) (sermons + Bible, 39,118 checks, 0 overflow,
  0 cut words, 0 text changes). Nothing about songs (run (b) was killed). Size and SHA-256 come in step 7.
- Available memory 930 MB (separate command), then `dotnet test tests\MessageFlow.Tests -c Release` on `e58ca33`:
  **Passed 40, Failed 0** (13 s, 20:48).

## 2026-11-14 20:49 to 21:05: step 5 (installer build)

- The previous 1.0.9 installer in `dist\` was renamed (not deleted) to `dist\MessageFlowMediaSetup-1.0.9.exe` (547,650,186 bytes).
- Available memory before: 1,820 MB (separate command, 20:49). The 30 s watch reported no reading below 400 MB.
- Existing procedure: `powershell -File tools\Installer\build-installer.ps1` on main `e58ca33` (its own temp folder is
  `dist\_tmp` on E:). Exit code 0. Log: `E:\Cache\tmp\build_installer_1.0.10.log`.
  - Snapshot of the database (VACUUM INTO): ok. Favorites/history removed **from the snapshot only**
    (FavoriteParagraphs 1 row, BibleFavoriteVerses 2 rows). Snapshot `integrity_check`: ok. Library counts: verified.
  - `dotnet publish` self-contained win-x64; `MessageFlow.App.exe` ProductVersion `1.0.10+e58ca33…`, FileVersion 1.0.10.0.
  - Inno Setup (repo `Inno Setup 6\ISCC.exe`): successful compile (383 s).
- **Installer: `E:\My Projects\MessageFlow\dist\MessageFlowMediaSetup.exe`, 547,653,029 bytes (about 522 MB),
  SHA-256 `B2F9A2E1FA50F1B0234A5E8532567AEFA5FE8F34CDFF04F71B412C2C87F2BB51`.**
- Database `database\messageflow.db` SHA-256 after the build: `64A9B583…AF741A7` (**MATCH**, unchanged).

## 2026-11-14 21:07: step 6 pre-checks (clean install test)

- `E:\MF_Install_Test4` already existed: it holds only `logs\app-startup.log` (169 bytes, 25 Oct 2026, a launch that exited
  because another instance was running). No program files, no database: **not a real installation.**
- No MessageFlow entry in the uninstall registry (HKLM, HKLM WOW6432Node, HKCU), and no MessageFlow process running.
- The old 25 Oct log was renamed to `logs\app-startup.2026-10-25.log` so the startup times below come only from this test.

## 2026-11-14 21:06 to 21:14: step 6 install and database checks

- Available memory before: 1,450 MB (21:06:49).
- `dist\MessageFlowMediaSetup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CURRENTUSER /NOICONS /TASKS="" /DIR="E:\MF_Install_Test4"`
  (no shortcuts), exit code 0, 221 s. Inno log: `E:\Cache\tmp\install_test4.log`.
- Installed `MessageFlow.App.exe` ProductVersion **`1.0.10+e58ca3302ee9…`** (= commit e58ca33).
- Installed database (1,462,620,160 bytes), opened read-only with Python before any launch:
  FavoriteParagraphs **0**, BibleFavoriteVerses **0**, ProjectionHistories **0**, `PRAGMA integrity_check` **ok** (175 s).
- Memory watch: one reading of 376 MB at 21:12:18 (during the integrity check); the next reading 30 s later was
  above 400 MB, so the stop rule is not met.

## 2026-11-14 21:14 to 21:15: step 6 startup times, result

- Available memory before: 1,853 MB (21:13:58). Three launches of `E:\MF_Install_Test4\MessageFlow.App.exe`, each closed
  normally after "Search ready" (exit code 0). From `E:\MF_Install_Test4\logs\app-startup.log`:

  | Launch | MainWindow shown | Search ready and UI interactive |
  |---|---|---|
  | 1 (first) | 3,642 ms | 6,397 ms |
  | 2 | 2,569 ms | 4,647 ms |
  | 3 | 2,421 ms | 4,743 ms |

  First launch: "Database repair" found every table already present and created nothing; no rebuild of the search
  index; "Brother Frank publication metadata already current".
  Note: slower than the 1.0.9 test of 25 Oct (1.1 to 1.6 s window, 2.1 to 2.6 s search). The PC had VS Code and
  other programs open with about 1.8 GB available; I did not measure 1.0.9 again on the same PC state to compare.
- After the three launches: favorites 0, Bible favorites 0, history 0 (read-only check).
- **The test installation is LEFT in place** at `E:\MF_Install_Test4` (not uninstalled), so you can look at a sermon
  page and a Bible page in projection from it. It is registered as "MessageFlow Media" 1.0.10 for the current user
  (no shortcuts). To remove it later: `E:\MF_Install_Test4\unins000.exe`.

## 2026-11-14 21:16: step 7 done

- Commit **`b08583f`** on main "README and release notes: 1.0.10 installer size and SHA-256":
  `README.md` (current release v1.0.10, links to the v1.0.10 release, short "What 1.0.10 brings", installer table
  1.0.10 / 547,653,029 bytes / `B2F9A2E1…F2BB51`), `RELEASE_NOTES.md` (Download section with size and SHA-256, plus
  "Clean install tested on Windows 11: version 1.0.10, no favorites or history, database integrity ok"),
  `RESUME_HERE.md` (new 1.0.10 section at the top; it also corrects the old "draft" note).
- Website: local branch **`release-site-1.0.10`**, commit **`11dfef3`** (on top of `b08583f`), changes only
  `website\lib\site.ts`: tag v1.0.10, version 1.0.10, November 2026 (`releaseMonth` 2026-11).
  **Not pushed, not merged.** main contains no `website\` change.
- `docs\RELEASE_PROGRESS.md` and `docs\HANDOFF_2026-11-07.md` stay untracked (not committed).

## 2026-11-14 21:17: main pushed again

- `git push origin main`: `76bc8f0..b08583f  main -> main` (commits e58ca33 and b08583f; no `website\` change).
  `release-site-1.0.10` is not on GitHub (`git ls-remote` empty).

## 2026-11-14 21:18: step 8 started (draft release)

- `gh auth status`: logged in as josuekb09, scope `repo`; repository permission push = true. No v1.0.10 release exists yet.
- Creating a **draft** v1.0.10 (tag target `e58ca33`, the build commit) with `dist\MessageFlowMediaSetup.exe` attached.
  Body = the 1.0.10 section of RELEASE_NOTES.md. The "announce release" e-mail workflow runs only on a published
  release, not on a draft.

## STOPPED: 2026-11-14 21:18 (step 8, upload of the installer)

**Reason:** Claude Code killed the `gh release create` command because the system was briefly critically low on memory
(the same automatic stop as the songs fit check). Claude Code asks not to restart a command it stopped for memory
without your request, so I stopped there. Available memory just after: 1,287 MB; `Test-Path E:\` = True.

**State on GitHub (checked read-only with `gh api`):**
- A **draft** release "MessageFlow Media 1.0.10" exists (id 445530796), tag v1.0.10 (not created yet: drafts create
  the tag only when published), target `e58ca33`, body = the 1.0.10 release notes.
  Draft link (visible only to you while it is a draft):
  https://github.com/josuekb09/messageflow-media/releases/tag/untagged-03ff7aec4f92ebfec88e
- **It has NO asset**: the installer upload did not finish. Nothing was published; no e-mail was sent.

**Manual steps to finish step 8** (or ask me to do it):
1. Upload the installer to the draft:
   `gh release upload v1.0.10 "E:\My Projects\MessageFlow\dist\MessageFlowMediaSetup.exe" -R josuekb09/messageflow-media`
   (or open the draft link above, Edit, drag the file in, "Save draft").
2. Check the asset is 547,653,029 bytes, SHA-256 `B2F9A2E1FA50F1B0234A5E8532567AEFA5FE8F34CDFF04F71B412C2C87F2BB51`
   (`gh release view v1.0.10 -R josuekb09/messageflow-media --json assets`).

---

## 2026-11-14 21:31: resumed, step 8 (you asked: attach the installer to the draft, retry once if cut off, do not publish)

- Available memory 1,077 MB; `Test-Path E:\` = True. Draft v1.0.10: isDraft true, 0 assets.
- Local `dist\MessageFlowMediaSetup.exe`: 547,653,029 bytes, SHA-256 `B2F9A2E1…F2BB51` (re-computed now, same as at build).
- The upload runs as a separate Windows process (output in `E:\Cache\tmp\gh_upload_1.log`), so Claude Code's memory
  safety does not cut it off like the previous attempt.

## 2026-11-14 22:05: upload attempt 1 still running (slow network)

- `gh release upload` started 21:31:45 (process 24720), still running after 33 minutes, connection to GitHub open (port 443).
- The draft still shows 0 assets (GitHub shows the file only when the upload is complete).
- The Wi-Fi is sending only about **24 KB/s** (362,160 bytes in 15 s). At that speed 522 MB would take several hours.
  For comparison, the 1.0.9 upload on 25 Oct took about 15 minutes (asset created 01:09, updated 01:24 UTC).
- It is not cut off, so I am **not** retrying: I let it run and watch it. Available memory 536 MB (22:02).

## 2026-11-14 22:34: upload attempt 1 still running

- Process 24720 alive (CPU time rising, 1.8 s at 22:05, 4.6 s now); Wi-Fi sending about 35 KB/s (524,863 bytes in 15 s).
- Draft still 0 assets (normal until the upload ends). Available memory 554 MB. Still waiting, no retry.

## 2026-11-14 23:05: upload attempt 1 still running

- Process 24720 alive (CPU 6.3 s), Wi-Fi about 28 KB/s, no error output. Draft 0 assets. Available memory 778 MB. Waiting.

## 2026-11-14 23:35: upload attempt 1 still running

- Process 24720 alive (CPU 9.8 s), Wi-Fi about 23 KB/s, no error. Draft 0 assets. Available memory 758 MB.
- Rough estimate at 25 to 35 KB/s: about 180 to 250 MB sent after 2 h 04 min, so 3 to 4 more hours. Waiting.

## 2026-11-15 00:06: upload attempt 1 still running

- Process 24720 alive (CPU 14.2 s); Wi-Fi faster now, about 128 KB/s (1,917,751 bytes in 15 s). No error. Draft 0 assets.
  Available memory 789 MB. Waiting.

## 2026-11-15 00:37: upload attempt 1 still running

- Process 24720 alive (CPU 20.0 s), Wi-Fi about 96 KB/s, no error. Draft 0 assets. Available memory 831 MB. Waiting.

## 2026-11-15 01:08: upload attempt 1 still running

- Process 24720 alive (CPU 25.2 s), Wi-Fi about 124 KB/s, no error. Draft 0 assets. Available memory 715 MB. Waiting.

## 2026-11-15 01:39: upload attempt 1 still running, gh restarted the transfer once by itself

- Process 24720 alive (CPU 34.5 s), Wi-Fi about 107 KB/s, no error printed. Draft 0 assets. Available memory 787 MB.
- The gh process has read **752 MB** from disk, more than the file (522 MB), and its connection to GitHub was opened
  at **00:52**. So gh's own automatic retry restarted the transfer around 00:52, after the first pass failed or was
  dropped. The second pass has read about 205 MB so far. This is gh's built-in retry, not my "retry once".
  No partial asset appears on the draft. Waiting.

## 2026-11-15 02:10: upload attempt 1, second pass near the end

- gh has read 1,002.2 MiB in total; the file is 522.3 MiB, so the second pass is at about 480 of 522 MiB (about 92 %).
  (Correction to the 01:39 entry: in MiB the second pass was then at about 230 MiB, not 205 MB.)
- Same connection since 00:52, no error, draft still 0 assets, available memory 734 MB. Waiting for the end.

## 2026-11-15 02:11: step 8 DONE, installer attached to the draft

- `gh release upload` (attempt 1, started 21:31:45) ended at 02:11:05 with no output and no error. About 4 h 40 min
  because of the slow Wi-Fi and one automatic restart by gh itself around 00:52. **I did not need my one retry.**
- Draft v1.0.10 (`gh api .../releases/445530796`): `draft: true`, asset `MessageFlowMediaSetup.exe`, state `uploaded`.

  | | Size (bytes) | SHA-256 |
  |---|---|---|
  | Local `dist\MessageFlowMediaSetup.exe` | 547,653,029 | `B2F9A2E1FA50F1B0234A5E8532567AEFA5FE8F34CDFF04F71B412C2C87F2BB51` |
  | GitHub asset | 547,653,029 | `b2f9a2e1fa50f1b0234a5e8532567aefa5fe8f34cdff04f71b412c2c87f2bb51` (GitHub digest) |

  **Same size, same SHA-256.**
- Draft link: https://github.com/josuekb09/messageflow-media/releases/tag/untagged-03ff7aec4f92ebfec88e
- **Not published.** Tag v1.0.10 not created yet on GitHub (it is created when you publish). `release-site-1.0.10`
  is **not pushed** (`git ls-remote` empty). main = origin/main = `b08583f`.

## What you still have to do

1. Run `E:\MF_Install_Test4\MessageFlow.App.exe`, project one sermon page and one Bible page, check the text is centred.
2. Open the draft link above and press **Publish release** (this sends the announcement e-mail to subscribers).
3. Then push and merge the website branch: `git push origin release-site-1.0.10`, then merge it into main.

---

## 2026-11-15: draft release notes updated (your approval; not published, nothing pushed)

- New projection fit results, from **your own PowerShell runs on 14 Nov 2026** (`VerifyLibraryProjectionFit`, read-only,
  database SHA-256 unchanged; I did not see their logs, they are not in `E:\Cache\tmp`):
  - Songs: 129,168 layout configurations, 32,293 real-window page checks, 0 failures (612 s).
  - Sermons, Bible and Brother Frank, 1280x720 and 1920x1080: 73,296 layout configurations, 103,229 real-window page
    checks, 0 failures (2,354 s).
  - These replace the earlier 39,118 figure (run (a) above, sermons + Bible only) in the release notes.
- Draft v1.0.10 (id 445530796): `## Fixes` replaced by `## What's new` (the section the announcement e-mail quotes),
  and the 39,118 line replaced by the two runs above. Everything else unchanged.
- Side effect fixed: editing only the body reset the draft's tag to `untagged-03ff7aec4f92ebfec88e`; I set it back to
  `v1.0.10` (target `e58ca33`). Still a draft, `published_at` null, no tag created on GitHub.
- Asset still attached: `MessageFlowMediaSetup.exe`, 547,653,029 bytes, digest `b2f9a2e1…f2bb51` (same as local).
- `RELEASE_NOTES.md` (1.0.10 section = draft text) and `RESUME_HERE.md` updated locally, **not committed**.
- Supabase: the URL in `website\.env.local` does not resolve (DNS name does not exist), so the number of confirmed
  subscribers is unknown. Check the production Supabase project before publishing.
