import Link from 'next/link'
import { Award, Search, ExternalLink, IndianRupee } from 'lucide-react'
import { api } from '@/lib/api'
import { translate } from '@/lib/i18n'

export const metadata = { title: 'Scholarships', description: 'Discover scholarships and financial aid available for Indian students.' }

import { cookies } from 'next/headers'

interface Props { searchParams: { level?: string; search?: string; page?: string; locale?: string } }

export default async function ScholarshipsPage({ searchParams }: Props) {
  const page = Number(searchParams.page ?? 1)
  const cookieStore = cookies()
  const loc = cookieStore.get('locale')?.value ?? 'en'
  const data = await api.getScholarships({ level: searchParams.level, search: searchParams.search, page, locale: loc })
    .catch(() => ({ items: [], totalCount: 0, hasNextPage: false, hasPreviousPage: false, page: 1, pageSize: 20 }))

  const levels = ['Undergraduate', 'Postgraduate', 'All']

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 page-enter">
      <div className="mb-10">
        <h1 className="section-heading mb-2 flex items-center gap-3">
          <Award className="w-9 h-9 text-accent-teal" /> {translate('scholarshipFunds', loc)}
        </h1>
        <p className="section-sub">{data.totalCount} {translate('scholarships', loc).toLowerCase()}</p>
      </div>

      {/* Filters */}
      <div className="glass rounded-2xl p-5 mb-8 flex flex-col md:flex-row gap-4">
        <form className="flex-1 flex items-center gap-3 bg-white/5 rounded-xl px-4 py-2 border border-white/10">
          <Search className="w-4 h-4 text-white/40 shrink-0" />
          <input name="locale" type="hidden" value={loc} />
          <input name="search" type="text" defaultValue={searchParams.search}
            placeholder={translate('searchScholarships', loc)} className="flex-1 bg-transparent text-white placeholder-white/40 outline-none text-sm" />
          <button type="submit" className="text-brand-400 text-xs font-medium">{translate('go', loc)}</button>
        </form>
        <div className="flex gap-2 flex-wrap">
          <Link href={`/scholarships?locale=${loc}`} className={`px-4 py-2 rounded-xl text-xs font-medium transition-all ${!searchParams.level ? 'bg-brand-500 text-white' : 'glass text-white/60 hover:text-white'}`}>{translate('all', loc)}</Link>
          {levels.map(l => (
            <Link key={l} href={`/scholarships?level=${l}&locale=${loc}`}
              className={`px-4 py-2 rounded-xl text-xs font-medium transition-all ${searchParams.level === l ? 'bg-brand-500 text-white' : 'glass text-white/60 hover:text-white'}`}>
              {translate(l.toLowerCase(), loc)}
            </Link>
          ))}
        </div>
      </div>

      {/* Grid */}
      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-5 mb-10">
        {data.items.map((sch, i) => (
          <div key={sch.id} className="glass-card flex flex-col animate-slide-up" style={{ animationDelay: `${i * 0.04}s` }}>
            <div className="flex items-start justify-between mb-3">
              <div className="w-10 h-10 rounded-xl bg-accent-teal/20 border border-accent-teal/30 flex items-center justify-center">
                <Award className="w-5 h-5 text-accent-teal" />
              </div>
              {sch.level && <span className="badge-teal">{translate(sch.level.toLowerCase(), loc)}</span>}
            </div>
            <h2 className="font-display font-bold text-lg text-white mb-1">{sch.name}</h2>
            <p className="text-white/40 text-xs mb-3">{loc === 'hi' ? 'द्वारा:' : 'by'} {sch.providerName}</p>
            {sch.amountLabel && (
              <div className="flex items-center gap-1.5 text-accent-teal text-sm font-semibold mb-3">
                <IndianRupee className="w-4 h-4" />{sch.amountLabel}
              </div>
            )}
            {sch.eligibilitySummary && (
              <p className="text-white/50 text-xs mb-4 line-clamp-2 flex-1">{sch.eligibilitySummary}</p>
            )}
            {sch.disclaimer && (
              <p className="text-amber-200/40 text-xs mb-3 italic">⚠️ {sch.disclaimer}</p>
            )}
            {sch.officialUrl && (
              <a href={sch.officialUrl} target="_blank" rel="noopener noreferrer"
                className="mt-auto inline-flex items-center gap-1.5 text-brand-400 text-xs font-medium hover:text-brand-300 transition-colors">
                <ExternalLink className="w-3.5 h-3.5" /> {translate('officialWebsite', loc)}
              </a>
            )}
          </div>
        ))}
      </div>

      {(data.hasNextPage || data.hasPreviousPage) && (
        <div className="flex justify-center gap-3">
          {data.hasPreviousPage && <Link href={`/scholarships?page=${page - 1}&locale=${loc}`} className="glass-button text-sm">{loc === 'hi' ? '← पिछला' : '← Previous'}</Link>}
          {data.hasNextPage && <Link href={`/scholarships?page=${page + 1}&locale=${loc}`} className="btn-brand text-sm">{loc === 'hi' ? 'अगला →' : 'Next →'}</Link>}
        </div>
      )}
    </div>
  )
}
