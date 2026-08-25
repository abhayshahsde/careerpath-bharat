import type { Metadata } from 'next'
import { Inter, Outfit } from 'next/font/google'
import './globals.css'
import Navbar from '@/components/Navbar'
import AiChatWidget from '@/components/AiChatWidget'
import { AuthProvider } from '@/lib/auth-context'
import { cookies } from 'next/headers'
import { translate } from '@/lib/i18n'

const inter = Inter({
  subsets: ['latin'],
  variable: '--font-inter',
  display: 'swap',
})

const outfit = Outfit({
  subsets: ['latin'],
  variable: '--font-outfit',
  display: 'swap',
})

export const metadata: Metadata = {
  title: {
    default: 'CareerPath Bharat — Discover Your Career',
    template: '%s | CareerPath Bharat',
  },
  description:
    'India\'s most comprehensive career discovery platform. Explore careers, entrance exams, courses, and scholarships curated for Indian students.',
  keywords: ['career guidance India', 'entrance exams', 'courses', 'scholarships', 'UPSC', 'JEE', 'NEET'],
  openGraph: {
    type: 'website',
    locale: 'en_IN',
    siteName: 'CareerPath Bharat',
  },
}

export default function RootLayout({ children }: { children: React.ReactNode }) {
  const cookieStore = cookies();
  const locale = cookieStore.get('locale')?.value ?? 'en';

  return (
    <html lang={locale}>
      <head>
        <script
          dangerouslySetInnerHTML={{
            __html: `
              (function() {
                try {
                  var saved = localStorage.getItem('theme');
                  if (saved === 'dark') {
                    document.documentElement.classList.add('dark');
                  } else {
                    document.documentElement.classList.remove('dark');
                  }
                } catch (_) {}
              })();
            `
          }}
        />
      </head>
      <body className={`${inter.variable} ${outfit.variable} min-h-screen`}>
        <AuthProvider>
          <Navbar />
          <main>{children}</main>
          <AiChatWidget />
          <footer className="border-t border-white/10 py-10 mt-20">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center text-white/40 text-sm">
              <p>{translate('footerText', locale)}</p>
            </div>
          </footer>
        </AuthProvider>
      </body>
    </html>
  )
}
