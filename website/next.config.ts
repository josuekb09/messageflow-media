import type { NextConfig } from "next";

const installerUrl =
  process.env.INSTALLER_BLOB_URL ??
  "https://github.com/josuekb09/messageflow-media/releases/download/v1.0.2/MessageFlowMediaSetup.exe";

const nextConfig: NextConfig = {
  turbopack: {
    root: process.cwd(),
  },
  async redirects() {
    return [
      {
        source: "/MessageFlowMediaSetup.exe",
        destination: installerUrl,
        permanent: false,
      },
    ];
  },
};

export default nextConfig;
