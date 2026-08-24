import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  turbopack: {
    root: process.cwd(),
  },
  async redirects() {
    return [
      {
        source: "/MessageFlowMediaSetup.exe",
        destination:
          "https://github.com/josuekb09/messageflow-media/releases/download/v1.0.2/MessageFlowMediaSetup.exe",
        permanent: false,
      },
    ];
  },
};

export default nextConfig;
