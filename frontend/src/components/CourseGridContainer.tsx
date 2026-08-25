'use client'

import { useState, useEffect, useCallback } from 'react'
import { Search } from 'lucide-react'
import Link from 'next/link'
import { useAuth } from '@/lib/auth-context'
import { api } from '@/lib/api'
import { translate } from '@/lib/i18n'
import CourseCard from './CourseCard'

interface CourseGridContainerProps {
  initialData: {
    items: { id: number; name: string; shortName: string | null; degreeLevel: string; durationYears: number }[]
    totalCount: number
    hasNextPage: boolean
    hasPreviousPage: boolean
  }
  degreeLevels: string[]
  searchParams: { search?: string; degreeLevel?: string }
  loc: string
  page: number
}

export default function CourseGridContainer({
  initialData,
  degreeLevels,
  searchParams,
  loc,
  page
}: CourseGridContainerProps) {
  const { isAuthenticated } = useAuth()
  const [savedCourseIds, setSavedCourseIds] = useState<number[]>([])

  const loadSavedCourseIds = useCallback(async () => {
    try {
      const saved = await api.getSavedCourses(loc)
      setSavedCourseIds(saved.map(c => c.courseId))
    } catch (e) {
      console.error(e)
    }
  }, [loc])

  const handleToggleSaveCourse = useCallback(async (courseId: number) => {
    const isSaved = savedCourseIds.includes(courseId)
    try {
      if (isSaved) {
        await api.unsaveCourse(courseId)
        setSavedCourseIds(prev => prev.filter(id => id !== courseId))
      } else {
        await api.saveCourse(courseId)
        setSavedCourseIds(prev => [...prev, courseId])
      }
    } catch (e) {
      console.error(e)
    }
  }, [savedCourseIds])

  useEffect(() => {
    if (isAuthenticated) {
      loadSavedCourseIds()
    }
  }, [isAuthenticated, loadSavedCourseIds])

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 page-enter">
      {/* Header */}
      <div className="mb-10">
        <h1 className="section-heading mb-2">{translate('courses', loc)}</h1>
        <p className="section-sub">
          {translate('degreeLevel', loc)} ({initialData.totalCount})
        </p>
      </div>

      {/* Filters */}
      <div className="glass rounded-2xl p-5 mb-8 flex flex-col md:flex-row gap-4">
        <form className="flex-1 flex items-center gap-3 bg-white/5 rounded-xl px-4 py-2 border border-white/10">
          <Search className="w-4 h-4 text-white/40 shrink-0" />
          <input name="locale" type="hidden" value={loc} />
          <input
            name="search"
            type="text"
            defaultValue={searchParams.search}
            placeholder={translate('searchCourses', loc)}
            className="flex-1 bg-transparent text-white placeholder-white/40 outline-none text-sm"
          />
          <button type="submit" className="text-brand-400 text-xs font-medium">
            {translate('go', loc)}
          </button>
        </form>
        <div className="flex gap-2 flex-wrap">
          <Link
            href={`/courses?locale=${loc}`}
            className={`px-4 py-2 rounded-xl text-xs font-medium transition-all ${
              !searchParams.degreeLevel ? 'bg-brand-500 text-white' : 'glass text-white/60 hover:text-white'
            }`}
          >
            {translate('all', loc)}
          </Link>
          {degreeLevels.map(dl => (
            <Link
              key={dl}
              href={`/courses?degreeLevel=${dl}&locale=${loc}`}
              className={`px-4 py-2 rounded-xl text-xs font-medium transition-all ${
                searchParams.degreeLevel === dl ? 'bg-brand-500 text-white' : 'glass text-white/60 hover:text-white'
              }`}
            >
              {translate(dl.toLowerCase(), loc)}
            </Link>
          ))}
        </div>
      </div>

      {/* Grid */}
      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-5 mb-10">
        {initialData.items.map((course: { id: number; name: string; shortName: string | null; degreeLevel: string; durationYears: number }) => (
          <CourseCard
            key={course.id}
            course={course}
            loc={loc}
            isSaved={savedCourseIds.includes(course.id)}
            onToggleSave={handleToggleSaveCourse}
          />
        ))}
      </div>

      {(initialData.hasNextPage || initialData.hasPreviousPage) && (
        <div className="flex justify-center gap-3">
          {initialData.hasPreviousPage && (
            <Link
              href={`/courses?page=${page - 1}&locale=${loc}`}
              className="glass-button text-sm"
            >
              {loc === 'hi' ? '← पिछला' : '← Previous'}
            </Link>
          )}
          {initialData.hasNextPage && (
            <Link
              href={`/courses?page=${page + 1}&locale=${loc}`}
              className="btn-brand text-sm"
            >
              {loc === 'hi' ? 'अगला →' : 'Next →'}
            </Link>
          )}
        </div>
      )}
    </div>
  )
}
