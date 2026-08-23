# MessageFlow website

Light-themed landing site for the MessageFlow Windows desktop application.

## Logo

The blue wave mark is copied from the WPF app:

- Source: `src/MessageFlow.App/Assets/Brand/messageflow-mark.svg`
- Site file: `website/public/brand/mark.svg`

The app icon is copied from:

- Source: `src/MessageFlow.App/Assets/Brand/messageflow-app-icon.svg`
- Site file: `website/public/brand/app-icon.svg`

## Run locally

```powershell
cd website
$env:TEMP = "D:\Temp"; $env:TMP = "D:\Temp"
npm run dev
```

Open http://localhost:3000

Place `MessageFlowMediaSetup.exe` in `public/` before publishing downloads.

Product screenshots and the compressed demo video live in `public/media/`.

## Deploy

This site lives in `website/`. `vercel.json` cannot set `rootDirectory` (it is not in the official schema). Set it on the Vercel project:

1. Dashboard: **Settings → General → Root Directory → `website`**
2. Or the Vercel API: `PATCH /v9/projects/:id` with `{ "rootDirectory": "website", "framework": "nextjs" }`

`website/vercel.json` then applies. The project **messageflow-media** is already set this way.

The root `vercel.json` is only a fallback if Root Directory is reset to `.`. That path cannot deploy this Next.js app: Vercel looks for `next` in the repository-root `package.json` and fails. Keep Root Directory = `website`.

Production: https://messageflow-media.vercel.app

The Windows installer is too large for Git or Vercel. Keep `MessageFlowMediaSetup.exe` in `public/` for local downloads only. Production still uses the same `/MessageFlowMediaSetup.exe` path when the file is served from that host.

## Feedback & Support

The `/feedback` form posts to `app/api/feedback/route.ts`. Submissions go to **kabuyatambwe03@gmail.com**.

Default delivery is [FormSubmit](https://formsubmit.co) AJAX (`https://formsubmit.co/ajax/kabuyatambwe03@gmail.com`). No access key or Vercel env var is required.

The first live submit to a new FormSubmit inbox sends a confirmation email to that Gmail. Open it and click the confirmation link so later messages are forwarded. Check spam if it does not appear.

[Web3Forms](https://web3forms.com) is optional. If `WEB3FORMS_ACCESS_KEY` is set (local `.env.local` or Vercel env), the route tries Web3Forms first and falls back to FormSubmit. Keep the key server-side (do not use `NEXT_PUBLIC_`). The form never blocks on a missing key.
