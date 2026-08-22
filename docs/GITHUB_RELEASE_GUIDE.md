# GitHub Release Guide

This guide prepares a free public GitHub release. Do not upload or publish anything automatically from these instructions.

## Steps

1. Create the GitHub repository.
2. Push the source code.
3. Create a release tag, for example `v1.0.2`.
4. Create a GitHub Release from that tag.
5. Attach this installer (do not commit it to git):

```text
D:\My Projects\MessageFlow\dist\MessageFlowMediaSetup.exe
```

A local copy for the Next.js site lives at `website/public/MessageFlowMediaSetup.exe` (gitignored). Vercel cannot host this ~563 MB file; the public download button should open the GitHub Release.

## Release Title

```text
MessageFlow Media v1.0.2
```

## Release Notes

See [RELEASE_NOTES.md](../RELEASE_NOTES.md) for the current inventory (English, French, and Kiswahili sermons, Bibles, and songbooks).

## Distribution Notes

- Keep source code in the GitHub repository.
- Attach the installer executable to the GitHub Release.
- Do not commit the installer executable directly into normal git history unless that decision is made intentionally.
- Keep MessageFlow Media free.
- Do not add paid subscriptions, ads, in-app purchases, or fundraising gates.
