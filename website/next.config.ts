import type { NextConfig } from "next";

const installerUrl =
  process.env.INSTALLER_BLOB_URL ??
  "https://github.com/josuekb09/messageflow-media/releases/download/v1.0.2/MessageFlowMediaSetup.exe";

const nextConfig: NextConfig = {
  turbopack: {
    root: process.cwd(),
  },
  // Proxy the file so the browser stays on this site and saves the .exe.
  // A redirect sends people to GitHub instead of downloading.
  async rewrites() {
    return [
      {
        source: "/MessageFlowMediaSetup.exe",
        destination: installerUrl,
      },
    ];
  },
  async headers() {
    return [
      {
        source: "/MessageFlowMediaSetup.exe",
        headers: [
          {
            key: "Content-Type",
            value: "application/octet-stream",
          },
          {
            key: "Content-Disposition",
            value: 'attachment; filename="MessageFlowMediaSetup.exe"',
          },
        ],
      },
    ];
  },
};

export default nextConfig;
