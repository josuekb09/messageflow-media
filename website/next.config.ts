import type { NextConfig } from "next";

const installerUrl =
  process.env.INSTALLER_BLOB_URL ??
  "https://github.com/josuekb09/messageflow-media/releases/download/v1.0.3/MessageFlowMediaSetup.exe";

const nextConfig: NextConfig = {
  turbopack: {
    root: process.cwd(),
  },
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
