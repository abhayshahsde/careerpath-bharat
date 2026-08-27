'use client'

import { useState, useEffect, useRef } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { Compass, Mail, Lock, Eye, EyeOff, ArrowLeft, CheckCircle2, AlertCircle, RefreshCw, KeyRound, MessageSquare, PhoneCall, ShieldCheck, Clock } from 'lucide-react'
import { api } from '@/lib/api'
import { getLocaleFromUrl } from '@/lib/i18n'

type Step = 'IDENTIFIER' | 'OTP' | 'NEW_PASSWORD' | 'SUCCESS'
type Channel = 'Email' | 'WhatsApp'

export default function ForgotPasswordPage() {
  const router = useRouter()
  const [loc, setLoc] = useState('en')

  // Step flow state
  const [step, setStep] = useState<Step>('IDENTIFIER')
  const [channel, setChannel] = useState<Channel>('Email')
  const [identifier, setIdentifier] = useState('')

  // OTP state
  const [otp, setOtp] = useState(['', '', '', '', '', ''])
  const otpInputs = useRef<(HTMLInputElement | null)[]>([])
  const [countdown, setCountdown] = useState(60)
  const [canResend, setCanResend] = useState(false)
  const [resending, setResending] = useState(false)
  const [resetToken, setResetToken] = useState('')

  // New Password state
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPw, setShowPw] = useState(false)
  const [showConfirmPw, setShowConfirmPw] = useState(false)

  // Status indicators
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [infoMsg, setInfoMsg] = useState('')

  useEffect(() => {
    setLoc(getLocaleFromUrl())
  }, [])

  // 60-second countdown timer for Resend OTP
  useEffect(() => {
    let timer: NodeJS.Timeout
    if (step === 'OTP' && countdown > 0) {
      setCanResend(false)
      timer = setInterval(() => {
        setCountdown(prev => {
          if (prev <= 1) {
            setCanResend(true)
            return 0
          }
          return prev - 1
        })
      }, 1000)
    }
    return () => clearInterval(timer)
  }, [step, countdown])

  // Step 1: Send OTP
  async function handleSendOtp(e?: React.FormEvent) {
    if (e) e.preventDefault()
    setError('')
    setInfoMsg('')

    const cleanId = identifier.trim()
    if (!cleanId) {
      setError(channel === 'Email' ? 'Please enter your registered Gmail or Email address.' : 'Please enter your registered WhatsApp phone number.')
      return
    }

    setLoading(true)
    try {
      const res = await api.sendForgotPasswordOtp(cleanId, channel)
      setInfoMsg(res.message)
      setCountdown(res.resendCooldownSeconds || 60)
      setCanResend(false)
      setOtp(['', '', '', '', '', ''])
      setStep('OTP')
      setTimeout(() => otpInputs.current[0]?.focus(), 100)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to dispatch verification code. Please try again.')
    } finally {
      setLoading(false)
    }
  }

  // Resend OTP handler
  async function handleResendOtp() {
    if (!canResend || resending) return
    setError('')
    setInfoMsg('')
    setResending(true)
    try {
      const res = await api.sendForgotPasswordOtp(identifier.trim(), channel)
      setInfoMsg(`New OTP sent! ${res.message}`)
      setCountdown(res.resendCooldownSeconds || 60)
      setCanResend(false)
      setOtp(['', '', '', '', '', ''])
      otpInputs.current[0]?.focus()
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to resend OTP code.')
    } finally {
      setResending(false)
    }
  }

  // Handle OTP digit entry
  function handleOtpChange(index: number, val: string) {
    const cleaned = val.replace(/\D/g, '')
    if (cleaned.length > 1) {
      // Pasted full OTP code
      const digits = cleaned.slice(0, 6).split('')
      const nextOtp = [...otp]
      digits.forEach((d, i) => {
        if (i < 6) nextOtp[i] = d
      })
      setOtp(nextOtp)
      const nextFocus = Math.min(digits.length, 5)
      otpInputs.current[nextFocus]?.focus()
      return
    }

    const nextOtp = [...otp]
    nextOtp[index] = cleaned
    setOtp(nextOtp)

    if (cleaned && index < 5) {
      otpInputs.current[index + 1]?.focus()
    }
  }

  function handleOtpKeyDown(index: number, e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Backspace' && !otp[index] && index > 0) {
      otpInputs.current[index - 1]?.focus()
    }
  }

  // Step 2: Verify OTP
  async function handleVerifyOtp(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setInfoMsg('')

    const fullOtp = otp.join('')
    if (fullOtp.length !== 6) {
      setError('Please enter the complete 6-digit verification code.')
      return
    }

    setLoading(true)
    try {
      const res = await api.verifyForgotPasswordOtp(identifier.trim(), fullOtp)
      if (res.success && res.resetToken) {
        setResetToken(res.resetToken)
        setInfoMsg(res.message)
        setStep('NEW_PASSWORD')
      } else {
        setError('Invalid OTP code. Please try again.')
      }
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'OTP verification failed.')
    } finally {
      setLoading(false)
    }
  }

  // Step 3: Reset Password
  async function handleResetPassword(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setInfoMsg('')

    if (newPassword.length < 8) {
      setError('Password must be at least 8 characters long.')
      return
    }

    if (!/[A-Z]/.test(newPassword)) {
      setError('Password must contain at least one uppercase letter (A-Z).')
      return
    }

    if (!/[0-9]/.test(newPassword)) {
      setError('Password must contain at least one numeric digit (0-9).')
      return
    }

    if (newPassword !== confirmPassword) {
      setError('New password and confirm password do not match.')
      return
    }

    setLoading(true)
    try {
      await api.resetPasswordWithOtp(resetToken, newPassword)
      setStep('SUCCESS')
      setTimeout(() => {
        router.push(`/auth/login?locale=${loc}&reset=success`)
      }, 3000)
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Password reset failed.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center px-4 py-20 bg-hero-gradient relative">
      {/* Ambient background glow */}
      <div className="absolute inset-0 pointer-events-none overflow-hidden">
        <div className="absolute top-1/3 left-1/4 w-80 h-80 bg-brand-500/10 rounded-full blur-3xl animate-float" />
        <div className="absolute bottom-1/3 right-1/4 w-80 h-80 bg-accent-purple/10 rounded-full blur-3xl animate-float" style={{ animationDelay: '3s' }} />
      </div>

      <div className="relative w-full max-w-md animate-slide-up">
        {/* Card */}
        <div 
          className="rounded-3xl p-8 md:p-10 border shadow-2xl transition-colors"
          style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
        >
          {/* Header */}
          <div className="text-center mb-6">
            <div className="w-16 h-16 rounded-2xl bg-brand-gradient mx-auto mb-4 flex items-center justify-center shadow-brand animate-glow">
              {step === 'SUCCESS' ? (
                <ShieldCheck className="w-8 h-8 text-white" />
              ) : step === 'NEW_PASSWORD' ? (
                <KeyRound className="w-8 h-8 text-white" />
              ) : (
                <Compass className="w-8 h-8 text-white" />
              )}
            </div>

            <h1 className="font-display font-bold text-2xl" style={{ color: 'var(--text-primary)' }}>
              {step === 'IDENTIFIER' && (loc === 'hi' ? 'पासवर्ड रीसेट करें' : 'Reset Password')}
              {step === 'OTP' && (loc === 'hi' ? 'ओटीपी सत्यापन' : 'Verify OTP Code')}
              {step === 'NEW_PASSWORD' && (loc === 'hi' ? 'नया पासवर्ड सेट करें' : 'Set New Password')}
              {step === 'SUCCESS' && (loc === 'hi' ? 'पासवर्ड सफलतापूर्वक रीसेट' : 'Password Reset Complete')}
            </h1>

            <p className="text-xs mt-1 leading-relaxed" style={{ color: 'var(--text-muted)' }}>
              {step === 'IDENTIFIER' && (loc === 'hi' ? 'ओटीपी प्राप्त करने के लिए अपना पंजीकृत माध्यम चुनें।' : 'Select how you want to receive your 6-digit OTP.')}
              {step === 'OTP' && `${loc === 'hi' ? 'सत्यापन कोड भेजा गया:' : 'Code dispatched to'} ${identifier}`}
              {step === 'NEW_PASSWORD' && (loc === 'hi' ? 'अपने अकाउंट के लिए एक मजबूत पासवर्ड बनाएं।' : 'Create a strong, secure new password for your account.')}
              {step === 'SUCCESS' && (loc === 'hi' ? 'आपको लॉगिन पेज पर रीडायरेक्ट किया जा रहा है...' : 'Redirecting you to the sign-in page...')}
            </p>
          </div>

          {/* Feedback messages */}
          {error && (
            <div className="mb-6 p-4 rounded-xl bg-red-500/10 border border-red-500/20 text-red-600 dark:text-red-400 text-xs flex items-start gap-2.5">
              <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" />
              <span>{error}</span>
            </div>
          )}

          {infoMsg && step !== 'SUCCESS' && (
            <div className="mb-6 p-3.5 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-xs flex items-start gap-2.5">
              <CheckCircle2 className="w-4 h-4 shrink-0 mt-0.5" />
              <span>{infoMsg}</span>
            </div>
          )}

          {/* ── STEP 1: Choose Channel & Identifier ── */}
          {step === 'IDENTIFIER' && (
            <form onSubmit={handleSendOtp} className="space-y-5">
              {/* Channel Selector Tabs */}
              <div>
                <label className="block text-xs font-semibold mb-2" style={{ color: 'var(--text-secondary)' }}>
                  {loc === 'hi' ? 'ओटीपी प्राप्त करने का माध्यम चुनें' : 'Choose OTP Delivery Channel'}
                </label>
                <div className="grid grid-cols-2 gap-2">
                  <button
                    type="button"
                    onClick={() => { setChannel('Email'); setIdentifier(''); setError(''); }}
                    className={`py-3 px-3 rounded-2xl text-xs font-bold border transition-all flex items-center justify-center gap-2 ${
                      channel === 'Email'
                        ? 'bg-brand-500 text-white border-brand-500 shadow-sm ring-2 ring-brand-500/20'
                        : 'border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'
                    }`}
                  >
                    <Mail className="w-4 h-4" />
                    <span>Gmail / Email</span>
                  </button>

                  <button
                    type="button"
                    onClick={() => { setChannel('WhatsApp'); setIdentifier(''); setError(''); }}
                    className={`py-3 px-3 rounded-2xl text-xs font-bold border transition-all flex items-center justify-center gap-2 ${
                      channel === 'WhatsApp'
                        ? 'bg-emerald-600 text-white border-emerald-600 shadow-sm ring-2 ring-emerald-500/20'
                        : 'border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'
                    }`}
                  >
                    <MessageSquare className="w-4 h-4" />
                    <span>WhatsApp</span>
                  </button>
                </div>
              </div>

              {/* Identifier Input */}
              <div>
                <label className="block text-xs font-medium mb-1.5" style={{ color: 'var(--text-secondary)' }}>
                  {channel === 'Email' ? (loc === 'hi' ? 'पंजीकृत ईमेल / जीमेल' : 'Registered Gmail / Email Address') : (loc === 'hi' ? 'व्हाट्सएप मोबाइल नंबर' : 'WhatsApp Mobile Number')}
                </label>
                <div className="relative">
                  {channel === 'Email' ? (
                    <Mail className="absolute left-3.5 top-3.5 w-4 h-4 pointer-events-none" style={{ color: 'var(--text-muted)' }} />
                  ) : (
                    <PhoneCall className="absolute left-3.5 top-3.5 w-4 h-4 pointer-events-none" style={{ color: 'var(--text-muted)' }} />
                  )}
                  <input
                    type={channel === 'Email' ? 'email' : 'tel'}
                    required
                    value={identifier}
                    onChange={e => setIdentifier(e.target.value)}
                    placeholder={channel === 'Email' ? 'you@gmail.com' : '+91 9876543210'}
                    className="w-full rounded-xl px-4 py-3 pl-10 text-xs font-medium outline-none border focus:border-brand-500 transition-colors"
                    style={{ 
                      backgroundColor: 'var(--bg-app)', 
                      borderColor: 'var(--border-color)',
                      color: 'var(--text-primary)'
                    }}
                  />
                </div>
                <p className="text-[11px] mt-1.5" style={{ color: 'var(--text-muted)' }}>
                  {channel === 'Email' 
                    ? 'A 6-digit OTP code will be sent to your Gmail inbox.' 
                    : 'A 6-digit OTP code will be sent to your WhatsApp number.'}
                </p>
              </div>

              <button
                type="submit"
                disabled={loading}
                className="w-full btn-brand py-3.5 rounded-2xl flex items-center justify-center gap-2 text-xs font-bold shadow-brand disabled:opacity-60"
              >
                {loading ? (
                  <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                ) : (
                  <>
                    <KeyRound className="w-4 h-4" /> {loc === 'hi' ? 'ओटीपी कोड भेजें' : 'Send Verification OTP'}
                  </>
                )}
              </button>
            </form>
          )}

          {/* ── STEP 2: Enter 6-digit OTP ── */}
          {step === 'OTP' && (
            <form onSubmit={handleVerifyOtp} className="space-y-6">
              <div>
                <label className="block text-center text-xs font-semibold mb-3" style={{ color: 'var(--text-primary)' }}>
                  {loc === 'hi' ? '6-अंकीय ओटीपी दर्ज करें' : 'Enter 6-Digit Verification Code'}
                </label>
                
                {/* 6-box input */}
                <div className="flex justify-center gap-2 sm:gap-2.5">
                  {otp.map((digit, idx) => (
                    <input
                      key={idx}
                      ref={el => { otpInputs.current[idx] = el }}
                      type="text"
                      inputMode="numeric"
                      maxLength={1}
                      value={digit}
                      onChange={e => handleOtpChange(idx, e.target.value)}
                      onKeyDown={e => handleOtpKeyDown(idx, e)}
                      className="w-11 h-12 text-center font-display font-bold text-lg rounded-xl border outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-500/20 transition-all"
                      style={{
                        backgroundColor: 'var(--bg-app)',
                        borderColor: 'var(--border-color)',
                        color: 'var(--text-primary)'
                      }}
                    />
                  ))}
                </div>
              </div>

              {/* Timer & Resend Button */}
              <div 
                className="p-3.5 rounded-2xl border flex items-center justify-between gap-3 text-xs"
                style={{ backgroundColor: 'var(--bg-app)', borderColor: 'var(--border-color)' }}
              >
                <div className="flex items-center gap-2" style={{ color: 'var(--text-muted)' }}>
                  <Clock className="w-4 h-4 text-brand-500" />
                  {countdown > 0 ? (
                    <span>Resend in <strong className="text-brand-500 font-bold">{countdown}s</strong></span>
                  ) : (
                    <span className="text-emerald-500 font-medium">OTP Expired / Ready</span>
                  )}
                </div>

                <button
                  type="button"
                  onClick={handleResendOtp}
                  disabled={!canResend || resending}
                  className={`text-xs font-bold px-3 py-1.5 rounded-xl border flex items-center gap-1.5 transition-all ${
                    canResend
                      ? 'bg-brand-500 text-white border-brand-500 shadow-sm hover:opacity-90'
                      : 'opacity-40 cursor-not-allowed border-slate-300 dark:border-slate-700 text-slate-500'
                  }`}
                >
                  <RefreshCw className={`w-3.5 h-3.5 ${resending ? 'animate-spin' : ''}`} />
                  {resending ? 'Sending...' : 'Resend OTP'}
                </button>
              </div>

              <div className="space-y-2.5">
                <button
                  type="submit"
                  disabled={loading || otp.join('').length !== 6}
                  className="w-full btn-brand py-3.5 rounded-2xl flex items-center justify-center gap-2 text-xs font-bold shadow-brand disabled:opacity-60"
                >
                  {loading ? (
                    <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                  ) : (
                    <>
                      <ShieldCheck className="w-4 h-4" /> {loc === 'hi' ? 'ओटीपी सत्यापित करें' : 'Verify & Continue'}
                    </>
                  )}
                </button>

                <button
                  type="button"
                  onClick={() => { setStep('IDENTIFIER'); setError(''); setInfoMsg(''); }}
                  className="w-full py-2.5 text-xs font-medium hover:underline text-center"
                  style={{ color: 'var(--text-muted)' }}
                >
                  ← {loc === 'hi' ? 'ईमेल / नंबर बदलें' : 'Change Email or Number'}
                </button>
              </div>
            </form>
          )}

          {/* ── STEP 3: Set New Password ── */}
          {step === 'NEW_PASSWORD' && (
            <form onSubmit={handleResetPassword} className="space-y-4">
              {/* New Password */}
              <div>
                <label className="block text-xs font-medium mb-1.5" style={{ color: 'var(--text-secondary)' }}>
                  {loc === 'hi' ? 'नया पासवर्ड' : 'New Password'}
                </label>
                <div className="relative">
                  <Lock className="absolute left-3.5 top-3.5 w-4 h-4 pointer-events-none" style={{ color: 'var(--text-muted)' }} />
                  <input
                    type={showPw ? 'text' : 'password'}
                    required
                    value={newPassword}
                    onChange={e => setNewPassword(e.target.value)}
                    placeholder="Min 8 chars, 1 uppercase & 1 digit"
                    className="w-full rounded-xl px-4 py-3 pl-10 pr-10 text-xs font-medium outline-none border focus:border-brand-500 transition-colors"
                    style={{ 
                      backgroundColor: 'var(--bg-app)', 
                      borderColor: 'var(--border-color)',
                      color: 'var(--text-primary)'
                    }}
                  />
                  <button
                    type="button"
                    onClick={() => setShowPw(!showPw)}
                    className="absolute right-3 top-3.5 text-slate-400 hover:text-slate-600 dark:hover:text-white"
                  >
                    {showPw ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
              </div>

              {/* Confirm Password */}
              <div>
                <label className="block text-xs font-medium mb-1.5" style={{ color: 'var(--text-secondary)' }}>
                  {loc === 'hi' ? 'पासवर्ड की पुष्टि करें' : 'Confirm New Password'}
                </label>
                <div className="relative">
                  <Lock className="absolute left-3.5 top-3.5 w-4 h-4 pointer-events-none" style={{ color: 'var(--text-muted)' }} />
                  <input
                    type={showConfirmPw ? 'text' : 'password'}
                    required
                    value={confirmPassword}
                    onChange={e => setConfirmPassword(e.target.value)}
                    placeholder="Re-enter your new password"
                    className="w-full rounded-xl px-4 py-3 pl-10 pr-10 text-xs font-medium outline-none border focus:border-brand-500 transition-colors"
                    style={{ 
                      backgroundColor: 'var(--bg-app)', 
                      borderColor: 'var(--border-color)',
                      color: 'var(--text-primary)'
                    }}
                  />
                  <button
                    type="button"
                    onClick={() => setShowConfirmPw(!showConfirmPw)}
                    className="absolute right-3 top-3.5 text-slate-400 hover:text-slate-600 dark:hover:text-white"
                  >
                    {showConfirmPw ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
              </div>

              {/* Requirements Checklist */}
              <div 
                className="p-3 rounded-xl border space-y-1.5 text-[11px]"
                style={{ backgroundColor: 'var(--bg-app)', borderColor: 'var(--border-color)' }}
              >
                <div className={`flex items-center gap-2 ${newPassword.length >= 8 ? 'text-emerald-500 font-semibold' : 'text-slate-400'}`}>
                  <span className="text-xs">{newPassword.length >= 8 ? '✓' : '○'}</span> At least 8 characters
                </div>
                <div className={`flex items-center gap-2 ${/[A-Z]/.test(newPassword) ? 'text-emerald-500 font-semibold' : 'text-slate-400'}`}>
                  <span className="text-xs">{/[A-Z]/.test(newPassword) ? '✓' : '○'}</span> At least one uppercase letter (A-Z)
                </div>
                <div className={`flex items-center gap-2 ${/[0-9]/.test(newPassword) ? 'text-emerald-500 font-semibold' : 'text-slate-400'}`}>
                  <span className="text-xs">{/[0-9]/.test(newPassword) ? '✓' : '○'}</span> At least one number (0-9)
                </div>
              </div>

              <button
                type="submit"
                disabled={loading || !newPassword || !confirmPassword}
                className="w-full btn-brand py-3.5 rounded-2xl flex items-center justify-center gap-2 text-xs font-bold shadow-brand disabled:opacity-60"
              >
                {loading ? (
                  <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                ) : (
                  <>
                    <KeyRound className="w-4 h-4" /> {loc === 'hi' ? 'पासवर्ड सहेजें और लॉगिन करें' : 'Update Password & Sign In'}
                  </>
                )}
              </button>
            </form>
          )}

          {/* ── STEP 4: Success ── */}
          {step === 'SUCCESS' && (
            <div className="text-center py-4 space-y-4">
              <div className="w-16 h-16 rounded-full bg-emerald-500/20 text-emerald-500 mx-auto flex items-center justify-center">
                <CheckCircle2 className="w-10 h-10" />
              </div>
              <p className="text-xs font-medium" style={{ color: 'var(--text-secondary)' }}>
                {loc === 'hi'
                  ? 'आपका पासवर्ड सफलतापूर्वक अपडेट कर दिया गया है। आप अब नए पासवर्ड के साथ लॉगिन कर सकते हैं।'
                  : 'Your password has been reset securely. You will now be redirected to sign in.'}
              </p>
              <Link
                href={`/auth/login?locale=${loc}`}
                className="inline-flex items-center justify-center btn-brand py-3 px-6 rounded-2xl text-xs font-bold shadow-brand"
              >
                {loc === 'hi' ? 'लॉगिन करें' : 'Go to Login Now'}
              </Link>
            </div>
          )}

          {/* Back to Login Link */}
          {step !== 'SUCCESS' && (
            <div className="mt-6 pt-6 border-t text-center" style={{ borderColor: 'var(--border-color)' }}>
              <Link
                href={`/auth/login?locale=${loc}`}
                className="inline-flex items-center gap-2 text-xs font-semibold text-brand-500 hover:text-brand-400 transition-colors"
              >
                <ArrowLeft className="w-3.5 h-3.5" />
                {loc === 'hi' ? 'लॉगिन पेज पर वापस जाएं' : 'Back to Login'}
              </Link>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
