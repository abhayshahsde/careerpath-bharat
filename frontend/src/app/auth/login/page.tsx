'use client'

import { useState, useEffect } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { Compass, Eye, EyeOff, LogIn, Mail, Lock } from 'lucide-react'
import { api } from '@/lib/api'
import { useAuth } from '@/lib/auth-context'
import { translate, getLocaleFromUrl } from '@/lib/i18n'

export default function LoginPage() {
  const router = useRouter()
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPw, setShowPw] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [loc, setLoc] = useState('en')

  useEffect(() => {
    setLoc(getLocaleFromUrl())
  }, [])

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const res = await api.login(email, password)
      login(res.accessToken, res.refreshToken, res.user)
      router.push(`/dashboard?locale=${loc}`)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Login failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center px-4 py-20 bg-hero-gradient relative">
      {/* Background blobs */}
      <div className="absolute inset-0 pointer-events-none overflow-hidden">
        <div className="absolute top-1/3 left-1/4 w-80 h-80 bg-brand-500/10 rounded-full blur-3xl animate-float" />
        <div className="absolute bottom-1/3 right-1/4 w-80 h-80 bg-accent-purple/10 rounded-full blur-3xl animate-float" style={{ animationDelay: '3s' }} />
      </div>

      <div className="relative w-full max-w-md animate-slide-up">
        {/* Card */}
        <div className="glass rounded-3xl p-8 md:p-10 border border-white/10 shadow-2xl">
          {/* Logo */}
          <div className="text-center mb-8">
            <div className="w-16 h-16 rounded-2xl bg-brand-gradient mx-auto mb-4 flex items-center justify-center shadow-brand animate-glow">
              <Compass className="w-8 h-8 text-white" />
            </div>
            <h1 className="font-display font-bold text-2xl" style={{ color: 'var(--text-primary)' }}>
              {translate('loginTitle', loc)}
            </h1>
            <p className="text-sm mt-1" style={{ color: 'var(--text-muted)' }}>
              {loc === 'hi' ? 'अपने अकाउंट में लॉग इन करें' : 'Welcome back! Sign in to continue'}
            </p>
          </div>

          {error && (
            <div className="mb-6 p-4 rounded-xl bg-red-500/10 border border-red-500/20 text-red-500 text-sm flex items-start gap-2">
              <span className="text-base leading-none">⚠️</span>
              <span>{error}</span>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-5">
            {/* Email */}
            <div>
              <label className="block text-sm font-medium mb-2" style={{ color: 'var(--text-secondary)' }} htmlFor="email">
                {translate('emailLabel', loc)}
              </label>
              <div className="relative">
                <Mail className="absolute left-3.5 top-3.5 w-4 h-4 pointer-events-none" style={{ color: 'var(--text-muted)' }} />
                <input
                  id="email"
                  type="email"
                  required
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  placeholder="you@example.com"
                  className="input pl-10"
                />
              </div>
            </div>

            {/* Password */}
            <div>
              <label className="block text-sm font-medium mb-2" style={{ color: 'var(--text-secondary)' }} htmlFor="password">
                {translate('passwordLabel', loc)}
              </label>
              <div className="relative">
                <Lock className="absolute left-3.5 top-3.5 w-4 h-4 pointer-events-none" style={{ color: 'var(--text-muted)' }} />
                <input
                  id="password"
                  type={showPw ? 'text' : 'password'}
                  required
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="input pl-10 pr-12"
                />
                <button type="button" onClick={() => setShowPw(!showPw)}
                  className="absolute right-3.5 top-3.5 transition-colors"
                  style={{ color: 'var(--text-muted)' }}
                >
                  {showPw ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                </button>
              </div>
            </div>

            <button type="submit" disabled={loading}
              className="w-full btn-brand py-3.5 flex items-center justify-center gap-2 disabled:opacity-60 disabled:cursor-not-allowed mt-2">
              {loading ? (
                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
              ) : (
                <><LogIn className="w-4 h-4" /> {translate('submitLogin', loc)}</>
              )}
            </button>
          </form>

          <p className="text-center text-sm mt-6" style={{ color: 'var(--text-muted)' }}>
            {translate('noAccount', loc)}{' '}
            <Link href={`/auth/register?locale=${loc}`} className="text-brand-500 hover:text-brand-400 font-semibold transition-colors">
              {loc === 'hi' ? 'मुफ़्त में बनाएं' : 'Create one free'}
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
