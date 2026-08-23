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

The `/feedback` form posts to `app/api/feedback/route.ts`, which forwards to [Web3Forms](https://web3forms.com) (`https://api.web3forms.com/submit`). Destination inbox: `kabuyatambwe03@gmail.com` (bound to the access key you create, plus a `to` field in the payload).

1. Sign up at https://web3forms.com with **kabuyatambwe03@gmail.com** and copy the access key.
2. Local: copy `website/.env.example` to `website/.env.local` and set `WEB3FORMS_ACCESS_KEY`.
3. Production: Vercel project **messageflow-media** → **Settings → Environment Variables** → add `WEB3FORMS_ACCESS_KEY` for Production, Preview, and Development. Redeploy after saving.

The form UI still loads if the key is missing; submit then shows a configuration error. Keep the key server-side (do not use `NEXT_PUBLIC_`). Web3Forms documents that server-side submits may need IP allowlisting on paid plans — if Vercel deliveries fail after the key is set, check the Web3Forms inbox/spam folder and their dashboard.
