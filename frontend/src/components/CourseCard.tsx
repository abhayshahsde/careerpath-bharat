'use client'

import { useState, useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { GraduationCap, Clock, Heart } from 'lucide-react'
import { useAuth } from '@/lib/auth-context'
import { translate } from '@/lib/i18n'

interface CourseCardProps {
  course: { id: number; name: string; shortName: string | null; degreeLevel: string; durationYears: number }
  loc: string
  isSaved: boolean
  onToggleSave: (courseId: number) => Promise<void>
}

export default function CourseCard({ course, loc, isSaved, onToggleSave }: CourseCardProps) {
  const router = useRouter()
  const { isAuthenticated } = useAuth()
  const [saved, setSaved] = useState(isSaved)
  const [loading, setLoading] = useState(false)
  const [pulse, setPulse] = useState(false)

  useEffect(() => {
    setSaved(isSaved)
  }, [isSaved])

  const degreeColors: Record<string, string> = {
    Undergraduate: 'badge-blue',
    Postgraduate:  'badge-purple',
    Diploma:       'badge-orange',
    Certificate:   'badge-teal',
    Doctoral:      'badge-red',
  }

  async function handleToggleSave(e: React.MouseEvent) {
    e.preventDefault()
    e.stopPropagation()
    
    if (!isAuthenticated) {
      router.push('/auth/login')
      return
    }

    // Optimistically update UI instantly!
    const targetState = !saved
    setSaved(targetState)
    setPulse(true)
    setTimeout(() => setPulse(false), 500)

    setLoading(true)
    try {
      await onToggleSave(course.id)
    } catch (err) {
      // Revert if API call fails
      setSaved(!targetState)
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="glass-card animate-slide-up relative group flex flex-col justify-between">
      <div>
        <div className="flex items-start justify-between mb-3">
          <div className="w-10 h-10 rounded-xl bg-accent-purple/20 border border-accent-purple/30 flex items-center justify-center">
            <GraduationCap className="w-5 h-5 text-accent-purple" />
          </div>
          
          <div className="flex items-center gap-2">
            <button
              onClick={handleToggleSave}
              disabled={loading}
              className={`p-1.5 rounded-lg border transition-all active:scale-90 duration-200 ${
                saved 
                  ? 'bg-red-500/20 border-red-500/30 text-red-500 hover:bg-red-500/30' 
                  : 'glass border-white/10 text-white/40 hover:text-white hover:bg-white/5'
              } ${pulse ? 'scale-125' : ''}`}
              title={saved ? 'Remove' : 'Save Course'}
            >
              <Heart className={`w-3.5 h-3.5 ${saved ? 'fill-current text-red-500' : ''} ${pulse ? 'animate-bounce' : ''} transition-all`} />
            </button>
            <span className={degreeColors[course.degreeLevel] ?? 'badge'}>
              {translate(course.degreeLevel.toLowerCase(), loc)}
            </span>
          </div>
        </div>

        {course.shortName && (
          <div className="text-brand-400 text-xs font-bold uppercase tracking-wide mb-1">
            {course.shortName}
          </div>
        )}
        <h2 className="font-display font-bold text-base text-white mb-3 line-clamp-2">
          {course.name}
        </h2>
      </div>

      <div className="flex items-center gap-2 text-white/50 text-xs mt-3">
        <Clock className="w-3.5 h-3.5" />
        {course.durationYears} {translate('years', loc)} {loc === 'hi' ? 'अवधि' : 'duration'}
      </div>
    </div>
  )
}
