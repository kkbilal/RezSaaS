import type { NextConfig } from "next";

const apiBaseUrl = process.env.REZSAAS_API_BASE_URL ?? "http://localhost:5252";

const nextConfig: NextConfig = {
  reactStrictMode: true,
  turbopack: {
    root: process.cwd()
  },
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${apiBaseUrl}/api/:path*`
      }
    ];
  }
};

export default nextConfig;
