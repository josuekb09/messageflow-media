import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  turbopack: {
    root: process.cwd(),
  },
  images: {
    // 85 keeps the small text in the app screenshots sharp; 75 is the default.
    qualities: [75, 85],
  },
};

export default nextConfig;
