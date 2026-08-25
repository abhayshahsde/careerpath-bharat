'use client'

import { useState, useEffect, useCallback } from 'react'
import { useParams, useRouter } from 'next/navigation'
import Link from 'next/link'
import { 
  Compass, ArrowLeft, CheckCircle2, Circle, Plus, 
  Loader2, Check
} from 'lucide-react'
import { api, RoadmapDetailDto } from '@/lib/api'
import { useAuth } from '@/lib/auth-context'
import { translate } from '@/lib/i18n'

export default function RoadmapDetailPage() {
  const params = useParams()
  const router = useRouter()
  const roadmapId = params.id as string
  const { isAuthenticated, isLoading: authLoading } = useAuth()

  const [roadmap, setRoadmap] = useState<RoadmapDetailDto | null>(null)
  const [loading, setLoading] = useState(true)
  const [currentLocale, setCurrentLocale] = useState('en')
  
  // Adding milestone modal state
  const [showAddMilestone, setShowAddMilestone] = useState(false)
  const [milestoneTitle, setMilestoneTitle] = useState('')
  const [milestoneDesc, setMilestoneDesc] = useState('')
  const [submittingMilestone, setSubmittingMilestone] = useState(false)

  // Adding task modal state
  const [activeMilestoneId, setActiveMilestoneId] = useState<number | null>(null)
  const [taskTitle, setTaskTitle] = useState('')
  const [taskType, setTaskType] = useState('General')
  const [submittingTask, setSubmittingTask] = useState(false)

  // Tracking task completion state
  const [togglingTaskId, setTogglingTaskId] = useState<number | null>(null)

  useEffect(() => {
    if (typeof window !== 'undefined') {
      setCurrentLocale(localStorage.getItem('locale') ?? 'en')
    }
  }, [])

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push(`/auth/login?redirect=/me/roadmaps/${roadmapId}`)
    }
  }, [authLoading, isAuthenticated, router, roadmapId])

  const fetchRoadmap = useCallback(async () => {
    if (!roadmapId) return
    setLoading(true)
    try {
      const data = await api.getRoadmapDetail(roadmapId)
      setRoadmap(data)
    } catch (err) {
      console.error('Failed to load roadmap detail', err)
    } finally {
      setLoading(false)
    }
  }, [roadmapId])

  useEffect(() => {
    if (isAuthenticated && roadmapId) {
      fetchRoadmap()
    }
  }, [isAuthenticated, roadmapId, fetchRoadmap])

  async function handleToggleTask(milestoneId: number, taskId: number, currentCompleted: boolean) {
    if (currentCompleted) return // already completed

    setTogglingTaskId(taskId)
    try {
      await api.completeTask(roadmapId, milestoneId, taskId)
      // Optimistic update
      setRoadmap(prev => {
        if (!prev) return prev
        return {
          ...prev,
          milestones: prev.milestones.map(m => {
            if (m.id !== milestoneId) return m
            const updatedTasks = m.tasks.map(t => t.id === taskId ? { ...t, isCompleted: true, completedAt: new Date().toISOString() } : t)
            const allCompleted = updatedTasks.every(t => t.isCompleted)
            return {
              ...m,
              isCompleted: allCompleted,
              tasks: updatedTasks
            }
          })
        }
      })
    } catch (err) {
      console.error('Failed to complete task', err)
      fetchRoadmap()
    } finally {
      setTogglingTaskId(null)
    }
  }

  async function handleAddMilestone(e: React.FormEvent) {
    e.preventDefault()
    if (!milestoneTitle.trim()) return

    setSubmittingMilestone(true)
    try {
      await api.addMilestone(roadmapId, {
        title: milestoneTitle.trim(),
        description: milestoneDesc.trim() || undefined,
        sortOrder: (roadmap?.milestones.length ?? 0) + 1,
      })
      setMilestoneTitle('')
      setMilestoneDesc('')
      setShowAddMilestone(false)
      fetchRoadmap()
    } catch (err) {
      console.error('Failed to add milestone', err)
    } finally {
      setSubmittingMilestone(false)
    }
  }

  async function handleAddTask(e: React.FormEvent) {
    e.preventDefault()
    if (!activeMilestoneId || !taskTitle.trim()) return

    setSubmittingTask(true)
    try {
      await api.addTask(roadmapId, activeMilestoneId, {
        title: taskTitle.trim(),
        taskType,
        sortOrder: 1,
      })
      setTaskTitle('')
      setActiveMilestoneId(null)
      fetchRoadmap()
    } catch (err) {
      console.error('Failed to add task', err)
    } finally {
      setSubmittingTask(false)
    }
  }

  if (authLoading || loading) {
    return (
      <div className="min-h-[70vh] flex items-center justify-center">
        <Loader2 className="w-10 h-10 text-brand-400 animate-spin" />
      </div>
    )
  }

  if (!roadmap) {
    return (
      <div className="max-w-2xl mx-auto px-4 py-20 text-center page-enter">
        <div className="glass rounded-3xl p-10 border shadow-lg" style={{ borderColor: 'var(--border-color)' }}>
          <div className="w-14 h-14 rounded-2xl bg-brand-gradient flex items-center justify-center shadow-brand mx-auto mb-4">
            <Compass className="w-7 h-7 text-white" />
          </div>
          <h2 className="font-display font-bold text-2xl mb-2" style={{ color: 'var(--text-primary)' }}>
            {currentLocale === 'hi' ? 'रोडमैप नहीं मिला' : 'Roadmap Not Found'}
          </h2>
          <p className="text-sm mb-6" style={{ color: 'var(--text-muted)' }}>
            {currentLocale === 'hi'
              ? 'यह रोडमैप उपलब्ध नहीं है या हटा दिया गया है।'
              : 'This learning roadmap could not be loaded or was removed.'}
          </p>
          <div className="flex justify-center gap-3">
            <Link href="/me/roadmaps" className="btn-brand text-xs px-5 py-2.5 font-semibold">
              {currentLocale === 'hi' ? 'मेरे सभी रोडमैप देखें' : 'View My Roadmaps'}
            </Link>
            <Link href="/careers" className="glass-button text-xs px-5 py-2.5 font-semibold">
              {currentLocale === 'hi' ? 'करियर ब्राउज़ करें' : 'Browse Careers'}
            </Link>
          </div>
        </div>
      </div>
    )
  }

  const totalTasks = roadmap.milestones.reduce((acc, m) => acc + m.tasks.length, 0)
  const completedTasks = roadmap.milestones.reduce((acc, m) => acc + m.tasks.filter(t => t.isCompleted).length, 0)
  const progressPercent = totalTasks > 0 ? Math.round((completedTasks / totalTasks) * 100) : 0

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-12 page-enter">
      {/* Back button */}
      <Link
        href="/me/roadmaps"
        className="inline-flex items-center gap-2 text-sm font-medium mb-6 text-white/60 hover:text-white transition-colors"
      >
        <ArrowLeft className="w-4 h-4" /> {translate('myRoadmaps', currentLocale)}
      </Link>

      {/* Roadmap Header Card */}
      <div className="glass rounded-3xl p-8 mb-8 border" style={{ borderColor: 'var(--border-color)' }}>
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
          <div className="flex items-start gap-4">
            <div className="w-14 h-14 rounded-2xl bg-brand-gradient flex items-center justify-center shadow-brand shrink-0">
              <Compass className="w-7 h-7 text-white" />
            </div>
            <div>
              <div className="flex items-center gap-2 mb-1">
                {roadmap.careerTitle && (
                  <span className="badge-brand">{roadmap.careerTitle}</span>
                )}
                <span className="badge-purple">{progressPercent}% {translate('completed', currentLocale)}</span>
              </div>
              <h1 className="font-display font-black text-2xl md:text-3xl" style={{ color: 'var(--text-primary)' }}>
                {roadmap.title}
              </h1>
            </div>
          </div>

          <div className="flex items-center gap-2.5 self-start md:self-auto">
            <button
              onClick={() => window.print()}
              className="glass-button text-xs px-3.5 py-2.5 flex items-center gap-1.5 border shadow-sm"
              style={{ borderColor: 'var(--border-color)', color: 'var(--text-primary)' }}
              title="Print or Save as PDF"
            >
              📄 {currentLocale === 'hi' ? 'पीडीएफ / प्रिंट' : 'Print / Export PDF'}
            </button>
            <button
              onClick={() => setShowAddMilestone(true)}
              className="btn-brand text-xs px-4 py-2.5 flex items-center gap-2"
            >
              <Plus className="w-4 h-4" />
              {currentLocale === 'hi' ? 'नया चरण जोड़ें' : 'Add Milestone'}
            </button>
          </div>
        </div>

        {roadmap.description && (
          <p className="text-sm mb-6 leading-relaxed" style={{ color: 'var(--text-secondary)' }}>
            {roadmap.description}
          </p>
        )}

        {/* Progress bar */}
        <div className="space-y-2">
          <div className="flex items-center justify-between text-xs font-semibold" style={{ color: 'var(--text-muted)' }}>
            <span>{currentLocale === 'hi' ? 'कुल सीखने की प्रगति' : 'Overall Learning Progress'}</span>
            <span>{completedTasks} / {totalTasks} {translate('tasks', currentLocale)}</span>
          </div>
          <div className="w-full bg-black/5 dark:bg-white/10 rounded-full h-3 overflow-hidden">
            <div
              className="bg-brand-gradient h-full rounded-full transition-all duration-700"
              style={{ width: `${progressPercent}%` }}
            />
          </div>
        </div>
      </div>

      {/* Milestones and Tasks List */}
      <div className="space-y-6">
        {roadmap.milestones.length === 0 ? (
          <div className="glass rounded-3xl p-12 text-center border-dashed border-2" style={{ borderColor: 'var(--border-color)' }}>
            <p className="text-sm text-white/50 mb-4">
              {currentLocale === 'hi' ? 'इस रोडमैप में अभी कोई चरण नहीं है।' : 'No milestones in this roadmap yet.'}
            </p>
            <button
              onClick={() => setShowAddMilestone(true)}
              className="btn-brand text-xs px-4 py-2"
            >
              <Plus className="w-4 h-4" /> {currentLocale === 'hi' ? 'पहला चरण जोड़ें' : 'Add First Milestone'}
            </button>
          </div>
        ) : (
          roadmap.milestones.map((milestone, mIndex) => {
            const milestoneCompleted = milestone.isCompleted || (milestone.tasks.length > 0 && milestone.tasks.every(t => t.isCompleted))
            return (
              <div
                key={milestone.id}
                className="glass rounded-2xl p-6 border transition-all"
                style={{ borderColor: milestoneCompleted ? 'rgba(16, 185, 129, 0.3)' : 'var(--border-color)' }}
              >
                {/* Milestone Header */}
                <div className="flex items-start justify-between gap-4 mb-4">
                  <div className="flex items-start gap-3">
                    <div className={`w-8 h-8 rounded-xl flex items-center justify-center font-bold text-xs shrink-0 ${
                      milestoneCompleted
                        ? 'bg-emerald-500 text-white shadow-lg shadow-emerald-500/20'
                        : 'bg-brand-500/20 text-brand-400'
                    }`}>
                      {milestoneCompleted ? <Check className="w-4 h-4" /> : mIndex + 1}
                    </div>
                    <div>
                      <h3 className="font-display font-bold text-lg" style={{ color: 'var(--text-primary)' }}>
                        {milestone.title.replace(/^Phase \d+:\s*/i, '').replace(/^चरण \d+:\s*/i, '')}
                      </h3>
                      {milestone.description && (
                        <p className="text-xs mt-0.5" style={{ color: 'var(--text-muted)' }}>
                          {milestone.description}
                        </p>
                      )}
                    </div>
                  </div>

                  <button
                    onClick={() => setActiveMilestoneId(milestone.id)}
                    className="p-1.5 rounded-lg border hover:bg-brand-500/10 text-brand-500 text-xs font-semibold flex items-center gap-1 shrink-0"
                    style={{ borderColor: 'var(--border-color)' }}
                  >
                    <Plus className="w-3.5 h-3.5" />
                    {currentLocale === 'hi' ? 'कार्य जोड़ें' : 'Add Task'}
                  </button>
                </div>

                {/* Milestone Tasks */}
                <div className="space-y-2.5 pl-11">
                  {milestone.tasks.length === 0 ? (
                    <p className="text-xs italic py-2" style={{ color: 'var(--text-muted)' }}>
                      {currentLocale === 'hi' ? 'कोई कार्य नहीं जोड़ा गया।' : 'No tasks added for this phase yet.'}
                    </p>
                  ) : (
                    milestone.tasks.map(task => (
                      <div
                        key={task.id}
                        onClick={() => handleToggleTask(milestone.id, task.id, task.isCompleted)}
                        className={`flex items-center justify-between gap-3 p-3.5 rounded-xl border transition-all cursor-pointer ${
                          task.isCompleted
                            ? 'bg-emerald-500/5 border-emerald-500/20 opacity-75'
                            : 'hover:bg-brand-500/5 hover:border-brand-500/30'
                        }`}
                        style={task.isCompleted ? {} : { borderColor: 'var(--border-color)' }}
                      >
                        <div className="flex items-center gap-3">
                          <button
                            type="button"
                            disabled={togglingTaskId === task.id || task.isCompleted}
                            className="shrink-0"
                          >
                            {togglingTaskId === task.id ? (
                              <Loader2 className="w-5 h-5 animate-spin text-brand-400" />
                            ) : task.isCompleted ? (
                              <CheckCircle2 className="w-5 h-5 text-emerald-500" />
                            ) : (
                              <Circle className="w-5 h-5 text-white/30 hover:text-brand-500 transition-colors" />
                            )}
                          </button>
                          <span
                            className={`text-sm font-medium ${
                              task.isCompleted ? 'line-through' : ''
                            }`}
                            style={{ color: task.isCompleted ? 'var(--text-muted)' : 'var(--text-primary)' }}
                          >
                            {task.title}
                          </span>
                        </div>

                        <div className="flex items-center gap-2">
                          {task.taskType && (
                            <span className="text-[10px] px-2 py-0.5 rounded-full font-semibold bg-black/5 dark:bg-white/10" style={{ color: 'var(--text-secondary)' }}>
                              {task.taskType}
                            </span>
                          )}
                          {task.isCompleted && (
                            <span className="text-[10px] text-emerald-500 font-semibold flex items-center gap-0.5">
                              <Check className="w-3 h-3" /> {translate('completed', currentLocale)}
                            </span>
                          )}
                        </div>
                      </div>
                    ))
                  )}
                </div>
              </div>
            )
          })
        )}
      </div>

      {/* Add Milestone Modal */}
      {showAddMilestone && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="glass rounded-3xl p-6 md:p-8 max-w-md w-full animate-slide-up shadow-2xl border" style={{ borderColor: 'var(--border-color)' }}>
            <h3 className="font-display font-bold text-xl mb-1" style={{ color: 'var(--text-primary)' }}>
              {currentLocale === 'hi' ? 'नया सीखने का चरण जोड़ें' : 'Add Learning Phase / Milestone'}
            </h3>
            <p className="text-xs mb-4" style={{ color: 'var(--text-muted)' }}>
              {currentLocale === 'hi' ? 'इस मील के पत्थर के लिए शीर्षक और विवरण दर्ज करें।' : 'Specify milestone title and description.'}
            </p>

            <form onSubmit={handleAddMilestone} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold mb-1" style={{ color: 'var(--text-secondary)' }}>
                  {currentLocale === 'hi' ? 'चरण शीर्षक *' : 'Milestone Title *'}
                </label>
                <input
                  type="text"
                  required
                  value={milestoneTitle}
                  onChange={(e) => setMilestoneTitle(e.target.value)}
                  placeholder={currentLocale === 'hi' ? 'उदा. चरण 2: मुख्य प्रोग्रामिंग अवधारणाएं' : 'e.g. Phase 2: Core Programming Concepts'}
                  className="input text-sm"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1" style={{ color: 'var(--text-secondary)' }}>
                  {currentLocale === 'hi' ? 'विवरण' : 'Description'}
                </label>
                <textarea
                  rows={2}
                  value={milestoneDesc}
                  onChange={(e) => setMilestoneDesc(e.target.value)}
                  placeholder={currentLocale === 'hi' ? 'इस चरण में क्या हासिल करना है...' : 'What needs to be achieved in this phase...'}
                  className="input text-sm resize-none"
                />
              </div>

              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setShowAddMilestone(false)}
                  className="glass-button text-xs py-2.5 flex-1"
                >
                  {currentLocale === 'hi' ? 'रद्द करें' : 'Cancel'}
                </button>
                <button
                  type="submit"
                  disabled={submittingMilestone || !milestoneTitle.trim()}
                  className="btn-brand text-xs py-2.5 flex-1 flex items-center justify-center gap-1.5"
                >
                  {submittingMilestone ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
                  {currentLocale === 'hi' ? 'चरण जोड़ें' : 'Add Milestone'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Add Task Modal */}
      {activeMilestoneId !== null && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="glass rounded-3xl p-6 md:p-8 max-w-md w-full animate-slide-up shadow-2xl border" style={{ borderColor: 'var(--border-color)' }}>
            <h3 className="font-display font-bold text-xl mb-1" style={{ color: 'var(--text-primary)' }}>
              {currentLocale === 'hi' ? 'नया कार्य जोड़ें' : 'Add Checklist Task'}
            </h3>
            <p className="text-xs mb-4" style={{ color: 'var(--text-muted)' }}>
              {currentLocale === 'hi' ? 'चरण में पूरा करने के लिए एक कार्य दर्ज करें।' : 'Add a specific action item or task.'}
            </p>

            <form onSubmit={handleAddTask} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold mb-1" style={{ color: 'var(--text-secondary)' }}>
                  {currentLocale === 'hi' ? 'कार्य का शीर्षक *' : 'Task Title *'}
                </label>
                <input
                  type="text"
                  required
                  value={taskTitle}
                  onChange={(e) => setTaskTitle(e.target.value)}
                  placeholder={currentLocale === 'hi' ? 'उदा. डेटा स्ट्रक्चर और एल्गोरिदम का अध्ययन करें' : 'e.g. Study Data Structures & Algorithms'}
                  className="input text-sm"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1" style={{ color: 'var(--text-secondary)' }}>
                  {currentLocale === 'hi' ? 'कार्य का प्रकार' : 'Task Type'}
                </label>
                <select
                  value={taskType}
                  onChange={(e) => setTaskType(e.target.value)}
                  className="input text-sm"
                >
                  <option value="General">General</option>
                  <option value="Skill">Skill</option>
                  <option value="Course">Course</option>
                  <option value="Exam">Exam</option>
                  <option value="Project">Project</option>
                </select>
              </div>

              <div className="flex gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => setActiveMilestoneId(null)}
                  className="glass-button text-xs py-2.5 flex-1"
                >
                  {currentLocale === 'hi' ? 'रद्द करें' : 'Cancel'}
                </button>
                <button
                  type="submit"
                  disabled={submittingTask || !taskTitle.trim()}
                  className="btn-brand text-xs py-2.5 flex-1 flex items-center justify-center gap-1.5"
                >
                  {submittingTask ? <Loader2 className="w-4 h-4 animate-spin" /> : <Plus className="w-4 h-4" />}
                  {currentLocale === 'hi' ? 'कार्य जोड़ें' : 'Add Task'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
