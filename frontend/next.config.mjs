/** @type {import('next').NextConfig} */
const nextConfig = {
  // Allow server components to fetch from the local API during dev
  experimental: {},
  async rewrites() {
    return [
      {
        source: '/api/v1/:path*',
        destination: 'https://careerpath-api-bharat-gqbngkhmhhbhzdrb8.centralindia-01.azurewebsites.net/api/v1/:path*',
      },
    ]
  },
}

export default nextConfig
