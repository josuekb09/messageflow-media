# MessageFlow Media Command Audit

Audit date: 2026-07-20

| Screen / section | Visible action | Command / handler | Enabled condition | Expected and verified behavior | Result |
|---|---|---|---|---|---|
| Main toolbar | Sermons / Bible / Songs tabs | WPF tab selection | Always | Changes operator workspace and Preview only; live snapshot is unchanged | Pass |
| Main toolbar | Search fields | Debounced search setters | Relevant module available | Updates private results and suggestions only | Pass |
| Main toolbar | Author / Source / Year filters | Selection bindings | Sermon mode | Filters private sermon results only | Pass |
| Main toolbar | Projection text-size selector | `SelectedProjectionFontSize` | Always | Changes projection fitting preference without changing content | Pass |
| Main toolbar | A- | `DecreaseProjectionTextSizeCommand` | Above adjustment minimum | Reduces live projection scale | Pass |
| Main toolbar | Fit | `ResetProjectionTextSizeCommand` | Always | Restores maximum safe Bible/Song fit or sermon default | Pass |
| Main toolbar | A+ | `IncreaseProjectionTextSizeCommand` | Below adjustment maximum | Increases preference without permitting Bible/Song clipping | Pass |
| Library | Result selection | Selection binding | Result exists | Updates local Preview only | Pass |
| Preview | Project | `ProjectCommand` | Valid Bible verse, Song section, or Sermon paragraph selected | Creates immutable snapshot, then updates/reuses one projection window | Pass |
| Preview | Copy | `CopyCommand` | Valid selection | Copies selected Preview text | Pass |
| Preview | Previous / Next Bible verse or Song section | `PreviousParagraphCommand` / `NextParagraphCommand` | Valid neighboring item | Updates Preview and atomically advances live output only when the open projection's stable source/item identity matches the pre-navigation selection; otherwise remains Preview-only | Pass |
| Preview | Previous / Next sermon paragraph | `PreviousParagraphCommand` / `NextParagraphCommand` | Valid neighboring item | Navigates private Preview selection | Pass |
| Preview | Previous / Next projected page | Projection page commands | Live sermon has relevant page | Intentionally changes the displayed sermon page | Pass |
| Preview | Add / Remove Favorite | `ToggleFavoriteCommand` | Valid Bible/Sermon selection | Updates applicable favorite; hidden for Songs because Song favorites are not implemented | Pass |
| Favorites | Project | Favorite project commands | Favorite exists | Selects and explicitly snapshots favorite content | Pass |
| Favorites | Copy Bible favorite | `CopyBibleFavoriteCommand` | Bible favorite exists | Copies verse text | Pass |
| Favorites | Remove Favorite | Remove commands | Favorite exists and database idle | Removes selected favorite after operator action | Pass |
| History | Clear History | `ClearHistoryCommand` | History exists and database idle | Clears projection history | Pass |
| History | Project | `ProjectHistoryCommand` | History item exists | Explicitly projects saved sermon content | Pass |
| Status bar | Admin | `Admin_Click` | Always | Opens or focuses Admin Tools | Pass |
| Admin / Sources | Manage Sources | `ManageSourcesCommand` | Database idle | Opens source-management window | Pass |
| Admin / Sources | Add Source | `AddNewSourceCommand` | Database idle | Opens validated source dialog | Pass |
| Admin / Sources | Import Source | `ImportSourceCommand` | Source selected and database idle | Opens import workflow | Pass |
| Admin / Sources | Repair Source Metadata | `RepairSourceMetadataCommand` | Source selected and database idle | Runs focused metadata repair | Pass |
| Admin / Bible | Import Bible | `ImportBibleCommand` | Database idle | Opens preview-first CSV import | Pass |
| Admin / Database | Backup / Restore / Open Backup Folder | Corresponding commands | Database idle; folder command additionally requires a backup | Performs guarded database maintenance | Pass |
| Admin / Verification | Verify Production Data | `VerifyProductionDataCommand` | Database idle | Runs built-in production checks and displays report | Pass |
| Admin / Cleanup | Cleanup Test Data | `CleanupTestDataCommand` | Database idle | Uses confirmation/preview dialog before cleanup | Pass |
| Admin / Cleanup | Cleanup Brother Frank Circular Letters | Corresponding command | Database idle | Runs targeted, confirmed cleanup | Pass |
| Admin / Projection | Display selector | `SelectedProjectionDisplayOption` | Display enumerated | Saves preference; real Project safely falls back if unavailable | Pass |
| Admin / Projection | Test Projection Display | `TestProjectionDisplayCommand` | Database idle | Opens test output using adaptive display behavior | Pass |
| Admin / Projection | Open Projection Preview Window | `OpenProjectionPreviewCommand` | Valid Preview selection | Opens explicit normal window without changing snapshot content | Pass |
| Admin / Projection | Refresh Displays | `RefreshProjectionDisplaysCommand` | Always | Re-enumerates current Windows displays | Pass |
| Dialogs | Browse / Preview / Save / Start Import / Cancel / Close | Named click handlers | Dialog-specific validation | All handlers contain concrete actions; import remains preview/validation guarded | Pass |
| Keyboard | Ctrl+P | `ProjectCommand` | Valid Preview selection | Dedicated intentional projection shortcut | Pass |
| Keyboard | Enter in Bible search/results | Bible navigation activation | Bible focus | Opens/selects Preview only | Pass |
| Keyboard | Enter in sermon search | `SearchNowAsync` | Sermon search focus | Searches/selects Preview; no live projection | Pass |
| Keyboard | Escape | Projection close logic | Projection exists | Closes projection output without closing MainWindow | Pass |
| Projection window | Normal title-bar controls | Windows window chrome | Single-display/windowed mode | Drag, resize, minimize, maximize, restore, and close | Pass by configuration; manual interaction required |

## Findings

- No visible button is bound to an empty handler.
- No placeholder or `NotImplementedException` blocks production workflows.
- Song favorites are intentionally unavailable and the favorite button is hidden in Song mode.
- The active UI has no separate Blank, Black, Clear Projection, or Stop button. Closing the projection is the intentional clearing operation.
- Global Enter-to-Project was removed. Ctrl+P remains the dedicated projection shortcut.
