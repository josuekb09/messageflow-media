import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  turbopack: {
    root: process.cwd(),
  },
  async rewrites() {
    return [
      {
        source: "/MessageFlowMediaSetup.exe",
        destination: "/api/installer",
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
