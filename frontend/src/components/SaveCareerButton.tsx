'use client'

import { useState, useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { Heart, Loader2 } from 'lucide-react'
import { useAuth } from '@/lib/auth-context'
import { api } from '@/lib/api'

interface SaveCareerButtonProps {
  careerId: string
  initialSaved: boolean
  loc: string
}

export default function SaveCareerButton({ careerId, initialSaved, loc }: SaveCareerButtonProps) {
  const router = useRouter()
  const { isAuthenticated } = useAuth()
  const [saved, setSaved] = useState(initialSaved)
  const [loading, setLoading] = useState(false)
  const [pulse, setPulse] = useState(false)

  useEffect(() => {
    if (isAuthenticated) {
      api.getSavedCareers(loc)
        .then(list => {
          setSaved(list.some(c => c.careerId === careerId))
        })
        .catch(console.error)
    }
  }, [careerId, isAuthenticated, loc])

  async function handleToggleSave(e: React.MouseEvent) {
    e.preventDefault()
    e.stopPropagation()

    if (!isAuthenticated) {
      router.push('/auth/login')
      return
    }

    // Optimistic UI toggle
    const targetState = !saved
    setSaved(targetState)
    setPulse(true)
    setTimeout(() => setPulse(false), 500)

    setLoading(true)
    try {
      if (targetState) {
        await api.saveCareer(careerId)
      } else {
        await api.unsaveCareer(careerId)
      }
    } catch (err) {
      // Revert on error
      setSaved(!targetState)
      console.error(err)
    } finally {
      setLoading(false)
    }
  }

  return (
    <button
      onClick={handleToggleSave}
      disabled={loading}
      className={`p-2 rounded-xl border transition-all active:scale-95 duration-200 flex items-center justify-center ${
        saved
          ? 'bg-red-500/20 border-red-500/30 text-red-500 hover:bg-red-500/30'
          : 'glass border-white/10 text-white/40 hover:text-white hover:bg-white/5'
      } ${pulse ? 'scale-110' : ''}`}
      title={saved ? 'Remove Bookmark' : 'Bookmark Career'}
    >
      {loading ? (
        <Loader2 className="w-5 h-5 animate-spin text-brand-400" />
      ) : (
        <Heart className={`w-5 h-5 ${saved ? 'fill-current text-red-500' : ''} ${pulse ? 'animate-bounce' : ''} transition-all`} />
      )}
    </button>
  )
}
