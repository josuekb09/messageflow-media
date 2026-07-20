# MessageFlow Media Church Installation Checklist

## Install

1. Copy the approved installer or complete release folder to the church computer.
2. Run `MessageFlowMediaSetup.exe` and follow the prompts. Administrator permission is not normally required for the current per-user installer.
3. Launch MessageFlow Media from the desktop shortcut or Start menu.
4. Confirm Bible, Songs, and Sermons appear before the service.

## Configure church displays

1. Connect the TV/projector by HDMI or the church display system.
2. Press **Windows + P** and choose **Extend**.
3. Open Windows Display Settings and confirm the operator monitor and church display are separate logical displays.
4. In MessageFlow, open **Admin**.
5. Select the external church display under Projection Display. Use **Refresh Displays** after connecting or changing Windows + P mode.
6. Run **Test Projection Display**.
7. Confirm the test output is borderless on the church display while MessageFlow controls remain on the operator monitor.

## Service test content

- Bible: 1 Samuel 2:2, John 3:16, Romans 8:4, and Psalm 23.
- Songs: Song 116 slide 2 and Song 110.
- Sermons: Why Little Bethlehem and Wedding Ceremony.
- Confirm A-, Fit, and A+ work and that sermon Previous Page / Next Page still paginate.
- Project one item, then search for another. The church display must retain the first item until **Project** is clicked again.

## Single-monitor behavior

With only one Windows display, real Project opens a normal resizable desktop window. It may be dragged, resized, minimized, maximized, restored, or closed. The taskbar and MainWindow remain available. The same window is reused by later Project actions.

With an external display in Extend mode, real Project uses borderless fullscreen on the selected non-primary display.

## Closing projection safely

- In single-monitor mode, use the projection window's normal close button.
- Press **Escape** while a projection window is open to close projection output.
- Closing projection does not close MessageFlow or erase the retained live snapshot.

## Troubleshooting

- Wrong screen: choose **Extend**, click **Refresh Displays**, select the external display, and test again.
- Projector disconnected: click Project again; MessageFlow safely falls back to a normal window on the remaining display.
- Projection window off-screen: disconnect/reconnect the display or click Project; display topology and bounds are re-evaluated.
- Missing content: keep the bundled `database` folder beside `MessageFlow.App.exe` and restore it from the approved release package if necessary.
- Security warning: verify the installer SHA-256 against the release handoff record before running it.

## Backup and recovery

- Use **Admin > Backup Database** before database maintenance or importing new content.
- Keep the approved installer/release package and a recent database backup off the church computer.
- Do not replace the production database with an unknown or partially copied SQLite file.
