import Link from 'next/link'
import { Compass, TrendingUp, ArrowRight, Search } from 'lucide-react'
import { api } from '@/lib/api'

export const metadata = { title: 'Careers', description: 'Browse all career paths available for Indian students.' }

import { cookies } from 'next/headers'
import { translate } from '@/lib/i18n'

interface Props { searchParams: { categoryId?: string; search?: string; page?: string; locale?: string } }

export default async function CareersPage({ searchParams }: Props) {
  const page = Number(searchParams.page ?? 1)
  const cookieStore = cookies()
  const loc = cookieStore.get('locale')?.value ?? 'en'
  const [careersData, categories] = await Promise.all([
    api.getCareers({ categoryId: searchParams.categoryId, search: searchParams.search, page, pageSize: 12, locale: loc }).catch(() => ({ items: [], totalCount: 0, hasNextPage: false, hasPreviousPage: false, page: 1, pageSize: 12 })),
    api.getCategories().catch(() => []),
  ])

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 page-enter">

      {/* Header */}
      <div className="mb-10">
        <h1 className="section-heading mb-2">{translate('careerPaths', loc)}</h1>
        <p className="section-sub">{translate('exploreCareers', loc)} ({careersData.totalCount})</p>
      </div>

      {/* Filters */}
      <div className="glass rounded-2xl p-5 mb-8 flex flex-col md:flex-row gap-4">
        <form className="flex-1 flex items-center gap-3 rounded-xl px-4 py-2 border" style={{ borderColor: 'var(--border-color)', backgroundColor: 'var(--input-bg)' }}>
          <Search className="w-4 h-4 shrink-0" style={{ color: 'var(--text-muted)' }} />
          <input name="locale" type="hidden" value={loc} />
          <input
            name="search"
            type="text"
            defaultValue={searchParams.search}
            placeholder={translate('searchPlaceholder', loc)}
            className="flex-1 bg-transparent outline-none text-sm"
            style={{ color: 'var(--input-text)' }}
          />
          <button type="submit" className="text-brand-500 text-xs font-semibold hover:text-brand-400">{translate('go', loc)}</button>
        </form>

        <div className="flex gap-2 flex-wrap">
          <Link href={`/careers?locale=${loc}`}
            className={`px-4 py-2 rounded-xl text-xs font-medium transition-all
              ${!searchParams.categoryId ? 'bg-brand-500 text-white' : 'glass hover:bg-brand-500/10'}`}
            style={!searchParams.categoryId ? {} : { color: 'var(--text-secondary)' }}>
            {translate('all', loc)}
          </Link>
          {categories.slice(0, 6).map(cat => (
            <Link key={cat.id}
              href={`/careers?categoryId=${cat.id}&locale=${loc}`}
              className={`px-4 py-2 rounded-xl text-xs font-medium transition-all
                ${searchParams.categoryId === cat.id ? 'bg-brand-500 text-white' : 'glass hover:bg-brand-500/10'}`}
              style={searchParams.categoryId === cat.id ? {} : { color: 'var(--text-secondary)' }}>
              {translate(cat.id, loc)}
            </Link>
          ))}
        </div>
      </div>

      {/* Grid */}
      {careersData.items.length === 0 ? (
        <div className="glass rounded-2xl p-16 text-center text-white/40">
          <Compass className="w-12 h-12 mx-auto mb-4 opacity-30" />
          <p className="text-lg">{translate('noCareers', loc)}</p>
        </div>
      ) : (
        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6 mb-10">
          {careersData.items.map((career, i) => (
            <Link key={career.id} href={`/careers/${career.slug}?locale=${loc}`}
              className="glass-card group animate-slide-up"
              style={{ animationDelay: `${i * 0.04}s` }}>
              <div className="flex items-start justify-between mb-4">
                <div className="w-11 h-11 rounded-xl bg-brand-gradient flex items-center justify-center
                                shadow-brand group-hover:shadow-glow transition-shadow duration-300">
                  <Compass className="w-5 h-5 text-white" />
                </div>
                {career.isFeatured && <span className="badge-teal">{loc === 'hi' ? 'विशेष रुप से प्रदर्शित' : 'Featured'}</span>}
              </div>
              <h2 className="font-display font-bold text-lg text-white mb-2 group-hover:text-brand-300 transition-colors line-clamp-2">
                {career.title}
              </h2>
              <p className="text-white/50 text-sm line-clamp-2 mb-4">{career.summary}</p>
              <div className="flex items-center justify-between">
                {career.salaryRangeLabel
                  ? <span className="text-accent-teal text-xs font-medium flex items-center gap-1.5">
                      <TrendingUp className="w-3.5 h-3.5" />{career.salaryRangeLabel}
                    </span>
                  : <span />}
                <span className="text-brand-400 text-xs flex items-center gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                  {loc === 'hi' ? 'खोजें' : 'Explore'} <ArrowRight className="w-3 h-3" />
                </span>
              </div>
            </Link>
          ))}
        </div>
      )}

      {/* Pagination */}
      {(careersData.hasNextPage || careersData.hasPreviousPage) && (
        <div className="flex justify-center gap-3">
          {careersData.hasPreviousPage && (
            <Link href={`/careers?page=${page - 1}&locale=${loc}${searchParams.categoryId ? `&categoryId=${searchParams.categoryId}` : ''}`}
              className="glass-button text-sm">{loc === 'hi' ? '← पिछला' : '← Previous'}</Link>
          )}
          {careersData.hasNextPage && (
            <Link href={`/careers?page=${page + 1}&locale=${loc}${searchParams.categoryId ? `&categoryId=${searchParams.categoryId}` : ''}`}
              className="btn-brand text-sm">{loc === 'hi' ? 'अगला →' : 'Next →'}</Link>
          )}
        </div>
      )}
    </div>
  )
}
