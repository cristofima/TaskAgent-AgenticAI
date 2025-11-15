import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: "export", // Generate static HTML export for Azure Static Web Apps
  images: {
    unoptimized: true, // Disable image optimization (not available in static export)
  },
};

export default nextConfig;
