/** @type {import('next').NextConfig} */
const nextConfig = {
  // Allow server components to fetch from the local API during dev
  experimental: {},
  async rewrites() {
    return []
  },
}

export default nextConfig
