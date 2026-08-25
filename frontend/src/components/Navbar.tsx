'use client'

import Link from 'next/link'
import { usePathname, useRouter } from 'next/navigation'
import { useState, useEffect } from 'react'
import { Menu, X, Compass, BookOpen, GraduationCap, Award, LogIn, LogOut, User, ChevronDown, Shield, Sparkles, Sun, Moon } from 'lucide-react'
import { useAuth } from '@/lib/auth-context'
import { translate } from '@/lib/i18n'

const navLinks = [
  { href: '/careers',      label: 'Careers',      icon: Compass },
  { href: '/exams',        label: 'Exams',         icon: BookOpen },
  { href: '/courses',      label: 'Courses',       icon: GraduationCap },
  { href: '/scholarships', label: 'Scholarships',  icon: Award },
]

export default function Navbar() {
  const pathname  = usePathname()
  const router    = useRouter()
  const { user, isAuthenticated, isLoading, logout } = useAuth()
  
  const links = [
    ...(isAuthenticated ? [
      { href: '/dashboard', label: 'dashboard', icon: Sparkles },
      { href: '/me/roadmaps', label: 'roadmaps', icon: Compass }
    ] : []),
    ...navLinks.map(l => ({ ...l, label: l.label.toLowerCase() }))
  ]

  const [open, setOpen]           = useState(false)
  const [userMenuOpen, setUserMenuOpen] = useState(false)
  const [currentLocale, setCurrentLocale] = useState('en')
  const [theme, setTheme] = useState<'light' | 'dark'>('light')

  useEffect(() => {
    if (typeof window !== 'undefined') {
      const saved = localStorage.getItem('locale') ?? 'en'
      setCurrentLocale(saved)
      document.cookie = `locale=${saved}; path=/; max-age=${365*24*60*60};`

      const savedTheme = localStorage.getItem('theme') as 'light' | 'dark' | null
      const activeTheme = savedTheme ?? 'light'
      setTheme(activeTheme)
      if (activeTheme === 'dark') {
        document.documentElement.classList.add('dark')
      } else {
        document.documentElement.classList.remove('dark')
      }
    }
  }, [])

  function toggleTheme() {
    const nextTheme = theme === 'light' ? 'dark' : 'light'
    setTheme(nextTheme)
    localStorage.setItem('theme', nextTheme)
    if (nextTheme === 'dark') {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
  }

  function handleLocaleChange(newLocale: string) {
    localStorage.setItem('locale', newLocale)
    setCurrentLocale(newLocale)
    if (typeof window !== 'undefined') {
      document.cookie = `locale=${newLocale}; path=/; max-age=${365*24*60*60};`
      const url = new URL(window.location.href)
      url.searchParams.delete('locale')
      window.location.href = url.pathname + url.search
    }
  }

  function handleLogout() {
    logout()
    setUserMenuOpen(false)
    router.push('/')
  }

  const initials = user?.displayName
    ? user.displayName.split(' ').map(w => w[0]).join('').toUpperCase().slice(0, 2)
    : user?.email?.[0]?.toUpperCase() ?? '?'

  return (
    <nav className="sticky top-0 z-50 glass border-b border-white/10">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex items-center justify-between h-16">

          {/* ── Logo ── */}
          <Link href={`/?locale=${currentLocale}`} className="flex items-center gap-2.5 group shrink-0">
            <div className="w-9 h-9 rounded-xl bg-brand-gradient flex items-center justify-center
                            shadow-brand group-hover:shadow-glow transition-shadow duration-300">
              <Compass className="w-5 h-5 text-white" />
            </div>
            <span className="font-display font-bold text-lg">
              <span className="gradient-text">{translate('logoText', currentLocale)}</span>{' '}
              <span className="text-sm font-medium" style={{ color: 'var(--text-muted)' }}>
                {translate('logoSub', currentLocale)}
              </span>
            </span>
          </Link>

          {/* ── Desktop Nav Links ── */}
          <div className="hidden md:flex items-center gap-1">
            {links.map(({ href, label }) => (
              <Link
                key={href}
                href={href}
                className={`px-4 py-2 rounded-lg text-sm font-medium transition-all duration-200
                  ${pathname.startsWith(href)
                    ? 'bg-brand-500/10 text-brand-500'
                    : 'hover:bg-black/5 dark:hover:bg-white/5'}`}
                style={pathname.startsWith(href) ? {} : { color: 'var(--text-secondary)' }}
              >
                {translate(label, currentLocale)}
              </Link>
            ))}
          </div>

          {/* ── Right side controls ── */}
          <div className="flex items-center gap-2">

            {/* Language Switcher */}
            <div className="hidden sm:flex items-center border rounded-lg px-2.5 py-1.5 text-xs"
              style={{ borderColor: 'var(--border-color)', backgroundColor: 'var(--card-bg)', color: 'var(--text-secondary)' }}>
              <select
                value={currentLocale}
                onChange={(e) => handleLocaleChange(e.target.value)}
                className="bg-transparent outline-none cursor-pointer pr-1 text-xs"
                style={{ color: 'var(--text-secondary)' }}
              >
                <option value="en">English</option>
                <option value="hi">हिंदी (Hindi)</option>
              </select>
            </div>

            {/* Theme Toggle */}
            <button
              onClick={toggleTheme}
              className="w-9 h-9 rounded-lg flex items-center justify-center transition-all border"
              style={{ borderColor: 'var(--border-color)', backgroundColor: 'var(--card-bg)', color: 'var(--text-secondary)' }}
              title={theme === 'light' ? 'Switch to Dark Mode' : 'Switch to Light Mode'}
              aria-label="Toggle Theme"
            >
              {theme === 'light'
                ? <Moon className="w-4 h-4" />
                : <Sun className="w-4 h-4 text-amber-400" />
              }
            </button>

            {isLoading ? (
              <div className="w-9 h-9 rounded-lg animate-pulse" style={{ backgroundColor: 'var(--card-bg)' }} />
            ) : isAuthenticated ? (
              /* Logged-in: avatar + dropdown */
              <div className="relative hidden md:block">
                <button
                  onClick={() => setUserMenuOpen(!userMenuOpen)}
                  className="flex items-center gap-2 px-3 py-1.5 rounded-lg border text-sm font-medium transition-all"
                  style={{ borderColor: 'var(--border-color)', backgroundColor: 'var(--card-bg)', color: 'var(--text-secondary)' }}
                >
                  <div className="w-6 h-6 rounded-full bg-brand-gradient flex items-center justify-center text-xs font-bold text-white shadow-brand shrink-0">
                    {initials}
                  </div>
                  <span className="max-w-[100px] truncate">{user?.displayName ?? user?.email}</span>
                  <ChevronDown className={`w-3.5 h-3.5 transition-transform ${userMenuOpen ? 'rotate-180' : ''}`} />
                </button>

                {/* Dropdown */}
                {userMenuOpen && (
                  <div className="absolute right-0 mt-2 w-52 glass rounded-xl border border-white/10 shadow-xl overflow-hidden animate-slide-up">
                    <div className="px-4 py-3 border-b" style={{ borderColor: 'var(--border-color)' }}>
                      <p className="text-sm font-semibold truncate" style={{ color: 'var(--text-primary)' }}>{user?.displayName ?? 'User'}</p>
                      <p className="text-xs truncate mt-0.5" style={{ color: 'var(--text-muted)' }}>{user?.email}</p>
                      {user?.roles?.includes('Admin') && (
                        <span className="inline-block mt-1.5 px-2 py-0.5 text-xs rounded-full bg-brand-500/20 text-brand-500 font-medium">Admin</span>
                      )}
                    </div>
                    <div className="p-1">
                      {user?.roles?.includes('Admin') && (
                        <Link
                          href="/admin"
                          onClick={() => setUserMenuOpen(false)}
                          className="flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm font-medium transition-colors w-full text-brand-500 hover:bg-brand-500/10"
                        >
                          <Shield className="w-4 h-4" />
                          {translate('adminControl', currentLocale)}
                        </Link>
                      )}
                      <Link
                        href="/me/profile"
                        onClick={() => setUserMenuOpen(false)}
                        className="flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm transition-colors w-full hover:bg-black/5 dark:hover:bg-white/5"
                        style={{ color: 'var(--text-secondary)' }}
                      >
                        <User className="w-4 h-4" />
                        {translate('myProfile', currentLocale)}
                      </Link>
                      <Link
                        href="/me/roadmaps"
                        onClick={() => setUserMenuOpen(false)}
                        className="flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm transition-colors w-full hover:bg-black/5 dark:hover:bg-white/5"
                        style={{ color: 'var(--text-secondary)' }}
                      >
                        <Compass className="w-4 h-4" />
                        {translate('myRoadmaps', currentLocale)}
                      </Link>
                      <button
                        onClick={handleLogout}
                        className="flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm text-red-500 hover:bg-red-500/10 transition-colors w-full"
                      >
                        <LogOut className="w-4 h-4" />
                        {translate('signOut', currentLocale)}
                      </button>
                    </div>
                  </div>
                )}
              </div>
            ) : (
              /* Guest: Sign In + Get Started — identical height via h-9 */
              <div className="hidden md:flex items-center gap-2">
                <Link
                  href="/auth/login"
                  className="flex items-center gap-1.5 h-9 px-4 rounded-lg border text-sm font-medium transition-all"
                  style={{ borderColor: 'var(--border-color)', backgroundColor: 'var(--card-bg)', color: 'var(--text-secondary)' }}
                >
                  <LogIn className="w-3.5 h-3.5" />
                  {translate('signIn', currentLocale)}
                </Link>
                <Link
                  href="/auth/register"
                  className="flex items-center h-9 px-4 rounded-lg bg-brand-gradient text-white text-sm font-semibold shadow-brand hover:shadow-glow hover:scale-[1.02] active:scale-[0.98] transition-all"
                >
                  {translate('getStarted', currentLocale)}
                </Link>
              </div>
            )}

            {/* Mobile hamburger */}
            <button
              onClick={() => setOpen(!open)}
              className="md:hidden w-9 h-9 flex items-center justify-center rounded-lg border transition-all"
              style={{ borderColor: 'var(--border-color)', backgroundColor: 'var(--card-bg)', color: 'var(--text-secondary)' }}
              aria-label="Toggle menu"
            >
              {open ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
            </button>
          </div>
        </div>
      </div>

      {/* ── Mobile menu ── */}
      {open && (
        <div className="md:hidden border-t animate-slide-up" style={{ borderColor: 'var(--border-color)', backgroundColor: 'var(--card-bg)' }}>
          <div className="px-4 py-4 flex flex-col gap-1">
            {links.map(({ href, label, icon: Icon }) => (
              <Link
                key={href}
                href={href}
                onClick={() => setOpen(false)}
                className={`flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium transition-all
                  ${pathname.startsWith(href)
                    ? 'bg-brand-500/10 text-brand-500'
                    : 'hover:bg-black/5 dark:hover:bg-white/5'}`}
                style={pathname.startsWith(href) ? {} : { color: 'var(--text-secondary)' }}
              >
                <Icon className="w-4 h-4" />
                {translate(label, currentLocale)}
              </Link>
            ))}

            <div className="divider my-2" />

            {/* Mobile Language */}
            <div className="flex items-center border rounded-xl px-4 py-3"
              style={{ borderColor: 'var(--border-color)' }}>
              <select
                value={currentLocale}
                onChange={(e) => handleLocaleChange(e.target.value)}
                className="bg-transparent outline-none w-full text-sm"
                style={{ color: 'var(--text-secondary)' }}
              >
                <option value="en">English</option>
                <option value="hi">हिंदी (Hindi)</option>
              </select>
            </div>

            {isAuthenticated ? (
              <>
                <div className="px-4 py-2 mt-1">
                  <p className="text-sm font-semibold" style={{ color: 'var(--text-primary)' }}>{user?.displayName ?? 'User'}</p>
                  <p className="text-xs mt-0.5" style={{ color: 'var(--text-muted)' }}>{user?.email}</p>
                </div>
                <Link href="/me/profile" onClick={() => setOpen(false)}
                  className="flex items-center gap-2 px-4 py-3 rounded-xl text-sm transition-colors hover:bg-black/5 dark:hover:bg-white/5"
                  style={{ color: 'var(--text-secondary)' }}>
                  <User className="w-4 h-4" />
                  {translate('myProfile', currentLocale)}
                </Link>
                <button
                  onClick={() => { setOpen(false); handleLogout() }}
                  className="flex items-center gap-2 px-4 py-3 rounded-xl text-sm text-red-500 hover:bg-red-500/10 transition-colors"
                >
                  <LogOut className="w-4 h-4" />
                  {translate('signOut', currentLocale)}
                </button>
              </>
            ) : (
              <>
                <Link href="/auth/login" onClick={() => setOpen(false)}
                  className="flex items-center justify-center gap-2 px-4 py-3 rounded-xl border text-sm font-medium transition-all"
                  style={{ borderColor: 'var(--border-color)', color: 'var(--text-secondary)' }}>
                  <LogIn className="w-4 h-4" />
                  {translate('signIn', currentLocale)}
                </Link>
                <Link href="/auth/register" onClick={() => setOpen(false)}
                  className="flex items-center justify-center py-3 rounded-xl bg-brand-gradient text-white text-sm font-semibold shadow-brand mt-1">
                  {translate('getStarted', currentLocale)}
                </Link>
              </>
            )}
          </div>
        </div>
      )}
    </nav>
  )
}
