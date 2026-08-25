/* eslint-disable @typescript-eslint/no-explicit-any */
import Link from 'next/link'
import { cookies } from 'next/headers'
import { ArrowRight, Compass, BookOpen, GraduationCap, Award } from 'lucide-react'
import { api } from '@/lib/api'
import { translate } from '@/lib/i18n'

export const metadata = {
  title: 'CareerPath Bharat — Discover Your Career',
  description: "India's most comprehensive career discovery platform for students.",
}

async function getHomeData(locale: string) {
  const [careers, categories] = await Promise.all([
    api.getCareers({ pageSize: 6, locale }).catch(() => ({ items: [], totalCount: 0 })),
    api.getCategories().catch(() => []),
  ])
  return { careers, categories }
}

export default async function HomePage() {
  const cookieStore = cookies()
  const loc = cookieStore.get('locale')?.value ?? 'en'
  const { categories } = await getHomeData(loc)

  const stats = [
    { label: translate('careerPaths', loc),   value: '200+', icon: Compass },
    { label: translate('entranceExams', loc), value: '50+',  icon: BookOpen },
    { label: translate('courses', loc),        value: '500+', icon: GraduationCap },
    { label: translate('scholarships', loc),   value: '100+', icon: Award },
  ]

  return (
    <div className="page-enter">
      {/* Hero Section */}
      <section className="relative overflow-hidden bg-hero-gradient min-h-[85vh] flex items-center py-16">
        {/* Background glow accents */}
        <div className="absolute inset-0 pointer-events-none">
          <div className="absolute top-1/4 left-1/4 w-[500px] h-[500px] bg-blue-500/10 dark:bg-brand-500/10 rounded-full blur-3xl animate-float" />
          <div className="absolute bottom-1/4 right-1/4 w-[500px] h-[500px] bg-indigo-500/10 dark:bg-accent-purple/10 rounded-full blur-3xl animate-float" style={{ animationDelay: '3s' }} />
        </div>

        <div className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          {/* Top Pill Badge */}
          <div className="inline-flex items-center gap-2 px-4 py-2 rounded-full glass border shadow-sm text-sm font-semibold mb-8"
            style={{ borderColor: 'var(--border-color)', color: 'var(--text-primary)' }}>
            <div className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
            <span className="text-brand-400 font-bold">#1</span>
            <span>{loc === 'hi' ? 'भारत का प्रमुख करियर और शिक्षा मंच' : "India's Premier Career Discovery Platform"}</span>
          </div>

          {/* Main Headline */}
          <h1 className="font-display font-black text-4xl sm:text-6xl md:text-7xl leading-tight mb-6" style={{ color: 'var(--text-primary)' }}>
            {loc === 'hi' ? (
              <>
                अपना <span className="gradient-text">आदर्श करियर</span> खोजें
                <br />
                <span className="opacity-80">भारत में</span>
              </>
            ) : (
              <>
                Discover Your <span className="gradient-text">Perfect Career</span>
                <br />
                <span className="opacity-80">in India</span>
              </>
            )}
          </h1>

          <p className="text-lg sm:text-xl max-w-2xl mx-auto mb-10 leading-relaxed" style={{ color: 'var(--text-secondary)' }}>
            {translate('heroSub', loc)}
          </p>

          {/* Action CTAs */}
          <div className="flex flex-col sm:flex-row gap-4 justify-center items-center">
            <Link href={`/careers?locale=${loc}`} className="btn-brand text-base px-8 py-4 flex items-center gap-2.5 justify-center font-bold !text-white shadow-xl hover:scale-105 transition-all">
              <Compass className="w-5 h-5 text-white" />
              <span>{loc === 'hi' ? 'करियर ब्राउज़ करें' : 'Explore Careers'}</span>
              <ArrowRight className="w-4 h-4 text-white" />
            </Link>
            <Link href={`/auth/register?locale=${loc}`} className="glass-button text-base px-8 py-4 font-bold border shadow-md hover:scale-105 transition-all" style={{ borderColor: 'var(--border-color)', color: 'var(--text-primary)' }}>
              {loc === 'hi' ? 'मुफ़्त खाता बनाएं' : 'Create Free Account'}
            </Link>
          </div>

          {/* Metric Stats Cards */}
          <div className="mt-16 grid grid-cols-2 md:grid-cols-4 gap-4 sm:gap-6">
            {stats.map(({ label, value, icon: Icon }) => (
              <div key={label} className="glass rounded-3xl p-6 text-center border shadow-sm transition-all duration-300 hover:-translate-y-1 hover:shadow-lg" style={{ borderColor: 'var(--border-color)' }}>
                <div className="w-12 h-12 rounded-2xl bg-brand-gradient flex items-center justify-center mx-auto mb-3 shadow-brand">
                  <Icon className="w-6 h-6 text-white" />
                </div>
                <div className="text-3xl font-black font-display" style={{ color: 'var(--text-primary)' }}>{value}</div>
                <div className="text-xs font-semibold mt-1" style={{ color: 'var(--text-muted)' }}>{label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Featured Career Domains Section */}
      <section className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-20">
        <div className="flex flex-col md:flex-row md:items-end justify-between mb-12 gap-4">
          <div>
            <div className="badge-brand mb-3">
              <Compass className="w-3.5 h-3.5" />
              <span>{loc === 'hi' ? 'प्रमुख क्षेत्र' : 'Top Industry Sectors'}</span>
            </div>
            <h2 className="section-heading">
              {loc === 'hi' ? 'लोकप्रिय करियर श्रेणियां' : 'Explore High-Growth Domains'}
            </h2>
            <p className="section-sub">
              {loc === 'hi' 
                ? 'तकनीक, चिकित्सा, वित्त, सिविल सेवा और डिज़ाइन में सबसे अधिक मांग वाले करियर का अन्वेषण करें।'
                : 'Browse trending careers across Technology, Healthcare, Finance, Civil Services, and Design.'}
            </p>
          </div>
          <Link href={`/careers?locale=${loc}`} className="btn-brand text-xs px-5 py-2.5 font-bold self-start md:self-auto flex items-center gap-2">
            <span>{loc === 'hi' ? 'सभी 200+ करियर देखें' : 'View All 200+ Careers'}</span>
            <ArrowRight className="w-4 h-4" />
          </Link>
        </div>

        {/* Categories Grid */}
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
          {categories.slice(0, 6).map((cat: any) => (
            <Link
              key={cat.id || cat.name}
              href={`/careers?category=${encodeURIComponent(cat.name)}&locale=${loc}`}
              className="glass-card group border transition-all duration-300 hover:scale-[1.02] hover:shadow-xl"
              style={{ borderColor: 'var(--border-color)' }}
            >
              <div className="w-12 h-12 rounded-2xl bg-brand-gradient flex items-center justify-center mb-4 shadow-brand group-hover:scale-110 transition-transform">
                <Compass className="w-6 h-6 text-white" />
              </div>
              <h3 className="font-display font-bold text-lg mb-2 group-hover:text-brand-400 transition-colors" style={{ color: 'var(--text-primary)' }}>
                {cat.name}
              </h3>
              <p className="text-xs line-clamp-2 leading-relaxed" style={{ color: 'var(--text-secondary)' }}>
                {cat.description || (loc === 'hi' ? 'इस श्रेणी में विस्तृत करियर, वेतन और परीक्षा मार्गदर्शन खोजें।' : 'Discover detailed paths, qualification routes, exams, and salary trends.')}
              </p>
              <div className="mt-4 flex items-center gap-1 text-xs font-bold text-brand-400 group-hover:translate-x-1 transition-transform">
                <span>{loc === 'hi' ? 'करियर देखें' : 'Explore Careers'}</span>
                <ArrowRight className="w-3.5 h-3.5" />
              </div>
            </Link>
          ))}
        </div>
      </section>
    </div>
  )
}
