'use client'

import { useState, useEffect, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import { User, Mail, GraduationCap, MapPin, Globe, Save, Loader2, ArrowLeft, CheckCircle2, XCircle, X } from 'lucide-react'
import { api } from '@/lib/api'
import { useAuth } from '@/lib/auth-context'
import { translate } from '@/lib/i18n'
import { locationData } from '@/lib/location-data'

// ── Toast notification component ──────────────────────────────────────────────
type ToastType = 'success' | 'error'
interface Toast { id: number; type: ToastType; message: string }

function ToastContainer({ toasts, onDismiss }: { toasts: Toast[]; onDismiss: (id: number) => void }) {
  return (
    <div className="fixed top-6 right-6 z-[9999] flex flex-col gap-3 pointer-events-none">
      {toasts.map(t => (
        <div
          key={t.id}
          className={`pointer-events-auto flex items-start gap-3 px-5 py-4 rounded-2xl shadow-2xl border max-w-sm w-full animate-toast-in backdrop-blur-md
            ${t.type === 'success'
              ? 'bg-emerald-950/90 border-emerald-500/30 text-emerald-100'
              : 'bg-red-950/90 border-red-500/30 text-red-100'
            }`}
        >
          {t.type === 'success'
            ? <CheckCircle2 className="w-5 h-5 text-emerald-400 shrink-0 mt-0.5" />
            : <XCircle className="w-5 h-5 text-red-400 shrink-0 mt-0.5" />
          }
          <p className="flex-1 text-sm font-medium leading-snug">{t.message}</p>
          <button
            onClick={() => onDismiss(t.id)}
            className="text-white/40 hover:text-white/80 transition-colors shrink-0"
          >
            <X className="w-4 h-4" />
          </button>
        </div>
      ))}
    </div>
  )
}

export default function ProfilePage() {
  const router = useRouter()
  const { isAuthenticated, isLoading: authLoading, user: authUser } = useAuth()
  
  const [displayName, setDisplayName] = useState('')
  const [educationLevel, setEducationLevel] = useState('')
  const [selectedState, setSelectedState] = useState('')
  const [selectedDistrict, setSelectedDistrict] = useState('')
  const [preferredLocale, setPreferredLocale] = useState('en')
  const [schoolBoard, setSchoolBoard] = useState('')
  const [streamOrSubjects, setStreamOrSubjects] = useState('')
  const [selectedInterests, setSelectedInterests] = useState<string[]>([])
  
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [toasts, setToasts] = useState<Toast[]>([])
  const [currentLocale, setCurrentLocale] = useState('en')

  // Toast helpers
  const showToast = (type: ToastType, message: string) => {
    const id = Date.now()
    setToasts(prev => [...prev, { id, type, message }])
    setTimeout(() => setToasts(prev => prev.filter(t => t.id !== id)), 4500)
  }
  const dismissToast = (id: number) => setToasts(prev => prev.filter(t => t.id !== id))

  useEffect(() => {
    if (typeof window !== 'undefined') {
      setCurrentLocale(localStorage.getItem('locale') ?? 'en')
    }
  }, [])

  useEffect(() => {
    if (!authLoading && !isAuthenticated) {
      router.push('/auth/login')
    }
  }, [isAuthenticated, authLoading, router])

  const loadProfile = useCallback(async () => {
    setLoading(true)
    try {
      const profile = await api.getProfile()
      setDisplayName(profile.displayName ?? authUser?.displayName ?? '')
      setEducationLevel(profile.currentEducationLevel ?? '')
      setSchoolBoard(profile.schoolBoard ?? '')
      setStreamOrSubjects(profile.streamOrSubjects ?? '')
      setSelectedInterests(profile.interests ?? [])
      
      const locVal = profile.stateOfResidence ?? ''
      if (locVal.includes(', ')) {
        const parts = locVal.split(', ')
        setSelectedDistrict(parts[0])
        setSelectedState(parts[1])
      } else {
        setSelectedState(locVal)
        setSelectedDistrict('')
      }
      setPreferredLocale(profile.preferredLocale ?? 'en')
    } catch (err) {
      const errMsg = (err as { message?: string })?.message ?? ''
      if (errMsg === 'Not Found' || errMsg.includes('404')) {
        // Handle first-time profiles gracefully
        setDisplayName(authUser?.displayName ?? '')
        setEducationLevel('')
        setSchoolBoard('')
        setStreamOrSubjects('')
        setSelectedInterests([])
        setSelectedState('')
        setSelectedDistrict('')
        setPreferredLocale(currentLocale)
      } else {
        showToast('error', errMsg || 'Failed to load profile details.')
      }
    } finally {
      setLoading(false)
    }
  }, [authUser, currentLocale])

  useEffect(() => {
    if (isAuthenticated) {
      loadProfile()
    }
  }, [isAuthenticated, loadProfile])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setSaving(true)
    try {
      const combinedLoc = `${selectedDistrict}, ${selectedState}`
      await api.upsertProfile({
        displayName,
        currentEducationLevel: educationLevel,
        stateOfResidence: combinedLoc,
        preferredLocale,
        schoolBoard,
        streamOrSubjects,
        interests: selectedInterests
      })
      
      if (preferredLocale !== currentLocale) {
        localStorage.setItem('locale', preferredLocale)
        document.cookie = `locale=${preferredLocale}; path=/; max-age=${365*24*60*60};`
        setCurrentLocale(preferredLocale)
        showToast('success', currentLocale === 'hi' ? 'भाषा बदली जा रही है...' : 'Language changed! Reloading...')
        setTimeout(() => window.location.reload(), 1500)
      } else {
        showToast('success',
          currentLocale === 'hi'
            ? '✅ प्रोफ़ाइल सफलतापूर्वक सहेजी गई!'
            : '✅ Profile saved successfully!'
        )
      }
    } catch (err) {
      showToast('error',
        (err as { message?: string })?.message
        ?? (currentLocale === 'hi'
          ? '❌ प्रोफ़ाइल सहेजने में त्रुटि। कृपया पुनः प्रयास करें।'
          : '❌ Failed to save profile. Please try again.')
      )
    } finally {
      setSaving(false)
    }
  }


  if (authLoading || (!isAuthenticated && !authUser)) {
    return (
      <div className="min-h-[70vh] flex items-center justify-center">
        <Loader2 className="w-10 h-10 text-brand-400 animate-spin" />
      </div>
    )
  }

  return (
    <>
      <ToastContainer toasts={toasts} onDismiss={dismissToast} />
      <div className="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8 py-12 page-enter">
        {/* Back button */}
        <button
          onClick={() => router.back()}
          className="inline-flex items-center gap-2 text-white/50 hover:text-white mb-8 transition-colors text-sm font-medium"
        >
          <ArrowLeft className="w-4 h-4" /> {currentLocale === 'hi' ? 'पीछे जाएं' : 'Go Back'}
        </button>

        <div className="glass rounded-3xl p-8 md:p-10 border border-brand-500/20 bg-gradient-to-br from-brand-600/5 to-accent-purple/5">
          <div className="flex items-center gap-4 mb-8">
            <div className="w-14 h-14 rounded-2xl bg-brand-gradient flex items-center justify-center shadow-brand animate-glow">
              <User className="w-7 h-7 text-white" />
            </div>
            <div>
              <h1 className="font-display font-black text-2xl md:text-3xl text-white">
                {translate('myProfile', currentLocale)}
              </h1>
              <p className="text-white/40 text-sm mt-0.5">{authUser?.email}</p>
            </div>
          </div>

        {loading ? (
          <div className="py-12 flex justify-center">
            <Loader2 className="w-8 h-8 text-brand-400 animate-spin" />
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-6">
            {/* Full Name */}
            <div>
              <label className="block text-sm font-medium text-white/70 mb-2">
                {translate('fullNameLabel', currentLocale)}
              </label>
              <div className="relative">
                <User className="absolute left-3.5 top-3.5 w-4 h-4 text-white/30" />
                <input
                  type="text"
                  required
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  placeholder={currentLocale === 'hi' ? 'अपना नाम दर्ज करें...' : 'Enter your full name...'}
                  className="w-full bg-white/5 border border-white/10 rounded-xl pl-11 pr-4 py-3 text-sm text-white placeholder-white/30 focus:border-brand-500 focus:bg-white/10 outline-none transition-all duration-200"
                />
              </div>
            </div>

            {/* Email (Readonly) */}
            <div>
              <label className="block text-sm font-medium text-white/40 mb-2">
                {translate('emailLabel', currentLocale)} ({currentLocale === 'hi' ? 'केवल पढ़ने के लिए' : 'Read-only'})
              </label>
              <div className="relative">
                <Mail className="absolute left-3.5 top-3.5 w-4 h-4 text-white/20" />
                <input
                  type="email"
                  disabled
                  value={authUser?.email ?? ''}
                  className="w-full bg-white/5 border border-white/5 rounded-xl pl-11 pr-4 py-3 text-sm text-white/40 cursor-not-allowed outline-none"
                />
              </div>
            </div>

            <div className="grid md:grid-cols-2 gap-6">
              {/* Education Level */}
              <div>
                <label className="block text-sm font-medium text-white/70 mb-2">
                  {translate('educationLevelLabel', currentLocale)}
                </label>
                <div className="relative">
                  <GraduationCap className="absolute left-3.5 top-3.5 w-4 h-4 text-white/30 pointer-events-none" />
                  <select
                    value={educationLevel}
                    onChange={(e) => setEducationLevel(e.target.value)}
                    className="w-full bg-surface-900 border border-white/10 rounded-xl pl-11 pr-4 py-3 text-sm text-white outline-none focus:border-brand-500 transition-all appearance-none cursor-pointer"
                  >
                    <option value="" disabled className="bg-surface-900 text-white/50">
                      {currentLocale === 'hi' ? 'अपना स्तर चुनें' : 'Select your level'}
                    </option>
                    <option value="Class 10" className="bg-surface-900 text-white">Class 10</option>
                    <option value="Class 12" className="bg-surface-900 text-white">Class 12</option>
                    <option value="Undergraduate" className="bg-surface-900 text-white">Undergraduate</option>
                    <option value="Postgraduate" className="bg-surface-900 text-white">Postgraduate</option>
                  </select>
                </div>
              </div>

              {/* State & District Selectors */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-white/70 mb-2">
                    {currentLocale === 'hi' ? 'राज्य' : 'State'}
                  </label>
                  <div className="relative">
                    <MapPin className="absolute left-3.5 top-3.5 w-4 h-4 text-white/30 pointer-events-none z-10" />
                    <select
                      required
                      value={selectedState}
                      onChange={e => {
                        setSelectedState(e.target.value)
                        setSelectedDistrict('')
                      }}
                      className="w-full bg-surface-900 border border-white/10 rounded-xl pl-11 pr-4 py-3 text-sm text-white outline-none focus:border-brand-500 transition-all appearance-none cursor-pointer"
                    >
                      <option value="" disabled className="text-white/40">
                        {currentLocale === 'hi' ? 'राज्य चुनें' : 'Select State'}
                      </option>
                      {Object.keys(locationData).map(st => (
                        <option key={st} value={st} className="bg-surface-900 text-white">{st}</option>
                      ))}
                    </select>
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-white/70 mb-2">
                    {currentLocale === 'hi' ? 'जिला' : 'District'}
                  </label>
                  <div className="relative">
                    <MapPin className="absolute left-3.5 top-3.5 w-4 h-4 text-white/30 pointer-events-none z-10" />
                    <select
                      required
                      value={selectedDistrict}
                      onChange={e => setSelectedDistrict(e.target.value)}
                      disabled={!selectedState}
                      className="w-full bg-surface-900 border border-white/10 rounded-xl pl-11 pr-4 py-3 text-sm text-white outline-none focus:border-brand-500 transition-all appearance-none cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                      <option value="" disabled className="text-white/40">
                        {currentLocale === 'hi' ? 'जिला चुनें' : 'Select District'}
                      </option>
                      {(locationData[selectedState] ?? []).map(dist => (
                        <option key={dist} value={dist} className="bg-surface-900 text-white">{dist}</option>
                      ))}
                    </select>
                  </div>
                </div>
              </div>
            </div>

            {/* School Board & Stream/Subjects */}
            <div className="grid md:grid-cols-2 gap-6">
              <div>
                <label className="block text-sm font-medium text-white/70 mb-2">
                  {currentLocale === 'hi' ? 'स्कूल बोर्ड' : 'School Board'}
                </label>
                <div className="relative">
                  <GraduationCap className="absolute left-3.5 top-3.5 w-4 h-4 text-white/30 pointer-events-none" />
                  <select
                    value={schoolBoard}
                    onChange={(e) => setSchoolBoard(e.target.value)}
                    className="w-full bg-surface-900 border border-white/10 rounded-xl pl-11 pr-4 py-3 text-sm text-white outline-none focus:border-brand-500 transition-all appearance-none cursor-pointer"
                  >
                    <option value="">
                      {currentLocale === 'hi' ? 'बोर्ड चुनें (वैकल्पिक)' : 'Select Board (Optional)'}
                    </option>
                    <option value="CBSE" className="bg-surface-900 text-white">CBSE</option>
                    <option value="ICSE" className="bg-surface-900 text-white">ICSE/ISC</option>
                    <option value="UP Board" className="bg-surface-900 text-white">UP Board (Uttar Pradesh)</option>
                    <option value="Bihar Board" className="bg-surface-900 text-white">BSEB (Bihar Board)</option>
                    <option value="State Board" className="bg-surface-900 text-white">Other State Board</option>
                    <option value="IB/IGCSE" className="bg-surface-900 text-white">IB / Cambridge</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-white/70 mb-2">
                  {currentLocale === 'hi' ? 'विषय स्ट्रीम' : 'Subject Stream'}
                </label>
                <div className="relative">
                  <GraduationCap className="absolute left-3.5 top-3.5 w-4 h-4 text-white/30 pointer-events-none" />
                  <select
                    value={streamOrSubjects}
                    onChange={(e) => setStreamOrSubjects(e.target.value)}
                    className="w-full bg-surface-900 border border-white/10 rounded-xl pl-11 pr-4 py-3 text-sm text-white outline-none focus:border-brand-500 transition-all appearance-none cursor-pointer"
                  >
                    <option value="">
                      {currentLocale === 'hi' ? 'स्ट्रीम चुनें (वैकल्पिक)' : 'Select Stream (Optional)'}
                    </option>
                    <option value="General" className="bg-surface-900 text-white">General / Standard (Below Class 11)</option>
                    <option value="PCM" className="bg-surface-900 text-white">PCM (Physics, Chemistry, Maths)</option>
                    <option value="PCB" className="bg-surface-900 text-white">PCB (Physics, Chemistry, Biology)</option>
                    <option value="PCMB" className="bg-surface-900 text-white">PCMB (Maths & Biology)</option>
                    <option value="Commerce" className="bg-surface-900 text-white">Commerce (Accounts, Business, Econ)</option>
                    <option value="Arts" className="bg-surface-900 text-white">Arts / Humanities / Social Sciences</option>
                    <option value="Science" className="bg-surface-900 text-white">General Science</option>
                  </select>
                </div>
              </div>
            </div>

            {/* Career Interests Checklist */}
            <div className="border-t border-white/10 pt-6">
              <label className="block text-sm font-medium text-white/70 mb-3">
                {currentLocale === 'hi' ? 'करियर श्रेणियाँ जिनमे आपकी रुचि है (बहु-चयन):' : 'Career Categories of Interest (Select all that apply):'}
              </label>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {[
                  { id: 'engineering', nameEn: '💻 Engineering & Technology', nameHi: '💻 इंजीनियरिंग और तकनीकी' },
                  { id: 'medicine', nameEn: '🩺 Medicine & Healthcare', nameHi: '🩺 चिकित्सा और स्वास्थ्य सेवा' },
                  { id: 'science', nameEn: '🔬 Science & Research', nameHi: '🔬 विज्ञान और अनुसंधान' },
                  { id: 'business', nameEn: '💼 Business & Management', nameHi: '💼 व्यवसाय और प्रबंधन' },
                  { id: 'law', nameEn: '⚖️ Law & Legal Services', nameHi: '⚖️ कानून और कानूनी सेवाएं' },
                  { id: 'arts', nameEn: '🎨 Arts & Design', nameHi: '🎨 कला और डिजाइन' },
                  { id: 'education', nameEn: '🎓 Education & Teaching', nameHi: '🎓 शिक्षा और शिक्षण' },
                  { id: 'government', nameEn: '🏛️ Government & Civil Services', nameHi: '🏛️ सरकारी और नागरिक सेवाएं' },
                  { id: 'media', nameEn: '📢 Media & Communications', nameHi: '📢 मीडिया और संचार' },
                  { id: 'sports', nameEn: '🏋️ Sports & Fitness', nameHi: '🏋️ खेल और फिटनेस' }
                ].map((cat) => {
                  const isChecked = selectedInterests.includes(cat.id);
                  return (
                    <button
                      key={cat.id}
                      type="button"
                      onClick={() => {
                        if (isChecked) {
                          setSelectedInterests(selectedInterests.filter(id => id !== cat.id));
                        } else {
                          setSelectedInterests([...selectedInterests, cat.id]);
                        }
                      }}
                      className={`flex items-center gap-3 px-4 py-3 rounded-xl border text-sm font-medium transition-all text-left w-full
                        ${isChecked 
                          ? 'bg-brand-500/10 border-brand-500 text-brand-300 shadow-sm' 
                          : 'bg-white/5 border-white/10 text-white/70 hover:bg-white/10 hover:text-white'}`}
                    >
                      <input
                        type="checkbox"
                        checked={isChecked}
                        readOnly
                        className="rounded border-white/20 text-brand-500 focus:ring-0 focus:ring-offset-0 pointer-events-none"
                      />
                      <span>{currentLocale === 'hi' ? cat.nameHi : cat.nameEn}</span>
                    </button>
                  );
                })}
              </div>
            </div>

            {/* Preferred Locale */}
            <div className="border-t border-white/10 pt-6">
              <label className="block text-sm font-medium text-white/70 mb-2">
                {translate('preferredLanguageLabel', currentLocale)}
              </label>
              <div className="relative">
                <Globe className="absolute left-3.5 top-3.5 w-4 h-4 text-white/30 pointer-events-none" />
                <select
                  value={preferredLocale}
                  onChange={(e) => setPreferredLocale(e.target.value)}
                  className="w-full bg-surface-900 border border-white/10 rounded-xl pl-11 pr-4 py-3 text-sm text-white outline-none focus:border-brand-500 transition-all appearance-none cursor-pointer"
                >
                  <option value="en" className="bg-surface-900 text-white">English</option>
                  <option value="hi" className="bg-surface-900 text-white">हिंदी (Hindi)</option>
                </select>
              </div>
            </div>

            {/* Submit Button */}
            <button
              type="submit"
              disabled={saving}
              className="btn-brand w-full py-3.5 text-sm font-semibold flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {saving ? (
                <>
                  <Loader2 className="w-4 h-4 animate-spin" />
                  {translate('loadingText', currentLocale)}
                </>
              ) : (
                <>
                  <Save className="w-4 h-4" />
                  {translate('saveChanges', currentLocale)}
                </>
              )}
            </button>
          </form>
        )}
        </div>
      </div>
    </>
  )
}
