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

On Vercel, set Root Directory to `website`.
