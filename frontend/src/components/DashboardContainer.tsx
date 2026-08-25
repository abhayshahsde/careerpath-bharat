'use client'

import { useState, useEffect, useCallback } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { 
  Compass, GraduationCap, Bookmark, Heart, Loader2, Sparkles, 
  ChevronRight, XCircle, CheckCircle2
} from 'lucide-react'
import { api, SavedCareerResponse, SavedCourseResponse } from '@/lib/api'
import { useAuth } from '@/lib/auth-context'
import { translate } from '@/lib/i18n'
import { locationData } from '@/lib/location-data'

interface DashboardContainerProps {
  initialLocale: string
}

interface RecommendedCareer {
  careerId: string
  careerSlug: string
  careerTitle: string
  categoryName: string | null
  score: number
}

interface RecommendedCourse {
  id: number
  name: string
  degreeLevel: string
  durationYears: number
}

export default function DashboardContainer({ initialLocale }: DashboardContainerProps) {
  const router = useRouter()
  const { isAuthenticated, isLoading: authLoading, user: authUser, logout } = useAuth()
  
  const [profileLoading, setProfileLoading] = useState(true)
  const [recommendations, setRecommendations] = useState<RecommendedCareer[]>([])
  const [recommendedCourses, setRecommendedCourses] = useState<RecommendedCourse[]>([])
  const [savedCareers, setSavedCareers] = useState<SavedCareerResponse[]>([])
  const [savedCourses, setSavedCourses] = useState<SavedCourseResponse[]>([])
  const [pulseCourseId, setPulseCourseId] = useState<number | null>(null)
  const [pulseCareerId, setPulseCareerId] = useState<string | null>(null)
  const [activeSub, setActiveSub] = useState<{ planName?: string; status?: string; maxDailyTokens?: number } | null>(null)
  
  // Onboarding Form State
  const [onboardingActive, setOnboardingActive] = useState(false)
  const [displayName, setDisplayName] = useState('')
  const [educationLevel, setEducationLevel] = useState('')
  const [stateOfResidence, setStateOfResidence] = useState('')
  const [schoolBoard, setSchoolBoard] = useState('')
  const [streamOrSubjects, setStreamOrSubjects] = useState('')
  const [selectedState, setSelectedState] = useState('')
  const [selectedDistrict, setSelectedDistrict] = useState('')
  const [submittingOnboarding, setSubmittingOnboarding] = useState(false)
  
  // Translation Locale
  const [currentLocale, setCurrentLocale] = useState(initialLocale)

  useEffect(() => {
    if (typeof window !== 'undefined') {
      setCurrentLocale(localStorage.getItem('locale') ?? initialLocale)
    }
  }, [initialLocale])

  // Redirect guest users to login
  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push('/auth/login')
    }
  }, [isAuthenticated, authLoading, router])

  const fetchDashboardData = useCallback(async (eduLevel: string, locale: string) => {
    try {
      const [recs, savCareers, savCourses, sub] = await Promise.all([
        apiFetchWrapper<RecommendedCareer[]>('/api/v1/me/recommendations'),
        api.getSavedCareers(locale),
        api.getSavedCourses(locale),
        api.getActiveSubscription().catch(() => null)
      ])
      
      setRecommendations(recs ?? [])
      setSavedCareers(savCareers ?? [])
      setSavedCourses(savCourses ?? [])
      setActiveSub(sub as { planName?: string; status?: string; maxDailyTokens?: number } | null)

      // Fetch recommended courses based on education level mapping
      let degreeLevel = 'Undergraduate'
      if (eduLevel === 'Undergraduate') degreeLevel = 'Postgraduate'
      else if (eduLevel === 'Postgraduate' || eduLevel === 'Doctoral') degreeLevel = 'Doctoral'

      const coursesData = await api.getCourses({ degreeLevel, locale })
      setRecommendedCourses((coursesData.items ?? []).slice(0, 4))
    } catch (e) {
      console.error("Error loading dashboard data", e)
    }
  }, [])

  const loadProfileAndDashboard = useCallback(async () => {
    setProfileLoading(true)
    try {
      // 1. Get profile
      const prof = await api.getProfile()
      setDisplayName(prof.displayName ?? authUser?.displayName ?? '')
      
      const eduLevel = prof.currentEducationLevel ?? ''
      setEducationLevel(eduLevel)
      setSchoolBoard(prof.schoolBoard ?? '')
      setStreamOrSubjects(prof.streamOrSubjects ?? '')
      
      const locVal = prof.stateOfResidence ?? ''
      setStateOfResidence(locVal)
      if (locVal.includes(', ')) {
        const parts = locVal.split(', ')
        setSelectedDistrict(parts[0])
        setSelectedState(parts[1])
      } else {
        setSelectedState(locVal || 'All')
        setSelectedDistrict(locVal ? 'All' : 'All')
      }

      if (!eduLevel) {
        // Show setup banner card
        setOnboardingActive(true)
        // Load default dashboard data (e.g. Class 10)
        await fetchDashboardData('Class 10', currentLocale)
      } else {
        setOnboardingActive(false)
        await fetchDashboardData(eduLevel, currentLocale)
      }
    } catch (err: unknown) {
      const errMsg = (err as Error)?.message ?? ''
      if (errMsg.includes('Unauthorized') || errMsg.includes('401')) {
        logout()
        router.push('/auth/login')
      } else {
        // Profile not created yet: show defaults
        setDisplayName(authUser?.displayName ?? '')
        setEducationLevel('')
        setSelectedState('All')
        setSelectedDistrict('All')
        setOnboardingActive(true)
        // Load default dashboard data (e.g. Class 10)
        await fetchDashboardData('Class 10', currentLocale)
      }
    } finally {
      setProfileLoading(false)
    }
  }, [authUser, currentLocale, fetchDashboardData, logout, router])

  useEffect(() => {
    if (isAuthenticated) {
      loadProfileAndDashboard()
    } else {
      setProfileLoading(false)
    }
  }, [isAuthenticated, loadProfileAndDashboard])

  // Helper fetch since recommendation module might not be in api object helper
  async function apiFetchWrapper<T>(path: string): Promise<T> {
    const token = localStorage.getItem('access_token')
    const res = await fetch(`http://localhost:5073${path}`, {
      cache: 'no-store',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
      }
    })
    if (!res.ok) throw new Error("API error")
    return res.json()
  }

  async function handleOnboardingSubmit(e: React.FormEvent) {
    e.preventDefault()
    setSubmittingOnboarding(true)
    try {
      const combinedLoc = `${selectedDistrict}, ${selectedState}`
      setStateOfResidence(combinedLoc)
      await api.upsertProfile({
        displayName,
        currentEducationLevel: educationLevel,
        stateOfResidence: combinedLoc,
        preferredLocale: currentLocale
      })
      setOnboardingActive(false)
      await fetchDashboardData(educationLevel, currentLocale)
    } catch (err: unknown) {
      console.error(err)
      const errMsg = (err as Error)?.message ?? ''
      if (errMsg.includes('Unauthorized') || errMsg.includes('401')) {
        logout()
        router.push('/auth/login')
      }
    } finally {
      setSubmittingOnboarding(false)
    }
  }

  async function handleUnsaveCareer(careerId: string) {
    try {
      await api.unsaveCareer(careerId)
      setSavedCareers(prev => prev.filter(c => c.careerId !== careerId))
    } catch (err) {
      console.error("Failed to unsave career", err)
    }
  }

  async function handleUnsaveCourse(courseId: number) {
    try {
      await api.unsaveCourse(courseId)
      setSavedCourses(prev => prev.filter(c => c.courseId !== courseId))
    } catch (err) {
      console.error("Failed to unsave course", err)
    }
  }

  async function handleToggleSaveCourse(courseId: number) {
    const isSaved = savedCourses.some(c => c.courseId === courseId)
    try {
      setPulseCourseId(courseId)
      setTimeout(() => setPulseCourseId(null), 500)
      if (isSaved) {
        await api.unsaveCourse(courseId)
        setSavedCourses(prev => prev.filter(c => c.courseId !== courseId))
      } else {
        await api.saveCourse(courseId)
        const updatedSaved = await api.getSavedCourses(currentLocale)
        setSavedCourses(updatedSaved)
      }
    } catch (err) {
      console.error("Failed to toggle save course", err)
    }
  }

  async function handleToggleSaveCareer(careerId: string) {
    const isSaved = savedCareers.some(c => c.careerId === careerId)
    try {
      setPulseCareerId(careerId)
      setTimeout(() => setPulseCareerId(null), 500)
      if (isSaved) {
        await api.unsaveCareer(careerId)
        setSavedCareers(prev => prev.filter(c => c.careerId !== careerId))
      } else {
        await api.saveCareer(careerId)
        const updatedSaved = await api.getSavedCareers(currentLocale)
        setSavedCareers(updatedSaved)
      }
    } catch (err) {
      console.error("Failed to toggle save career", err)
    }
  }

  // Loading Screen
  if (authLoading || (isAuthenticated && profileLoading)) {
    return (
      <div className="min-h-[80vh] flex items-center justify-center">
        <Loader2 className="w-10 h-10 text-brand-400 animate-spin" />
      </div>
    )
  }

  // If guest, show nothing (redirect useEffect handles this)
  if (!isAuthenticated) return null

  // 2. Logged-in Dashboard
  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-12 page-enter space-y-12">
      {/* Welcome Banner */}
      <div className="welcome-banner rounded-3xl p-8 border border-brand-500/20 flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
        <div>
          <div className="flex items-center gap-2.5">
            <Sparkles className="w-5 h-5 text-brand-400 animate-pulse" />
            <span className="text-brand-300 font-semibold text-sm tracking-wide uppercase">
              {currentLocale === 'hi' ? 'डैशबोर्ड' : 'Student Dashboard'}
            </span>
          </div>
          <h1 className="font-display font-black text-2xl md:text-3xl text-white mt-1">
            {currentLocale === 'hi' ? `नमस्ते, ${displayName || 'छात्र'}` : `Welcome back, ${displayName || 'Student'}`}
          </h1>
          <p className="text-white/40 text-xs mt-1 flex flex-wrap gap-x-2 gap-y-1 items-center">
            <span>📍 {stateOfResidence || (currentLocale === 'hi' ? 'सभी स्थान' : 'All Locations')}</span>
            <span>·</span>
            <span>🎓 {educationLevel || (currentLocale === 'hi' ? 'सभी स्तर' : 'All Levels')}</span>
            {schoolBoard && (
              <>
                <span>·</span>
                <span>📋 {schoolBoard}</span>
              </>
            )}
            {streamOrSubjects && (
              <>
                <span>·</span>
                <span>🔬 {streamOrSubjects}</span>
              </>
            )}
            <span>·</span>
            <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full font-bold bg-amber-500/10 text-amber-400 border border-amber-500/20">
              💎 {activeSub?.planName || (currentLocale === 'hi' ? 'निःशुल्क योजना' : 'Free Plan')}
            </span>
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Link href="/subscribe" className="btn-brand text-xs font-semibold px-4 py-2 flex items-center gap-1.5 shadow-brand">
            ⚡ {currentLocale === 'hi' ? 'प्रीमियम अपग्रेड' : 'Upgrade Plan'}
          </Link>
          <Link href="/me/profile" className="glass-button text-xs font-semibold px-4 py-2 border border-white/10 hover:bg-white/5">
            ✏️ {currentLocale === 'hi' ? 'प्रोफ़ाइल बदलें' : 'Edit Profile'}
          </Link>
        </div>
      </div>

      {/* Preferences Setup Card (Shown inline if profile is incomplete) */}
      {onboardingActive && (
        <div className="glass rounded-3xl p-6 md:p-8 border border-brand-500/30 bg-gradient-to-br from-brand-900/20 to-accent-purple/10 space-y-6">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 rounded-xl bg-brand-gradient flex items-center justify-center animate-glow">
              <Sparkles className="w-5 h-5 text-white" />
            </div>
            <div>
              <h2 className="font-display font-bold text-lg text-white">
                {currentLocale === 'hi' ? 'अपनी प्राथमिकताओं को सेट करें' : 'Set Your Preferences'}
              </h2>
              <p className="text-white/40 text-xs mt-0.5">
                {currentLocale === 'hi'
                  ? 'सिफारिशों को अनलॉक करने के लिए अपनी वर्तमान पढ़ाई और प्राथमिकताओं को दर्ज करें।'
                  : 'Enter your education and location details to unlock customized career matches.'}
              </p>
            </div>
          </div>

          <form onSubmit={handleOnboardingSubmit} className="grid grid-cols-1 md:grid-cols-4 gap-4 items-end">
            {/* Display Name */}
            <div>
              <label className="block text-xs font-medium text-white/70 mb-1.5">
                {currentLocale === 'hi' ? 'आपका पूरा नाम' : 'Your Full Name'}
              </label>
              <input
                type="text"
                required
                value={displayName}
                onChange={e => setDisplayName(e.target.value)}
                placeholder="e.g. Luke Doe"
                className="w-full bg-surface-900 border border-white/10 rounded-xl px-4 py-2.5 text-xs text-white placeholder-white/30 outline-none focus:border-brand-500"
              />
            </div>

            {/* Current Education */}
            <div>
              <label className="block text-xs font-medium text-white/70 mb-1.5">
                {currentLocale === 'hi' ? 'आप अभी क्या पढ़ रहे हैं?' : 'What are you studying?'}
              </label>
              <select
                required
                value={educationLevel}
                onChange={e => setEducationLevel(e.target.value)}
                className="w-full bg-surface-900 border border-white/10 rounded-xl px-4 py-2.5 text-xs text-white outline-none focus:border-brand-500"
              >
                <option value="" disabled className="bg-surface-900 text-white/40">
                  {currentLocale === 'hi' ? 'स्तर चुनें...' : 'Select your level...'}
                </option>
                <option value="Class 10" className="bg-surface-900">Class 10</option>
                <option value="Class 12" className="bg-surface-900">Class 12</option>
                <option value="Undergraduate" className="bg-surface-900">Undergraduate</option>
                <option value="Postgraduate" className="bg-surface-900">Postgraduate</option>
              </select>
            </div>

            {/* State & District Selector */}
            <div className="grid grid-cols-2 gap-2 md:col-span-2">
              <div>
                <label className="block text-xs font-medium text-white/70 mb-1.5">
                  {currentLocale === 'hi' ? 'राज्य' : 'State'}
                </label>
                <select
                  required
                  value={selectedState}
                  onChange={e => {
                    setSelectedState(e.target.value)
                    setSelectedDistrict('')
                  }}
                  className="w-full bg-surface-900 border border-white/10 rounded-xl px-4 py-2.5 text-xs text-white outline-none focus:border-brand-500"
                >
                  <option value="" disabled className="text-white/40">
                    {currentLocale === 'hi' ? 'राज्य चुनें' : 'Select State'}
                  </option>
                  <option value="All" className="bg-surface-900 text-white">All (सभी)</option>
                  {Object.keys(locationData).map(st => (
                    <option key={st} value={st} className="bg-surface-900">{st}</option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-xs font-medium text-white/70 mb-1.5">
                  {currentLocale === 'hi' ? 'जिला' : 'District'}
                </label>
                <select
                  required
                  value={selectedDistrict}
                  onChange={e => setSelectedDistrict(e.target.value)}
                  disabled={!selectedState || selectedState === 'All'}
                  className="w-full bg-surface-900 border border-white/10 rounded-xl px-4 py-2.5 text-xs text-white outline-none focus:border-brand-500 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  <option value="" disabled className="text-white/40">
                    {currentLocale === 'hi' ? 'जिला चुनें' : 'Select District'}
                  </option>
                  {selectedState === 'All' ? (
                    <option value="All" className="bg-surface-900 text-white">All (सभी)</option>
                  ) : (
                    <>
                      <option value="All" className="bg-surface-900 text-white">All (सभी)</option>
                      {(locationData[selectedState] ?? []).map(dist => (
                        <option key={dist} value={dist} className="bg-surface-900">{dist}</option>
                      ))}
                    </>
                  )}
                </select>
              </div>
            </div>

            {/* Submit Button */}
            <div className="md:col-span-4 mt-2 flex justify-end">
              <button
                type="submit"
                disabled={submittingOnboarding}
                className="btn-brand py-2 px-6 text-xs font-bold flex items-center gap-1.5 shadow-brand"
              >
                {submittingOnboarding ? (
                  <Loader2 className="w-3.5 h-3.5 animate-spin" />
                ) : (
                  <>
                    <CheckCircle2 className="w-3.5 h-3.5" />
                    {currentLocale === 'hi' ? 'प्राथमिकताएं सहेजें' : 'Save Preferences'}
                  </>
                )}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Dynamic Grid: Recommendations + Saved Items */}
      <div className="grid lg:grid-cols-3 gap-8">
        {/* Col 1 & 2: Career Matches & Recommended Courses */}
        <div className="lg:col-span-2 space-y-8">
          {/* Career Recommendations */}
          <div className="glass-card">
            <div className="flex items-center justify-between mb-5">
              <h2 className="font-display font-bold text-xl text-white flex items-center gap-2.5">
                <Compass className="w-5 h-5 text-brand-400" />
                {currentLocale === 'hi' ? 'आपके लिए अनुशंसित करियर' : 'Your Recommended Careers'}
              </h2>
              <Link href="/careers" className="text-brand-400 text-xs font-semibold hover:underline">
                {currentLocale === 'hi' ? 'सभी देखें →' : 'View All →'}
              </Link>
            </div>

            {recommendations.length > 0 ? (
              <div className="space-y-3">
                {recommendations.slice(0, 4).map((rec) => (
                  <Link
                    key={rec.careerId}
                    href={`/careers/${rec.careerSlug}?locale=${currentLocale}`}
                    className="flex items-center justify-between p-4 glass rounded-xl border border-white/5 hover:border-brand-500/30 hover:bg-white/5 transition-all group"
                  >
                    <div>
                      <div className="text-white font-semibold group-hover:text-brand-300 transition-colors">
                        {rec.careerTitle}
                      </div>
                      <div className="text-white/40 text-xs mt-0.5">
                        📂 {rec.categoryName || (currentLocale === 'hi' ? 'सामान्य' : 'General')}
                      </div>
                    </div>
                    <div className="flex items-center gap-3">
                      <button
                        onClick={(e) => {
                          e.preventDefault()
                          e.stopPropagation()
                          handleToggleSaveCareer(rec.careerId)
                        }}
                        className={`p-1 rounded-lg border transition-all active:scale-90 duration-200 ${
                          savedCareers.some(c => c.careerId === rec.careerId)
                            ? 'bg-red-500/20 border-red-500/30 text-red-500 hover:bg-red-500/30'
                            : 'glass border-white/10 text-white/40 hover:text-white hover:bg-white/5'
                        } ${pulseCareerId === rec.careerId ? 'scale-125' : ''}`}
                        title={savedCareers.some(c => c.careerId === rec.careerId) ? 'Remove' : 'Save Career'}
                      >
                        <Heart className={`w-3 h-3 ${savedCareers.some(c => c.careerId === rec.careerId) ? 'fill-current text-red-500' : ''} ${pulseCareerId === rec.careerId ? 'animate-bounce' : ''} transition-all`} />
                      </button>
                      
                      <span className="px-3 py-1 rounded-full text-xs font-bold bg-emerald-500/10 text-emerald-300 border border-emerald-500/20">
                        {rec.score}% {currentLocale === 'hi' ? 'मैच' : 'Fit'}
                      </span>
                      <ChevronRight className="w-4 h-4 text-white/30 group-hover:text-white transition-colors" />
                    </div>
                  </Link>
                ))}
              </div>
            ) : (
              <div className="text-center py-8 text-white/30 text-sm">
                {currentLocale === 'hi' ? 'कोई मिलान करियर नहीं मिला।' : 'No career matches found.'}
              </div>
            )}
          </div>

          {/* Recommended Courses */}
          <div className="glass-card">
            <div className="flex items-center justify-between mb-5">
              <h2 className="font-display font-bold text-xl text-white flex items-center gap-2.5">
                <GraduationCap className="w-5 h-5 text-accent-purple" />
                {currentLocale === 'hi' ? 'अनुशंसित अध्ययन पाठ्यक्रम' : 'Recommended Study Courses'}
              </h2>
              <Link href="/courses" className="text-brand-400 text-xs font-semibold hover:underline">
                {currentLocale === 'hi' ? 'कोर्स खोजें →' : 'Find Courses →'}
              </Link>
            </div>

            {recommendedCourses.length > 0 ? (
              <div className="grid sm:grid-cols-2 gap-4">
                {recommendedCourses.map(course => (
                  <div key={course.id} className="glass p-4 rounded-xl border border-white/5 hover:border-accent-purple/30 transition-all flex flex-col justify-between relative group">
                    <div>
                      <div className="flex justify-between items-start mb-2">
                        <span className="badge-purple">
                          {course.degreeLevel}
                        </span>
                        
                        <button
                          onClick={(e) => {
                            e.preventDefault()
                            e.stopPropagation()
                            handleToggleSaveCourse(course.id)
                          }}
                          className={`p-1 rounded-lg border transition-all active:scale-90 duration-200 ${
                            savedCourses.some(c => c.courseId === course.id)
                              ? 'bg-red-500/20 border-red-500/30 text-red-500 hover:bg-red-500/30'
                              : 'glass border-white/10 text-white/40 hover:text-white hover:bg-white/5'
                          } ${pulseCourseId === course.id ? 'scale-125' : ''}`}
                          title={savedCourses.some(c => c.courseId === course.id) ? 'Remove' : 'Save Course'}
                        >
                          <Heart className={`w-3 h-3 ${savedCourses.some(c => c.courseId === course.id) ? 'fill-current text-red-500' : ''} ${pulseCourseId === course.id ? 'animate-bounce' : ''} transition-all`} />
                        </button>
                      </div>
                      <h3 className="text-white font-semibold text-sm line-clamp-2">{course.name}</h3>
                      <p className="text-white/40 text-xs mt-1">🕒 {course.durationYears} {translate('years', currentLocale)} {currentLocale === 'hi' ? 'अवधि' : 'duration'}</p>
                    </div>
                    <Link href={`/courses?locale=${currentLocale}`} className="text-brand-400 text-xs font-medium mt-3 inline-flex items-center gap-1 hover:text-brand-300">
                      {currentLocale === 'hi' ? 'अन्वेषण करें' : 'Explore Program'} <ChevronRight className="w-3.5 h-3.5" />
                    </Link>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-8 text-white/30 text-sm">
                {currentLocale === 'hi' ? 'कोई अनुशंसित पाठ्यक्रम उपलब्ध नहीं है।' : 'No recommended courses available.'}
              </div>
            )}
          </div>
        </div>

        {/* Col 3: Roadmaps + Saved/Bookmarked Careers & Courses */}
        <div className="space-y-8">
          {/* Active Roadmaps Summary Widget */}
          <div className="glass-card">
            <div className="flex items-center justify-between mb-4">
              <h2 className="font-display font-bold text-lg flex items-center gap-2" style={{ color: 'var(--text-primary)' }}>
                <Compass className="w-4 h-4 text-brand-500" />
                {currentLocale === 'hi' ? 'सीखने के रोडमैप' : 'Learning Roadmaps'}
              </h2>
              <Link href="/me/roadmaps" className="text-brand-500 text-xs font-semibold hover:underline">
                {currentLocale === 'hi' ? 'प्रबंधित करें →' : 'Manage →'}
              </Link>
            </div>
            <div className="p-4 rounded-xl border flex flex-col gap-2.5 bg-brand-500/5" style={{ borderColor: 'var(--border-color)' }}>
              <div className="flex items-center justify-between">
                <span className="text-xs font-semibold" style={{ color: 'var(--text-primary)' }}>
                  {currentLocale === 'hi' ? 'कैरियर रोडमैप और माइलस्टोन' : 'Career Roadmaps & Milestones'}
                </span>
                <span className="text-[10px] px-2 py-0.5 rounded-full font-bold bg-brand-500/10 text-brand-500">
                  {currentLocale === 'hi' ? 'सक्रिय' : 'Active'}
                </span>
              </div>
              <p className="text-xs" style={{ color: 'var(--text-muted)' }}>
                {currentLocale === 'hi'
                  ? 'चरण-दर-चरण मील के पत्थर ट्रैक करें और अपने लक्षित करियर की तैयारी करें।'
                  : 'Track milestone tasks, entrance exams & skills step-by-step.'}
              </p>
              <Link
                href="/me/roadmaps"
                className="btn-brand text-xs py-2 px-3 flex items-center justify-center gap-1.5 font-semibold mt-1"
              >
                <Sparkles className="w-3.5 h-3.5" />
                {currentLocale === 'hi' ? 'रोडमैप देखें व बनाएं' : 'View & Build Roadmaps'}
              </Link>
            </div>
          </div>

          {/* Bookmarked Careers */}
          <div className="glass-card">
            <h2 className="font-display font-bold text-lg mb-4 flex items-center gap-2" style={{ color: 'var(--text-primary)' }}>
              <Bookmark className="w-4 h-4 text-brand-400" />
              {currentLocale === 'hi' ? 'सहेजे गए करियर' : 'Bookmarked Careers'}
            </h2>

            {savedCareers.length > 0 ? (
              <div className="space-y-3">
                {savedCareers.map(car => (
                  <div key={car.id} className="glass p-3 rounded-xl border border-white/5 flex items-center justify-between gap-2">
                    <Link href={`/careers/${car.careerSlug}?locale=${currentLocale}`} className="flex-1 min-w-0">
                      <div className="text-white text-sm font-semibold truncate hover:text-brand-300 transition-colors">
                        {car.careerTitle}
                      </div>
                      <div className="text-white/30 text-xs truncate mt-0.5">/careers/{car.careerSlug}</div>
                    </Link>
                    <button 
                      onClick={() => handleUnsaveCareer(car.careerId)}
                      className="p-1.5 glass rounded-lg text-red-400 hover:text-white hover:bg-red-500/20 transition-all"
                      title={currentLocale === 'hi' ? 'हटाएं' : 'Remove'}
                    >
                      <XCircle className="w-4 h-4" />
                    </button>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-6 text-white/30 text-xs leading-relaxed border border-dashed border-white/10 rounded-xl">
                {currentLocale === 'hi' 
                  ? 'कोई सहेजा हुआ करियर नहीं है। करियर खोजें और बुकमार्क करें!' 
                  : 'No saved careers. Browse careers and click the bookmark icon to save!'}
              </div>
            )}
          </div>

          {/* Bookmarked Courses */}
          <div className="glass-card">
            <h2 className="font-display font-bold text-lg text-white mb-4 flex items-center gap-2">
              <Heart className="w-4 h-4 text-accent-purple" />
              {currentLocale === 'hi' ? 'सहेजे गए पाठ्यक्रम' : 'Saved Courses'}
            </h2>

            {savedCourses.length > 0 ? (
              <div className="space-y-3">
                {savedCourses.map(course => (
                  <div key={course.id} className="glass p-3 rounded-xl border border-white/5 flex items-center justify-between gap-2">
                    <div className="flex-1 min-w-0">
                      <div className="text-white text-sm font-semibold truncate">
                        {course.courseName}
                      </div>
                      <div className="text-white/30 text-xs truncate mt-0.5">{course.degreeLevel} · {course.durationYears} {translate('years', currentLocale)} {currentLocale === 'hi' ? 'अवधि' : 'duration'}</div>
                    </div>
                    <button 
                      onClick={() => handleUnsaveCourse(course.courseId)}
                      className="p-1.5 glass rounded-lg text-red-400 hover:text-white hover:bg-red-500/20 transition-all"
                      title={currentLocale === 'hi' ? 'हटाएं' : 'Remove'}
                    >
                      <XCircle className="w-4 h-4" />
                    </button>
                  </div>
                ))}
              </div>
            ) : (
              <div className="text-center py-6 text-white/30 text-xs leading-relaxed border border-dashed border-white/10 rounded-xl">
                {currentLocale === 'hi' 
                  ? 'कोई सहेजा हुआ पाठ्यक्रम नहीं है। पाठ्यक्रम सूची में जाकर बुकमार्क करें!' 
                  : 'No saved courses. Go to the courses directory to save courses directly!'}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  )
}
