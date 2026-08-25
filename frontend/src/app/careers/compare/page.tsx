/* eslint-disable @typescript-eslint/no-explicit-any */
'use client'

import { useEffect, useState } from 'react'
import Link from 'next/link'
import { ArrowLeft, Compass, ArrowRight, Check } from 'lucide-react'
import { api } from '@/lib/api'

export default function CompareCareersPage() {
  const [locale, setLocale] = useState('en')
  const [allCareers, setAllCareers] = useState<any[]>([])
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [careersData, setCareersData] = useState<any[]>([])

  useEffect(() => {
    if (typeof window !== 'undefined') {
      const loc = localStorage.getItem('locale') ?? 'en'
      setLocale(loc)

      api.getCareers({ pageSize: 50, locale: loc })
        .then(res => {
          setAllCareers(res.items ?? [])
          // Pre-select first 2 careers by default
          if (res.items && res.items.length >= 2) {
            const firstTwo = [res.items[0].slug, res.items[1].slug]
            setSelectedIds(firstTwo)
          }
        })
    }
  }, [])

  useEffect(() => {
    if (selectedIds.length > 0) {
      Promise.all(selectedIds.map(slug => api.getCareerDetail(slug, locale).catch(() => null)))
        .then(details => setCareersData(details.filter(Boolean)))
    } else {
      setCareersData([])
    }
  }, [selectedIds, locale])

  const handleToggleSelect = (slug: string) => {
    if (selectedIds.includes(slug)) {
      if (selectedIds.length > 1) {
        setSelectedIds(prev => prev.filter(s => s !== slug))
      }
    } else {
      if (selectedIds.length < 3) {
        setSelectedIds(prev => [...prev, slug])
      } else {
        setSelectedIds(prev => [prev[1], prev[2], slug])
      }
    }
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 page-enter">
      {/* Back button */}
      <Link href={`/careers?locale=${locale}`} className="inline-flex items-center gap-2 text-xs font-semibold mb-6 hover:underline" style={{ color: 'var(--text-muted)' }}>
        <ArrowLeft className="w-4 h-4" /> {locale === 'hi' ? 'करियर सूची पर वापस जाएं' : 'Back to Careers'}
      </Link>

      <div className="flex flex-col md:flex-row md:items-end justify-between gap-4 mb-8">
        <div>
          <div className="badge-brand mb-2">
            <Compass className="w-3.5 h-3.5" />
            <span>{locale === 'hi' ? 'तुलना उपकरण' : 'Decision Matrix'}</span>
          </div>
          <h1 className="section-heading">
            {locale === 'hi' ? 'करियर तुलना मैट्रिक्स' : 'Side-by-Side Career Comparison'}
          </h1>
          <p className="section-sub">
            {locale === 'hi' 
              ? 'वेतन, आवश्यक शिक्षा, कौशल और प्रवेश परीक्षाओं के आधार पर 2 या 3 करियर की तुलना करें।' 
              : 'Compare up to 3 careers across salary potential, study years, required skills, and entrance criteria.'}
          </p>
        </div>
      </div>

      {/* Career Picker Strip */}
      <div className="glass rounded-2xl p-5 mb-8 border shadow-sm" style={{ borderColor: 'var(--border-color)' }}>
        <span className="text-xs font-bold block mb-3" style={{ color: 'var(--text-primary)' }}>
          {locale === 'hi' ? 'तुलना के लिए करियर चुनें (अधिकतम 3):' : 'Select Careers to Compare (Max 3):'}
        </span>
        <div className="flex gap-2 flex-wrap">
          {allCareers.map(c => {
            const isSelected = selectedIds.includes(c.slug)
            return (
              <button
                key={c.id}
                onClick={() => handleToggleSelect(c.slug)}
                className={`px-3 py-1.5 rounded-xl text-xs font-bold border transition-all flex items-center gap-1.5 ${
                  isSelected
                    ? 'btn-brand shadow-sm scale-105'
                    : 'glass hover:bg-black/5 dark:hover:bg-white/5'
                }`}
                style={isSelected ? {} : { borderColor: 'var(--border-color)', color: 'var(--text-secondary)' }}
              >
                {isSelected ? <Check className="w-3.5 h-3.5" /> : <Compass className="w-3.5 h-3.5 text-brand-400" />}
                <span>{c.title}</span>
              </button>
            )
          })}
        </div>
      </div>

      {/* Comparison Grid */}
      {careersData.length > 0 ? (
        <div className="glass rounded-3xl border overflow-hidden shadow-sm" style={{ borderColor: 'var(--border-color)' }}>
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs border-collapse">
              <thead>
                <tr className="border-b bg-white/5" style={{ borderColor: 'var(--border-color)' }}>
                  <th className="p-5 font-bold w-1/4" style={{ color: 'var(--text-muted)' }}>Comparison Attribute</th>
                  {careersData.map(c => (
                    <th key={c.id} className="p-5 font-bold text-sm w-1/4" style={{ color: 'var(--text-primary)' }}>
                      <div className="flex items-center gap-2">
                        <div className="w-8 h-8 rounded-xl bg-brand-gradient flex items-center justify-center text-white shadow-sm shrink-0">
                          <Compass className="w-4 h-4" />
                        </div>
                        <div>
                          <div className="font-display font-black text-sm">{c.title}</div>
                          <div className="text-[10px] font-semibold text-brand-400">{c.categoryName}</div>
                        </div>
                      </div>
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y" style={{ borderColor: 'var(--border-color)' }}>
                {/* Salary Range */}
                <tr className="hover:bg-white/5">
                  <td className="p-5 font-bold" style={{ color: 'var(--text-secondary)' }}>💰 Average Salary Range</td>
                  {careersData.map(c => (
                    <td key={c.id} className="p-5 font-black text-emerald-500 text-sm">
                      {c.salaryRangeLabel || 'Market Standard'}
                    </td>
                  ))}
                </tr>

                {/* Education Required */}
                <tr className="hover:bg-white/5">
                  <td className="p-5 font-bold" style={{ color: 'var(--text-secondary)' }}>🎓 Education Duration</td>
                  {careersData.map(c => (
                    <td key={c.id} className="p-5 font-semibold" style={{ color: 'var(--text-primary)' }}>
                      {c.minEducationYears && c.maxEducationYears
                        ? `${c.minEducationYears} – ${c.maxEducationYears} Years`
                        : 'Graduate / Professional Certification'}
                    </td>
                  ))}
                </tr>

                {/* Core Skills */}
                <tr className="hover:bg-white/5">
                  <td className="p-5 font-bold" style={{ color: 'var(--text-secondary)' }}>⚡ Core Skills Required</td>
                  {careersData.map(c => (
                    <td key={c.id} className="p-5">
                      <div className="flex flex-wrap gap-1.5">
                        {c.skills && c.skills.length > 0 ? (
                          c.skills.map((s: any) => (
                            <span key={s.id} className="skill-chip">
                              {s.name}
                            </span>
                          ))
                        ) : (
                          <span className="text-white/40">Domain problem solving</span>
                        )}
                      </div>
                    </td>
                  ))}
                </tr>

                {/* Top Entrance Exams */}
                <tr className="hover:bg-white/5">
                  <td className="p-5 font-bold" style={{ color: 'var(--text-secondary)' }}>📝 Key Entrance Exams</td>
                  {careersData.map(c => (
                    <td key={c.id} className="p-5">
                      <div className="flex flex-wrap gap-1.5">
                        {c.exams && c.exams.length > 0 ? (
                          c.exams.map((e: any) => (
                            <span key={e.id} className="badge-orange">
                              {e.name}
                            </span>
                          ))
                        ) : (
                          <span className="text-xs" style={{ color: 'var(--text-muted)' }}>Merit / Institute Direct Admission</span>
                        )}
                      </div>
                    </td>
                  ))}
                </tr>

                {/* Top Degree Courses */}
                <tr className="hover:bg-white/5">
                  <td className="p-5 font-bold" style={{ color: 'var(--text-secondary)' }}>📚 Recommended Courses</td>
                  {careersData.map(c => (
                    <td key={c.id} className="p-5">
                      <div className="space-y-1">
                        {c.courses && c.courses.length > 0 ? (
                          c.courses.map((co: any) => (
                            <div key={co.id} className="text-xs font-semibold" style={{ color: 'var(--text-primary)' }}>
                              • {co.name} ({co.degreeLevel})
                            </div>
                          ))
                        ) : (
                          <span className="text-xs" style={{ color: 'var(--text-muted)' }}>Bachelor&apos;s / Professional Degree</span>
                        )}
                      </div>
                    </td>
                  ))}
                </tr>

                {/* Action CTA */}
                <tr className="hover:bg-white/5">
                  <td className="p-5 font-bold" style={{ color: 'var(--text-secondary)' }}>🚀 Deep-Dive Action</td>
                  {careersData.map(c => (
                    <td key={c.id} className="p-5">
                      <Link
                        href={`/careers/${c.slug}?locale=${locale}`}
                        className="btn-brand text-xs font-bold py-2 px-4 inline-flex items-center gap-1.5 shadow-sm"
                      >
                        <span>View Full Career Guide</span>
                        <ArrowRight className="w-3.5 h-3.5" />
                      </Link>
                    </td>
                  ))}
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      ) : (
        <div className="glass rounded-3xl p-12 text-center text-white/40 border" style={{ borderColor: 'var(--border-color)' }}>
          Please select at least 2 careers from above to view side-by-side comparison.
        </div>
      )}
    </div>
  )
}
