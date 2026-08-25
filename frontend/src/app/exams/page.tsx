import Link from 'next/link'
import { BookOpen, Search, ExternalLink } from 'lucide-react'
import { api } from '@/lib/api'

export const metadata = { title: 'Entrance Exams', description: 'Browse entrance exams for Indian students — JEE, NEET, UPSC, CAT and more.' }

import { cookies } from 'next/headers'
import { translate } from '@/lib/i18n'

interface Props { searchParams: { level?: string; search?: string; page?: string; locale?: string } }

export default async function ExamsPage({ searchParams }: Props) {
  const page = Number(searchParams.page ?? 1)
  const cookieStore = cookies()
  const loc = cookieStore.get('locale')?.value ?? 'en'
  const data = await api.getExams({ level: searchParams.level, search: searchParams.search, page, locale: loc }).catch(() => ({ items: [], totalCount: 0, hasNextPage: false, hasPreviousPage: false, page: 1, pageSize: 20 }))

  const levels = ['National', 'State', 'University', 'International']

  const levelColors: Record<string, string> = {
    National:      'badge-brand',
    State:         'badge-teal',
    University:    'badge-purple',
    International: 'badge-orange',
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 page-enter">
      <div className="mb-10">
        <h1 className="section-heading mb-2 flex items-center gap-3">
          <BookOpen className="w-9 h-9 text-brand-400" /> {translate('entranceExams', loc)}
        </h1>
        <p className="section-sub">{data.totalCount} {translate('exams', loc).toLowerCase()}</p>
      </div>

      {/* Filters */}
      <div className="glass rounded-2xl p-5 mb-8 flex flex-col md:flex-row gap-4">
        <form className="flex-1 flex items-center gap-3 bg-white/5 rounded-xl px-4 py-2 border border-white/10">
          <Search className="w-4 h-4 text-white/40 shrink-0" />
          <input name="locale" type="hidden" value={loc} />
          <input name="search" type="text" defaultValue={searchParams.search}
            placeholder={loc === 'hi' ? 'परीक्षाएं खोजें...' : 'Search exams...'} className="flex-1 bg-transparent text-white placeholder-white/40 outline-none text-sm" />
          <button type="submit" className="text-brand-400 text-xs font-medium">{translate('go', loc)}</button>
        </form>
        <div className="flex gap-2 flex-wrap">
          <Link href={`/exams?locale=${loc}`} className={`px-4 py-2 rounded-xl text-xs font-medium transition-all ${!searchParams.level ? 'bg-brand-500 text-white' : 'glass text-white/60 hover:text-white'}`}>{translate('all', loc)}</Link>
          {levels.map(l => (
            <Link key={l} href={`/exams?level=${l}&locale=${loc}`} className={`px-4 py-2 rounded-xl text-xs font-medium transition-all ${searchParams.level === l ? 'bg-brand-500 text-white' : 'glass text-white/60 hover:text-white'}`}>{translate(l.toLowerCase(), loc)}</Link>
          ))}
        </div>
      </div>

      {/* Grid */}
      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-5 mb-10">
        {data.items.map((exam, i) => (
          <div key={exam.id} className="glass-card animate-slide-up" style={{ animationDelay: `${i * 0.04}s` }}>
            <div className="flex items-start justify-between mb-3">
              <div className="w-10 h-10 rounded-xl bg-accent-orange/20 border border-accent-orange/30 flex items-center justify-center">
                <BookOpen className="w-5 h-5 text-accent-orange" />
              </div>
              {exam.level && <span className={levelColors[exam.level] ?? 'badge'}>{translate(exam.level.toLowerCase(), loc)}</span>}
            </div>
            <h2 className="font-display font-bold text-lg text-white mb-1">{exam.name}</h2>
            {exam.fullName && <p className="text-white/40 text-xs mb-3">{exam.fullName}</p>}
            <div className="space-y-1.5 mb-4">
              {exam.conductingBody && (
                <div className="text-white/60 text-xs">🏛️ {exam.conductingBody}</div>
              )}
              {exam.frequency && (
                <div className="text-white/60 text-xs">📅 {loc === 'hi' ? (exam.frequency === 'Annual' ? 'वार्षिक' : exam.frequency === 'Bi-Annual' ? 'द्वि-वार्षिक' : 'मासिक') : exam.frequency}</div>
              )}
            </div>
            {exam.officialUrl && (
              <a href={exam.officialUrl} target="_blank" rel="noopener noreferrer"
                className="inline-flex items-center gap-1.5 text-brand-400 text-xs font-medium hover:text-brand-300 transition-colors">
                <ExternalLink className="w-3.5 h-3.5" /> {loc === 'hi' ? 'आधिकारिक वेबसाइट' : 'Official Website'}
              </a>
            )}
          </div>
        ))}
      </div>

      {(data.hasNextPage || data.hasPreviousPage) && (
        <div className="flex justify-center gap-3">
          {data.hasPreviousPage && <Link href={`/exams?page=${page - 1}&locale=${loc}`} className="glass-button text-sm">{loc === 'hi' ? '← पिछला' : '← Previous'}</Link>}
          {data.hasNextPage && <Link href={`/exams?page=${page + 1}&locale=${loc}`} className="btn-brand text-sm">{loc === 'hi' ? 'अगला →' : 'Next →'}</Link>}
        </div>
      )}
    </div>
  )
}
