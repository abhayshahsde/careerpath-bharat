'use client'

import { useState, useEffect, useCallback } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { 
  Compass, Plus, ArrowLeft, CheckCircle2, Clock, 
  Trash2, ChevronRight, Loader2, Sparkles, Target
} from 'lucide-react'
import { api, RoadmapSummaryDto } from '@/lib/api'
import { useAuth } from '@/lib/auth-context'
import { translate } from '@/lib/i18n'

export default function RoadmapsPage() {
  const router = useRouter()
  const { isAuthenticated, isLoading: authLoading } = useAuth()
  const [roadmaps, setRoadmaps] = useState<RoadmapSummaryDto[]>([])
  const [loading, setLoading] = useState(true)
  const [creating, setCreating] = useState(false)
  const [newTitle, setNewTitle] = useState('')
  const [newDesc, setNewDesc] = useState('')
  const [currentLocale, setCurrentLocale] = useState('en')
  const [deletingId, setDeletingId] = useState<string | null>(null)

  useEffect(() => {
    if (typeof window !== 'undefined') {
      setCurrentLocale(localStorage.getItem('locale') ?? 'en')
    }
  }, [])

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push('/auth/login?redirect=/me/roadmaps')
    }
  }, [authLoading, isAuthenticated, router])

  const fetchRoadmaps = useCallback(async () => {
    setLoading(true)
    try {
      const data = await api.getRoadmaps()
      setRoadmaps(data ?? [])
    } catch (err) {
      console.error('Failed to load roadmaps', err)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (isAuthenticated) {
      fetchRoadmaps()
    }
  }, [isAuthenticated, fetchRoadmaps])

  async function handleCreateRoadmap(e: React.FormEvent) {
    e.preventDefault()
    if (!newTitle.trim()) return

    setCreating(true)
    try {
      const res = await api.createRoadmap({
        title: newTitle.trim(),
        description: newDesc.trim() || undefined,
      })

      // Add a starter milestone
      await api.addMilestone(res.id, {
        title: currentLocale === 'hi' ? 'चरण 1: शुरुआत' : 'Phase 1: Getting Started',
        description: currentLocale === 'hi' ? 'बुनियादी लक्ष्य और अनुसंधान' : 'Initial foundations and orientation',
        sortOrder: 1,
      })

      setNewTitle('')
      setNewDesc('')
      router.push(`/me/roadmaps/${res.id}`)
    } catch (err) {
      console.error('Failed to create roadmap', err)
    } finally {
      setCreating(false)
    }
  }

  async function handleDelete(id: string) {
    if (!confirm(currentLocale === 'hi' ? 'क्या आप इस रोडमैप को हटाना चाहते हैं?' : 'Are you sure you want to delete this roadmap?')) {
      return
    }

    setDeletingId(id)
    try {
      await api.deleteRoadmap(id)
      setRoadmaps(prev => prev.filter(r => r.id !== id))
    } catch (err) {
      console.error('Failed to delete roadmap', err)
    } finally {
      setDeletingId(null)
    }
  }

  if (authLoading || (!isAuthenticated && loading)) {
    return (
      <div className="min-h-[70vh] flex items-center justify-center">
        <Loader2 className="w-10 h-10 text-brand-400 animate-spin" />
      </div>
    )
  }

  return (
    <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-12 page-enter">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <button
            onClick={() => router.push('/dashboard')}
            className="inline-flex items-center gap-2 text-sm font-medium mb-3 text-white/60 hover:text-white transition-colors"
          >
            <ArrowLeft className="w-4 h-4" /> {currentLocale === 'hi' ? 'डैशबोर्ड पर वापस' : 'Back to Dashboard'}
          </button>
          <h1 className="section-heading flex items-center gap-3">
            <Compass className="w-8 h-8 text-brand-500" />
            {translate('myRoadmaps', currentLocale)}
          </h1>
          <p className="section-sub">{translate('careerRoadmapsSub', currentLocale)}</p>
        </div>
      </div>

      <div className="grid lg:grid-cols-3 gap-8">
        {/* Roadmaps List */}
        <div className="lg:col-span-2 space-y-4">
          {loading ? (
            <div className="glass rounded-3xl p-12 text-center">
              <Loader2 className="w-8 h-8 text-brand-400 animate-spin mx-auto mb-3" />
              <p className="text-sm text-white/50">{translate('loadingText', currentLocale)}</p>
            </div>
          ) : roadmaps.length === 0 ? (
            <div className="glass rounded-3xl p-12 text-center border-dashed border-2" style={{ borderColor: 'var(--border-color)' }}>
              <div className="w-16 h-16 rounded-2xl bg-brand-gradient flex items-center justify-center shadow-brand mx-auto mb-4">
                <Target className="w-8 h-8 text-white" />
              </div>
              <h3 className="font-display font-bold text-xl mb-2" style={{ color: 'var(--text-primary)' }}>
                {currentLocale === 'hi' ? 'अभी तक कोई रोडमैप नहीं बनाया गया' : 'No Roadmaps Created Yet'}
              </h3>
              <p className="text-sm max-w-md mx-auto mb-6" style={{ color: 'var(--text-muted)' }}>
                {currentLocale === 'hi'
                  ? 'किसी भी करियर पेज से रोडमैप जेनरेट करें या दाईं ओर दिए गए फॉर्म से अपना कस्टमाइज़्ड रोडमैप बनाएं।'
                  : 'Generate a step-by-step roadmap from any career page, or create your customized milestones on the right.'}
              </p>
              <Link
                href={`/careers?locale=${currentLocale}`}
                className="btn-brand inline-flex items-center gap-2 text-sm"
              >
                <Compass className="w-4 h-4" />
                {translate('browseAll', currentLocale)}
              </Link>
            </div>
          ) : (
            roadmaps.map(roadmap => (
              <div
                key={roadmap.id}
                className="glass-card flex flex-col sm:flex-row sm:items-center justify-between gap-4 p-6 transition-all hover:scale-[1.01]"
              >
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-1.5 flex-wrap">
                    {roadmap.careerTitle && (
                      <span className="badge-brand">{roadmap.careerTitle}</span>
                    )}
                    <span className={`text-xs px-2.5 py-0.5 rounded-full font-semibold ${
                      roadmap.progressPercent === 100
                        ? 'bg-emerald-500/10 text-emerald-500 border border-emerald-500/20'
                        : 'bg-brand-500/10 text-brand-500 border border-brand-500/20'
                    }`}>
                      {roadmap.progressPercent === 100
                        ? (currentLocale === 'hi' ? 'पूर्ण' : 'Completed')
                        : (currentLocale === 'hi' ? `${roadmap.progressPercent}% प्रगति` : `${roadmap.progressPercent}% In Progress`)}
                    </span>
                  </div>

                  <h3 className="font-display font-bold text-lg mb-1" style={{ color: 'var(--text-primary)' }}>
                    {roadmap.title}
                  </h3>

                  {roadmap.description && (
                    <p className="text-xs line-clamp-2 mb-3" style={{ color: 'var(--text-secondary)' }}>
                      {roadmap.description}
                    </p>
                  )}

                  {/* Progress Bar */}
                  <div className="w-full bg-black/5 dark:bg-white/10 rounded-full h-2 overflow-hidden mb-2">
                    <div
                      className="bg-brand-gradient h-full rounded-full transition-all duration-500"
                      style={{ width: `${roadmap.progressPercent}%` }}
                    />
                  </div>

                  <div className="flex items-center gap-4 text-xs" style={{ color: 'var(--text-muted)' }}>
                    <span className="flex items-center gap-1">
                      <CheckCircle2 className="w-3.5 h-3.5 text-emerald-500" />
                      {roadmap.completedTasks} / {roadmap.totalTasks} {translate('tasks', currentLocale)}
                    </span>
                    <span className="flex items-center gap-1">
                      <Clock className="w-3.5 h-3.5" />
                      {new Date(roadmap.createdAt).toLocaleDateString(currentLocale === 'hi' ? 'hi-IN' : 'en-US')}
                    </span>
                  </div>
                </div>

                <div className="flex items-center gap-2 shrink-0 pt-2 sm:pt-0 border-t sm:border-t-0" style={{ borderColor: 'var(--border-color)' }}>
                  <button
                    onClick={() => handleDelete(roadmap.id)}
                    disabled={deletingId === roadmap.id}
                    className="p-2.5 rounded-xl border hover:bg-red-500/10 text-white/40 hover:text-red-500 transition-colors"
                    style={{ borderColor: 'var(--border-color)' }}
                    title={currentLocale === 'hi' ? 'रोडमैप हटाएं' : 'Delete Roadmap'}
                  >
                    {deletingId === roadmap.id ? (
                      <Loader2 className="w-4 h-4 animate-spin text-red-500" />
                    ) : (
                      <Trash2 className="w-4 h-4" />
                    )}
                  </button>
                  <Link
                    href={`/me/roadmaps/${roadmap.id}`}
                    className="btn-brand text-xs px-4 py-2.5 flex items-center gap-1.5 font-semibold"
                  >
                    {currentLocale === 'hi' ? 'देखें व ट्रैक करें' : 'View & Track'}
                    <ChevronRight className="w-3.5 h-3.5" />
                  </Link>
                </div>
              </div>
            ))
          )}
        </div>

        {/* Create Custom Roadmap Form */}
        <div className="glass rounded-3xl p-6 md:p-8 h-fit shadow-xl" style={{ borderColor: 'var(--border-color)' }}>
          <div className="flex items-center gap-3 mb-4">
            <div className="w-10 h-10 rounded-xl bg-brand-gradient flex items-center justify-center shadow-brand">
              <Plus className="w-5 h-5 text-white" />
            </div>
            <div>
              <h2 className="font-display font-bold text-lg" style={{ color: 'var(--text-primary)' }}>
                {translate('createRoadmap', currentLocale)}
              </h2>
              <p className="text-xs" style={{ color: 'var(--text-muted)' }}>
                {currentLocale === 'hi' ? 'कस्टम सीखने का लक्ष्य बनाएं' : 'Design your custom learning milestones'}
              </p>
            </div>
          </div>

          <form onSubmit={handleCreateRoadmap} className="space-y-4">
            <div>
              <label className="block text-xs font-semibold mb-1.5" style={{ color: 'var(--text-secondary)' }}>
                {currentLocale === 'hi' ? 'रोडमैप का शीर्षक *' : 'Roadmap Title *'}
              </label>
              <input
                type="text"
                required
                value={newTitle}
                onChange={(e) => setNewTitle(e.target.value)}
                placeholder={currentLocale === 'hi' ? 'उदा. फुल स्टैक डेवलपर 2026' : 'e.g. Full Stack Developer 2026'}
                className="input text-sm"
              />
            </div>

            <div>
              <label className="block text-xs font-semibold mb-1.5" style={{ color: 'var(--text-secondary)' }}>
                {currentLocale === 'hi' ? 'विवरण (वैकल्पिक)' : 'Description (Optional)'}
              </label>
              <textarea
                rows={3}
                value={newDesc}
                onChange={(e) => setNewDesc(e.target.value)}
                placeholder={currentLocale === 'hi' ? 'इस रोडमैप के लिए अपने मुख्य लक्ष्य लिखें...' : 'Outline your primary goals for this roadmap...'}
                className="input text-sm resize-none"
              />
            </div>

            <button
              type="submit"
              disabled={creating || !newTitle.trim()}
              className="btn-brand w-full py-3 text-sm font-semibold flex items-center justify-center gap-2 disabled:opacity-50"
            >
              {creating ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  {translate('loadingText', currentLocale)}
                </>
              ) : (
                <>
                  <Sparkles className="w-4 h-4" />
                  {translate('createRoadmap', currentLocale)}
                </>
              )}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
