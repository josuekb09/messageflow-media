# Known limitations

Behaviour that is understood, measured, and accepted for now. Each entry records what was
already ruled out, so a later investigation starts where the last one stopped instead of
repeating it.

## Browsing a large library stalls for about six seconds

**Symptom.** On the Sermons tab, switching the source filter to Brother Branham with the
search box empty lists 1,203 documents and freezes the interface for roughly five to seven
seconds. The status bar reports the delay itself (for example, "1,203 documents found in
7,300 ms").

**Scope.** Only the empty-search browse path is affected, because it is the only list in the
app that exceeds 200 items:

- Searching inside a library is capped at 200 results and stays fast.
- Brother Frank (85 documents) is instant.
- Songs (200 max), Bible navigation (66 books, at most 150 chapters) and every other list are
  well under the threshold.

The delay scales with the number of items, so it will grow if a library larger than Brother
Branham's is ever imported.

**Where the time goes.** Measured in a Release build with temporary instrumentation, switching
Brother Frank to Brother Branham:

| Stage | Time |
| --- | --- |
| `BrowseSermonsAsync` (the SQL query, end to end) | 804 ms |
| `ParagraphResultViewModel` construction (1,203 items) | 176 ms |
| `ApplyFavoriteStateAsync` | 24 ms |
| `SetResults` grouping and ordering | 1 ms |
| `SermonResults.ReplaceAll` | **7,147 ms** |

Breaking down that last figure further:

| Inside `ReplaceAll` | Time |
| --- | --- |
| Collection mutation (clear plus 1,203 appends) | 0.1 ms |
| One full linear scan using the element type's `Equals` | 0.1 ms |
| `OnCollectionChanged(Reset)` | **7,199 ms** |

The entire cost is WPF's synchronous reaction to a single `CollectionChanged` Reset on a
`ListBox` whose `SelectedItem` is bound. No application code is slow.

**Already ruled out.** Do not spend time re-testing these:

- *The database.* The query runs in 40-55 ms warm against SQLite directly, and under a second
  through the app. It uses the indexes from migration
  `20260627224500_AddSearchPerformanceIndexesAndFts` and does not scan.
- *Collection population strategy.* Adding items one at a time was the original cause of a
  related stall and was already fixed; `BulkObservableCollection.ReplaceAll` now raises one
  Reset. The mutation itself costs 0.1 ms.
- *Structural equality.* `SermonResultViewModel` is a positional `record`, so it has
  field-by-field equality. This was suspected, then refuted: a full 1,203-element scan using
  that equality takes 0.1 ms. Its first field is an `int`, so comparisons short-circuit before
  reaching any string.
- *Virtualization being silently off.* Confirmed active at the moment of the reset:
  `IsVirtualizing=True`, the items host is a real `VirtualizingStackPanel`, and only 85
  containers were realized before the switch rather than 1,203.
- *`VirtualizationMode`.* `Standard` behaves the same as `Recycling` (7,199 ms).
- *`ScrollUnit`.* `Item` behaves the same as `Pixel`.

**Root cause.** Not identified. What is established is that the cost is inside WPF's own Reset
handling, is proportional to collection size, and is not attributable to any of the settings
above. Plausible remaining directions, none tested:

- Container teardown and regeneration for the whole item range on Reset, rather than
  incremental updates.
- `Selector` selection bookkeeping interacting with the two-way `SelectedItem` binding
  (`MainWindow.xaml`, bound to `SelectedSermon`).
- Extent estimation for variable-height item templates; the sermon card has `MinHeight="92"`
  but no fixed height.

**Possible directions if this is picked up again.** Untested suggestions, not
recommendations:

- Replace the single Reset with a sequence of incremental `Add` notifications in batches, or
  page the browse list, so WPF never sees a 1,200-item reset.
- Bind the list to a fresh collection instance per browse instead of mutating one in place.
- Reproduce in a minimal WPF project to determine whether this is inherent to `ListBox` plus a
  bound `SelectedItem` at this item count.

**Why it is not blocking.** It affects one navigation action, is not a crash or data problem,
and the library content is correct. Operators browsing a whole library see a several-second
pause once per filter switch; every other path in the app is unaffected.

**History.** Investigated 22 September 2026. The instrumentation used to produce these numbers
was temporary and has been reverted; the virtualization fix it was built on top of is commit
`b01057e`. Re-adding equivalent `[PERF]` timing around `LoadSearchResultsAsync`, `SetResults`
and `BulkObservableCollection.ReplaceAll` reproduces the table above.
