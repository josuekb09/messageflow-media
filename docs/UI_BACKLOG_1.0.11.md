# UI backlog for MessageFlow Media 1.0.11

Written 2026-11-15 from the 1.0.10 screenshots in `docs/screenshots-2026-11-15/`.
No code has been changed yet. Line numbers are for `src/MessageFlow.App/MainWindow.xaml`
at commit `e58ca33` (1.0.10) and may move.

Effort: **S** = under half a day, **M** = one to two days, **L** = several days.

## Bugs and layout fixes

### 1. Bible tab: status message overlaps the "Bible Preview" title — S
- **What:** after projecting, `StatusText` ("Projection opened in windowed mode…", set in
  `ViewModels/MainViewModel.cs` around line 5078) runs into the centre panel title.
- **Where:** centre panel header, `MainWindow.xaml` ~1362–1400: a two-row `Grid`
  (`Margin="16,14,16,10"`) with the title `TextBlock` (`Text="{Binding CenterPanelTitle}"`,
  `TextWrapping="Wrap"`) in row 0, column 1, and the status `TextBlock` in row 1,
  `Grid.ColumnSpan="2"`, `HorizontalAlignment="Right"`, `TextWrapping="Wrap"`.
- **Fix:** give the status its own row with a minimum height and `TextTrimming="CharacterEllipsis"`
  (full text in a `ToolTip`), or move long projection messages to the bottom status bar only
  (it already shows `StatusText` at ~1896). Check at 1280x720 with the longest
  French and Kiswahili messages.

### 2. Search boxes: inconsistent left padding — S
- **What:** the typed text sits further right in the Bible search box than in the sermon box
  (about 34 px versus 30 px from the border in the screenshots), and the song box looks
  different again.
- **Where:** `SearchBox` (~228, top bar, `Width="240"`), `BibleSearchBox` (~750) and
  `SongSearchBox` (~875) all use the default `TextBox` style in `App.xaml` (~29,
  `Padding="14,10"`, custom `Template`). The extra offset comes from the template's
  inner content host or from the containers around the Bible and song boxes.
- **Fix:** one named style, for example `SearchTextBoxStyle`, with a single padding
  (suggest `12,0` plus `VerticalContentAlignment="Center"`), a search glyph on the left and
  a clear button on the right; apply it to all three boxes and to `SermonWithinSearchBox` (~1715).

### 3. Top bar height shifts when switching tabs — S
- **What:** the top bar is taller on Sermons than on Bible and Songs. You measured about 4 px;
  the screenshots suggest up to about 10 px (header bottom line at about 116 px on
  Sermons and 106 px on Bible, at 1456 px wide).
- **Where:** `SearchBox` (~228) has `Margin="0,0,10,6"` and a style trigger that sets
  `Visibility="Collapsed"` when `IsBibleMode` or `IsSongsMode` is true (~237–246), so the
  `WrapPanel` in the header (~226) loses its height.
- **Fix:** use `Visibility="Hidden"` (keeps the space) or give the header row a fixed
  `Height`/`MinHeight` and centre the controls vertically. Verify at 1280x720 and when
  the `WrapPanel` wraps on narrow windows.

## Polish ideas

### 4. Colour tokens — M
- Gather the colours in `Themes/DarkTheme.xaml` and `Themes/LightTheme.xaml` into a small set of named
  brushes, matching the website: surface, card, line, text (primary, secondary, muted), and the
  cyan accent (#38BDF8, with a darker #0369A1 for accent text in the light theme).
- The Project/Copy buttons use a saturated royal blue that does not match the cyan accent.
  Use one accent fill for the primary action (Project) and an outline or neutral style for
  secondary actions (Copy, Previous/Next, Add Favorite), so the main action stands out.
- Check AA contrast for the muted text and the cyan links in the light theme.

### 5. One icon set for the left rail — M
- **Where:** `NavigationRailButtonStyle` (~29) and the rail buttons (~350–400). Today the
  icons are a mix of Segoe Fluent glyphs and hand-drawn `Path` icons (book, cross, note).
- **Idea:** draw all five (Sermons, Bible, Songs, Favorites, History) as `Path` geometry from one
  set (Lucide, ISC licence, the same set as the website: book-open-text, cross, music, star,
  history) at one stroke width, with the same 24 px grid. Add tooltips and a
  selected-state pill behind the icon, not only the coloured bar.

### 6. Motion — M
- **Tab cross-fade:** fade and 8 px slide of the library and centre panels when switching
  Sermons, Bible and Songs (150–200 ms, `DoubleAnimation` on `Opacity` and a `TranslateTransform`).
- **Selected card:** animate the border colour and background of the selected result card
  (`ListBoxItem` `IsSelected` trigger with `ColorAnimation`, about 150 ms) instead of an instant swap.
- **Button press feedback:** scale to 0.97 on `IsPressed` (`ScaleTransform`, 80 ms) and a
  short hover colour transition on all buttons.
- Respect Windows "Show animations" (`SystemParameters.ClientAreaAnimation`): no animation when off.
- Never animate the projection window text itself.

### 7. Empty states — S
- When a search has no results, or before the first search, show a centred icon, one short
  line ("No sermons match 'xyz'. Try fewer words or a paragraph number."), and one hint,
  instead of an empty list. Same for Favorites and History when they are empty.
- Translate the texts in English, French and Kiswahili like the other strings (`loc:Tr`).
