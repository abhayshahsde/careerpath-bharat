'use client'

import { useState, useEffect } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { Compass, Eye, EyeOff, UserPlus } from 'lucide-react'
import { api } from '@/lib/api'
import { useAuth } from '@/lib/auth-context'
import { translate, getLocaleFromUrl } from '@/lib/i18n'

export default function RegisterPage() {
  const router = useRouter()
  const { login } = useAuth()
  const [form, setForm] = useState({ email: '', password: '', displayName: '' })
  const [showPw, setShowPw] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [loc, setLoc] = useState('en')

  useEffect(() => {
    setLoc(getLocaleFromUrl())
  }, [])

  const update = (k: keyof typeof form) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setForm(p => ({ ...p, [k]: e.target.value }))

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const res = await api.register(form.email, form.password, form.displayName || undefined)
      login(res.accessToken, res.refreshToken, res.user)
      router.push(`/dashboard?locale=${loc}`)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Registration failed')
    } finally {
      setLoading(false)
    }
  }

  const pwStrength = (() => {
    const p = form.password
    if (!p) return 0
    let s = 0
    if (p.length >= 8)         s++
    if (/[A-Z]/.test(p))       s++
    if (/[0-9]/.test(p))       s++
    if (/[^A-Za-z0-9]/.test(p)) s++
    return s
  })()

  const strengthColors = ['', 'bg-red-500', 'bg-amber-500', 'bg-yellow-400', 'bg-accent-teal']
  const strengthLabels = {
    en: ['', 'Weak', 'Fair', 'Good', 'Strong'],
    hi: ['', 'कमजोर', 'सामान्य', 'अच्छा', 'मजबूत']
  }[loc === 'hi' ? 'hi' : 'en']

  return (
    <div className="min-h-screen flex items-center justify-center px-4 py-20 bg-hero-gradient">
      <div className="absolute inset-0 pointer-events-none overflow-hidden">
        <div className="absolute top-1/3 right-1/4 w-72 h-72 bg-brand-500/10 rounded-full blur-3xl animate-float" />
        <div className="absolute bottom-1/3 left-1/4 w-72 h-72 bg-accent-teal/10 rounded-full blur-3xl animate-float" style={{ animationDelay: '2s' }} />
      </div>

      <div className="relative w-full max-w-md animate-slide-up">
        <div className="glass rounded-3xl p-8 border border-white/10">
          <div className="text-center mb-8">
            <div className="w-16 h-16 rounded-2xl bg-brand-gradient mx-auto mb-4 flex items-center justify-center shadow-brand">
              <Compass className="w-8 h-8 text-white" />
            </div>
            <h1 className="font-display font-bold text-2xl text-white">{translate('registerTitle', loc)}</h1>
            <p className="text-white/50 text-sm mt-1">{translate('registerSub', loc)}</p>
          </div>

          {error && (
            <div className="mb-6 p-4 rounded-xl bg-red-500/10 border border-red-500/20 text-red-300 text-sm">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label className="block text-white/60 text-sm font-medium mb-2" htmlFor="displayName">
                {translate('fullNameLabel', loc)} {loc === 'hi' ? '(वैकल्पिक)' : '(optional)'}
              </label>
              <input id="displayName" type="text" value={form.displayName} onChange={update('displayName')}
                placeholder="Priya Sharma" className="input" />
            </div>

            <div>
              <label className="block text-white/60 text-sm font-medium mb-2" htmlFor="reg-email">{translate('emailLabel', loc)}</label>
              <input id="reg-email" type="email" required value={form.email} onChange={update('email')}
                placeholder="you@example.com" className="input" />
            </div>

            <div>
              <label className="block text-white/60 text-sm font-medium mb-2" htmlFor="reg-password">{translate('passwordLabel', loc)}</label>
              <div className="relative">
                <input id="reg-password" type={showPw ? 'text' : 'password'} required value={form.password}
                  onChange={update('password')} placeholder={loc === 'hi' ? 'न्यूनतम 8 वर्ण' : 'Min. 8 characters'} className="input pr-12" />
                <button type="button" onClick={() => setShowPw(!showPw)}
                  className="absolute right-4 top-1/2 -translate-y-1/2 text-white/40 hover:text-white transition-colors">
                  {showPw ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </button>
              </div>
              {/* Strength meter */}
              {form.password && (
                <div className="mt-2">
                  <div className="flex gap-1">
                    {[1, 2, 3, 4].map(n => (
                      <div key={n} className={`h-1 flex-1 rounded-full transition-all duration-300 ${n <= pwStrength ? strengthColors[pwStrength] : 'bg-white/10'}`} />
                    ))}
                  </div>
                  <p className="text-xs mt-1 text-white/40">{strengthLabels[pwStrength]} {loc === 'hi' ? 'पासवर्ड' : 'password'}</p>
                </div>
              )}
            </div>

            <div className="text-xs text-white/30 mt-2">
              {loc === 'hi'
                ? 'कम से कम एक बड़े अक्षर और एक अंक के साथ 8+ वर्ण होने चाहिए।'
                : 'Must be 8+ characters with at least one uppercase letter and one digit.'}
            </div>

            <button type="submit" disabled={loading}
              className="w-full btn-brand py-3.5 flex items-center justify-center gap-2 disabled:opacity-60 disabled:cursor-not-allowed">
              {loading
                ? <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                : <><UserPlus className="w-4 h-4" /> {translate('submitRegister', loc)}</>}
            </button>
          </form>

          <p className="text-center text-white/40 text-sm mt-6">
            {translate('haveAccount', loc)}{' '}
            <Link href={`/auth/login?locale=${loc}`} className="text-brand-400 hover:text-brand-300 font-medium transition-colors">
              {loc === 'hi' ? 'यहाँ लॉग इन करें' : 'Sign in here'}
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
