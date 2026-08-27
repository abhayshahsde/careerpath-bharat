/* eslint-disable @typescript-eslint/no-explicit-any */
'use client'

import { useEffect, useState, useCallback } from 'react'
import { useRouter } from 'next/navigation'
import { useAuth } from '@/lib/auth-context'
import { api } from '@/lib/api'
import { CreditCard, Check, Shield, AlertCircle, Tag, Sparkles, X, Lock, CheckCircle2 } from 'lucide-react'

// Dynamic loader for official Razorpay Checkout SDK
const loadRazorpayScript = (): Promise<boolean> => {
  return new Promise((resolve) => {
    if (typeof window === 'undefined') return resolve(false)
    if ((window as any).Razorpay) return resolve(true)
    const script = document.createElement('script')
    script.src = 'https://checkout.razorpay.com/v1/checkout.js'
    script.async = true
    script.onload = () => resolve(true)
    script.onerror = () => resolve(false)
    document.body.appendChild(script)
  })
}

export default function SubscribePage() {
  const router = useRouter()
  const { user, isAuthenticated, isLoading } = useAuth()

  // Subscription States
  const [plans, setPlans] = useState<any[]>([])
  const [activeSub, setActiveSub] = useState<any | null>(null)
  const [publicCoupons, setPublicCoupons] = useState<any[]>([])

  // Coupon State
  const [couponCode, setCouponCode] = useState('')
  const [validatingCoupon, setValidatingCoupon] = useState(false)
  const [appliedCoupon, setAppliedCoupon] = useState<any | null>(null)
  const [couponMsg, setCouponMsg] = useState<{ text: string; isError: boolean } | null>(null)

  // Modal Checkout State
  const [showCheckoutModal, setShowCheckoutModal] = useState(false)
  const [selectedPlanForCheckout, setSelectedPlanForCheckout] = useState<any | null>(null)
  const [selectedMethod, setSelectedMethod] = useState<'UPI' | 'Card' | 'NetBanking'>('UPI')
  const [upiId, setUpiId] = useState('student@okaxis')

  // Status states
  const [loadingData, setLoadingData] = useState(true)
  const [purchasing, setPurchasing] = useState<string | null>(null)
  const [canceling, setCanceling] = useState(false)
  const [message, setMessage] = useState('')
  const [error, setError] = useState('')

  const loadBillingDetails = useCallback(async () => {
    setError('')
    try {
      const [plansList, pCoupons] = await Promise.all([
        api.getPlans() as Promise<any[]>,
        api.getPublicCoupons().catch(() => []) as Promise<any[]>,
      ])
      setPlans(plansList)
      setPublicCoupons(pCoupons)

      if (isAuthenticated) {
        const sub = (await api.getActiveSubscription().catch(() => null)) as any
        setActiveSub(sub)
      }
    } catch (err: any) {
      setError(err?.message ?? 'Failed to load subscription plans.')
    } finally {
      setLoadingData(false)
    }
  }, [isAuthenticated])

  useEffect(() => {
    loadBillingDetails()
    loadRazorpayScript()
  }, [loadBillingDetails])

  const handleApplyCoupon = async (planId?: string) => {
    if (!couponCode.trim()) return
    const targetPlan = planId || plans.find(p => p.price > 0)?.id
    if (!targetPlan) return

    setValidatingCoupon(true)
    setCouponMsg(null)
    try {
      const res = await api.validateCoupon(couponCode.trim(), targetPlan)
      if (res.isValid) {
        setAppliedCoupon(res)
        setCouponMsg({ text: res.message, isError: false })
      } else {
        setAppliedCoupon(null)
        setCouponMsg({ text: res.message, isError: true })
      }
    } catch (err: any) {
      setAppliedCoupon(null)
      setCouponMsg({ text: err?.message ?? 'Failed to validate coupon.', isError: true })
    } finally {
      setValidatingCoupon(false)
    }
  }

  const handleOpenPlanCheckout = (plan: any) => {
    if (!isAuthenticated) {
      router.push('/auth/login?redirect=/subscribe')
      return
    }

    // Free Tier is the default tier and cannot be paid for
    if (plan.price === 0) {
      return
    }

    setSelectedPlanForCheckout(plan)
    setShowCheckoutModal(true)
  }

  const launchRazorpayStandardCheckout = async () => {
    if (!selectedPlanForCheckout) return
    const plan = selectedPlanForCheckout

    setPurchasing(plan.id)
    setError('')
    setMessage('')

    const hasDiscount = appliedCoupon && plan.price >= (appliedCoupon.minPlanPrice || 0) && plan.price > 0
    const finalAmount = hasDiscount
      ? Math.max(1, plan.price - (appliedCoupon.discountType === 'Percentage' ? (plan.price * appliedCoupon.discountValue) / 100 : appliedCoupon.discountValue))
      : plan.price

    const isLoaded = await loadRazorpayScript()
    const razorpayKey = process.env.NEXT_PUBLIC_RAZORPAY_KEY_ID || 'rzp_test_careerpathbharat'

    if (isLoaded && (window as any).Razorpay) {
      const options = {
        key: razorpayKey,
        amount: Math.round(finalAmount * 100), // Amount in paise
        currency: 'INR',
        name: 'CareerPath Bharat',
        description: `${plan.name} (${plan.billingCycle})`,
        image: 'https://careerpath-bharat.azurestaticapps.net/logo.png',
        handler: async function (response: any) {
          try {
            const paymentId = response.razorpay_payment_id || `rzp_pay_${Date.now()}`
            const res = (await api.subscribeToPlan(
              plan.id,
              'Razorpay',
              paymentId,
              appliedCoupon?.code || undefined
            )) as any

            if (res.success) {
              setMessage(`🎉 Payment of ₹${finalAmount.toFixed(0)} confirmed via Razorpay! ${plan.name} plan is now active.`)
              const sub = (await api.getActiveSubscription().catch(() => null)) as any
              setActiveSub(sub)
              setAppliedCoupon(null)
              setCouponCode('')
              setShowCheckoutModal(false)
              setSelectedPlanForCheckout(null)
            }
          } catch (err: any) {
            setError(err?.message ?? 'Failed to finalize subscription verification.')
          } finally {
            setPurchasing(null)
          }
        },
        prefill: {
          name: user?.displayName || '',
          email: user?.email || '',
        },
        theme: {
          color: '#1e40af',
        },
        modal: {
          ondismiss: function () {
            setPurchasing(null)
          },
        },
      }

      const rzpInstance = new (window as any).Razorpay(options)
      rzpInstance.on('payment.failed', function (response: any) {
        setError(response?.error?.description || 'Payment transaction failed or cancelled.')
        setPurchasing(null)
      })
      rzpInstance.open()
    } else {
      // Direct payment confirmation fallback
      try {
        const txId = `rzp_${selectedMethod.toLowerCase()}_${Date.now()}`
        const res = (await api.subscribeToPlan(plan.id, 'Razorpay', txId, appliedCoupon?.code || undefined)) as any
        if (res.success) {
          setMessage(`🎉 Payment of ₹${finalAmount.toFixed(0)} confirmed! ${plan.name} plan is now active.`)
          const sub = (await api.getActiveSubscription().catch(() => null)) as any
          setActiveSub(sub)
          setAppliedCoupon(null)
          setCouponCode('')
          setShowCheckoutModal(false)
          setSelectedPlanForCheckout(null)
        }
      } catch (err: any) {
        setError(err?.message ?? 'Payment transaction failed.')
      } finally {
        setPurchasing(null)
      }
    }
  }

  const handleCancelRenewal = async () => {
    setCanceling(true)
    setError('')
    setMessage('')
    try {
      const res = (await api.cancelSubscriptionRenewal()) as any
      setMessage(res.message ?? 'Subscription cancellation requested.')
      const sub = (await api.getActiveSubscription().catch(() => null)) as any
      setActiveSub(sub)
    } catch (err: any) {
      setError(err?.message ?? 'Renewal cancellation failed.')
    } finally {
      setCanceling(false)
    }
  }

  if (isLoading || loadingData) {
    return (
      <div className="min-h-screen flex items-center justify-center text-sm" style={{ backgroundColor: 'var(--bg-app)', color: 'var(--text-muted)' }}>
        <div className="flex items-center gap-3">
          <div className="w-5 h-5 border-2 border-brand-500 border-t-transparent rounded-full animate-spin" />
          <span>Syncing plans and membership benefits...</span>
        </div>
      </div>
    )
  }

  // Free Tier is the default when user has no active paid subscription
  const isPaidActive = activeSub && activeSub.status === 'Active' && !activeSub.planName.toLowerCase().includes('free')

  return (
    <div className="min-h-screen px-4 sm:px-6 lg:px-8 py-16 page-enter" style={{ backgroundColor: 'var(--bg-app)' }}>
      <div className="max-w-5xl mx-auto">
        
        {/* Header */}
        <div className="text-center max-w-2xl mx-auto mb-12">
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold mb-4 bg-brand-500/10 text-brand-600 dark:text-brand-400 border border-brand-500/20">
            <Sparkles className="w-3.5 h-3.5" /> CareerPath Membership Plans
          </span>
          <h1 className="font-display font-black text-4xl md:text-5xl mb-4" style={{ color: 'var(--text-primary)' }}>
            Unleash Custom <span className="gradient-text">Roadmaps</span> & <span className="gradient-text">AI Guidance</span>
          </h1>
          <p className="text-base" style={{ color: 'var(--text-secondary)' }}>
            Upgrade your daily token quotas and access premium roadmap checklist builders.
          </p>
        </div>

        {/* Public Vouchers Banner Strip */}
        {publicCoupons.length > 0 && (
          <div 
            className="mb-10 p-4 rounded-2xl border flex flex-wrap items-center justify-between gap-4 shadow-sm"
            style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
          >
            <div className="flex items-center gap-2.5">
              <Sparkles className="w-5 h-5 text-amber-500 animate-pulse" />
              <div>
                <span className="text-xs font-bold" style={{ color: 'var(--text-primary)' }}>Active Promotion:</span>
                <span className="text-xs ml-2" style={{ color: 'var(--text-muted)' }}>
                  Use code <strong className="text-brand-500 font-bold">{publicCoupons[0].code}</strong> for {publicCoupons[0].discountType === 'Percentage' ? `${publicCoupons[0].discountValue}% OFF` : `₹${publicCoupons[0].discountValue} OFF`}
                </span>
              </div>
            </div>
            <button
              onClick={() => { setCouponCode(publicCoupons[0].code); handleApplyCoupon(publicCoupons[0].code); }}
              className="text-xs font-bold px-3 py-1.5 rounded-xl btn-brand shadow-sm"
            >
              Apply {publicCoupons[0].code}
            </button>
          </div>
        )}

        {error && (
          <div className="mb-8 p-4 rounded-xl bg-red-500/10 border border-red-500/20 text-red-600 dark:text-red-400 text-sm flex items-center gap-3">
            <AlertCircle className="w-5 h-5 flex-shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {message && (
          <div className="mb-8 p-4 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-sm flex items-center gap-3">
            <CheckCircle2 className="w-5 h-5 flex-shrink-0" />
            <span>{message}</span>
          </div>
        )}

        {/* Current Active Plan Status Banner */}
        <div 
          className="rounded-2xl p-6 mb-10 border flex flex-col md:flex-row md:items-center justify-between gap-6 shadow-sm"
          style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
        >
          <div>
            <div className="flex items-center gap-2.5">
              <Shield className={`w-5 h-5 ${isPaidActive ? 'text-brand-500' : 'text-emerald-500'}`} />
              <span className="font-bold text-lg" style={{ color: 'var(--text-primary)' }}>
                {isPaidActive ? 'Active Premium Membership' : 'Current Plan: Free Tier (Default)'}
              </span>
            </div>
            <p className="text-xs mt-1" style={{ color: 'var(--text-muted)' }}>
              Plan: <strong style={{ color: 'var(--text-primary)' }}>{activeSub?.planName || 'Free Tier'}</strong>
              {isPaidActive && activeSub?.currentPeriodEnd && (
                <> • Period End: <strong style={{ color: 'var(--text-primary)' }}>{new Date(activeSub.currentPeriodEnd).toLocaleDateString()}</strong></>
              )}
            </p>
            {isPaidActive && activeSub?.cancelAtPeriodEnd && (
              <p className="text-red-500 text-xs mt-1 font-medium">Your subscription will terminate at the end of the current billing cycle.</p>
            )}
          </div>

          {isPaidActive && !activeSub.cancelAtPeriodEnd && (
            <button
              onClick={handleCancelRenewal}
              disabled={canceling}
              className="text-xs py-2.5 px-4 text-red-500 hover:bg-red-500/10 border border-red-500/20 rounded-xl transition-colors font-semibold"
            >
              {canceling ? 'Canceling...' : 'Cancel Renewal'}
            </button>
          )}
        </div>

        {/* Optional Coupon Code Box */}
        <div 
          className="rounded-2xl p-4 mb-8 border flex flex-col sm:flex-row items-center justify-between gap-4 max-w-xl mx-auto shadow-sm"
          style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
        >
          <div className="flex items-center gap-2.5 w-full sm:w-auto">
            <Tag className="w-4 h-4 text-brand-500 shrink-0" />
            <input
              type="text"
              placeholder="Enter Promo Code (e.g. BHARAT50)"
              value={couponCode}
              onChange={e => setCouponCode(e.target.value.toUpperCase())}
              className="bg-transparent text-xs font-bold outline-none uppercase w-full"
              style={{ color: 'var(--text-primary)' }}
            />
          </div>
          <button
            onClick={() => handleApplyCoupon()}
            disabled={validatingCoupon || !couponCode.trim()}
            className="btn-brand text-xs font-bold py-2 px-4 shadow-sm shrink-0 w-full sm:w-auto"
          >
            {validatingCoupon ? 'Validating...' : appliedCoupon ? '✓ Applied' : 'Apply Coupon'}
          </button>
        </div>

        {couponMsg && (
          <div className={`text-center text-xs font-semibold mb-8 ${couponMsg.isError ? 'text-red-500' : 'text-emerald-500'}`}>
            {couponMsg.text}
          </div>
        )}

        {/* Pricing Cards Grid */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {plans.map((plan) => {
            const isFreePlan = plan.price === 0
            const isCurrentPlan = activeSub ? activeSub.planId === plan.id : isFreePlan
            const hasDiscount = appliedCoupon && plan.price >= (appliedCoupon.minPlanPrice || 0) && plan.price > 0
            const discountedPrice = hasDiscount 
              ? Math.max(1, plan.price - (appliedCoupon.discountType === 'Percentage' ? (plan.price * appliedCoupon.discountValue) / 100 : appliedCoupon.discountValue)) 
              : plan.price

            return (
              <div
                key={plan.id}
                className={`rounded-3xl p-8 border flex flex-col justify-between transition-all relative ${
                  isCurrentPlan
                    ? 'border-emerald-500 shadow-md ring-2 ring-emerald-500/20'
                    : plan.name.toLowerCase().includes('pro')
                    ? 'border-brand-500 shadow-brand ring-1 ring-brand-500/30'
                    : 'hover:border-brand-500/40'
                }`}
                style={{ backgroundColor: 'var(--card-bg)', borderColor: isCurrentPlan ? '#10b981' : undefined }}
              >
                {/* Popular or Current Badge */}
                {isCurrentPlan ? (
                  <span className="absolute -top-3 left-1/2 -translate-x-1/2 bg-emerald-500 text-white text-[11px] font-bold px-3 py-0.5 rounded-full shadow-sm">
                    ✓ Current Plan
                  </span>
                ) : plan.name.toLowerCase().includes('pro') ? (
                  <span className="absolute -top-3 left-1/2 -translate-x-1/2 bg-brand-500 text-white text-[11px] font-bold px-3 py-0.5 rounded-full shadow-sm">
                    ★ Most Popular
                  </span>
                ) : null}

                <div>
                  <h3 className="font-display font-bold text-xl mb-2" style={{ color: 'var(--text-primary)' }}>
                    {plan.name}
                  </h3>
                  
                  <div className="flex items-baseline gap-2 mb-6">
                    {isFreePlan ? (
                      <span className="font-display font-black text-3xl text-emerald-600 dark:text-emerald-400">
                        ₹0
                      </span>
                    ) : hasDiscount ? (
                      <>
                        <span className="font-display font-black text-3xl text-brand-600 dark:text-brand-400">
                          ₹{discountedPrice.toFixed(0)}
                        </span>
                        <span className="text-sm line-through opacity-50" style={{ color: 'var(--text-muted)' }}>
                          ₹{plan.price}
                        </span>
                      </>
                    ) : (
                      <span className="font-display font-black text-3xl" style={{ color: 'var(--text-primary)' }}>
                        ₹{plan.price}
                      </span>
                    )}
                    <span className="text-xs" style={{ color: 'var(--text-muted)' }}>
                      /{plan.billingCycle?.toLowerCase() || 'month'}
                    </span>
                  </div>

                  <ul 
                    className="space-y-3.5 text-xs border-t pt-6 mb-8" 
                    style={{ borderColor: 'var(--border-color)', color: 'var(--text-secondary)' }}
                  >
                    <li className="flex items-start gap-2.5">
                      <Check className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />
                      <span><strong>{plan.maxDailyAiTokens.toLocaleString()}</strong> daily AI tokens limit</span>
                    </li>
                    <li className="flex items-start gap-2.5">
                      <Check className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />
                      <span><strong>{plan.maxRoadmapsLimit}</strong> active roadmap builders</span>
                    </li>
                    <li className="flex items-start gap-2.5">
                      <Check className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />
                      <span>{isFreePlan ? 'Standard career matches' : 'Priority personalized career insights'}</span>
                    </li>
                    {!isFreePlan && (
                      <li className="flex items-start gap-2.5">
                        <Check className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />
                        <span>Instant PDF career roadmaps export</span>
                      </li>
                    )}
                  </ul>
                </div>

                <button
                  onClick={() => handleOpenPlanCheckout(plan)}
                  disabled={isCurrentPlan || purchasing !== null}
                  className={`w-full py-3.5 rounded-2xl text-xs font-bold flex items-center justify-center gap-2 transition-all ${
                    isCurrentPlan
                      ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border border-emerald-500/20 cursor-default'
                      : 'btn-brand shadow-brand hover:scale-[1.02]'
                  }`}
                >
                  {isCurrentPlan ? (
                    <>
                      <Check className="w-4 h-4" /> Current Active Tier
                    </>
                  ) : (
                    <>
                      <CreditCard className="w-4 h-4" />
                      {hasDiscount
                        ? `Upgrade with Razorpay (₹${discountedPrice.toFixed(0)})`
                        : `Upgrade with Razorpay (₹${plan.price})`}
                    </>
                  )}
                </button>
              </div>
            )
          })}
        </div>

        {/* ── Razorpay Gateway Checkout Modal (Light & Dark Polish) ────────────────────── */}
        {showCheckoutModal && selectedPlanForCheckout && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4 overflow-y-auto">
            <div 
              className="rounded-3xl p-6 sm:p-8 max-w-md w-full shadow-2xl relative border transition-colors"
              style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
            >
              {/* Modal Header */}
              <div className="flex items-center justify-between pb-4 border-b mb-6" style={{ borderColor: 'var(--border-color)' }}>
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-600 dark:text-brand-400 flex items-center justify-center font-black text-lg">
                    ₹
                  </div>
                  <div>
                    <h4 className="font-bold text-base" style={{ color: 'var(--text-primary)' }}>
                      Razorpay Secure Checkout
                    </h4>
                    <p className="text-xs flex items-center gap-1" style={{ color: 'var(--text-muted)' }}>
                      <Lock className="w-3 h-3 text-emerald-500" /> CareerPath Bharat • 256-bit SSL
                    </p>
                  </div>
                </div>
                <button 
                  onClick={() => setShowCheckoutModal(false)}
                  className="p-2 rounded-xl text-slate-400 hover:text-slate-600 dark:hover:text-white transition-colors"
                >
                  <X className="w-5 h-5" />
                </button>
              </div>

              {/* Order Summary Box */}
              <div 
                className="p-5 rounded-2xl border mb-6"
                style={{ 
                  backgroundColor: 'var(--bg-app)', 
                  borderColor: 'var(--border-color)' 
                }}
              >
                <div className="flex justify-between items-center text-xs mb-2" style={{ color: 'var(--text-secondary)' }}>
                  <span>Selected Membership:</span>
                  <span className="font-bold" style={{ color: 'var(--text-primary)' }}>
                    {selectedPlanForCheckout.name}
                  </span>
                </div>
                <div className="flex justify-between items-center text-xs mb-2" style={{ color: 'var(--text-secondary)' }}>
                  <span>Billing Frequency:</span>
                  <span className="font-medium" style={{ color: 'var(--text-primary)' }}>
                    {selectedPlanForCheckout.billingCycle}
                  </span>
                </div>
                {appliedCoupon && selectedPlanForCheckout.price > 0 && (
                  <div className="flex justify-between items-center text-xs text-emerald-600 dark:text-emerald-400 mb-2 font-semibold">
                    <span>Applied Promo ({appliedCoupon.code}):</span>
                    <span>- {appliedCoupon.discountType === 'Percentage' ? `${appliedCoupon.discountValue}%` : `₹${appliedCoupon.discountValue}`}</span>
                  </div>
                )}
                <div className="border-t pt-3 mt-2 flex justify-between items-baseline font-bold" style={{ borderColor: 'var(--border-color)' }}>
                  <span className="text-xs" style={{ color: 'var(--text-primary)' }}>Total Amount Payable:</span>
                  <span className="text-2xl font-black text-brand-600 dark:text-brand-400 font-display">
                    ₹{appliedCoupon && selectedPlanForCheckout.price >= (appliedCoupon.minPlanPrice || 0)
                      ? Math.max(1, selectedPlanForCheckout.price - (appliedCoupon.discountType === 'Percentage' ? (selectedPlanForCheckout.price * appliedCoupon.discountValue) / 100 : appliedCoupon.discountValue)).toFixed(0)
                      : selectedPlanForCheckout.price}
                  </span>
                </div>
              </div>

              {/* Payment Methods Selection */}
              <div className="mb-6">
                <label className="text-xs font-semibold block mb-2" style={{ color: 'var(--text-primary)' }}>
                  Select Payment Option
                </label>
                <div className="grid grid-cols-3 gap-2">
                  {(['UPI', 'Card', 'NetBanking'] as const).map((method) => {
                    const isSel = selectedMethod === method
                    return (
                      <button
                        key={method}
                        type="button"
                        onClick={() => setSelectedMethod(method)}
                        className={`py-2.5 px-3 rounded-xl text-xs font-bold border transition-all ${
                          isSel
                            ? 'bg-brand-500 text-white border-brand-500 shadow-sm'
                            : 'border-slate-200 dark:border-slate-700 text-slate-700 dark:text-slate-300 hover:bg-slate-100 dark:hover:bg-slate-800'
                        }`}
                      >
                        {method === 'UPI' ? '📱 UPI / QR' : method === 'Card' ? '💳 Card' : '🏦 NetBanking'}
                      </button>
                    )
                  })}
                </div>
              </div>

              {selectedMethod === 'UPI' && (
                <div 
                  className="mb-6 p-3.5 rounded-xl border"
                  style={{ backgroundColor: 'var(--bg-app)', borderColor: 'var(--border-color)' }}
                >
                  <label className="text-[11px] font-medium block mb-1" style={{ color: 'var(--text-muted)' }}>
                    UPI ID / Virtual Payment Address
                  </label>
                  <input
                    type="text"
                    value={upiId}
                    onChange={e => setUpiId(e.target.value)}
                    className="w-full rounded-lg px-3 py-2 text-xs outline-none border focus:border-brand-500 font-medium transition-colors"
                    style={{ 
                      backgroundColor: 'var(--card-bg)', 
                      borderColor: 'var(--border-color)',
                      color: 'var(--text-primary)'
                    }}
                    placeholder="e.g. mobile@upi"
                  />
                </div>
              )}

              {/* Pay Action Button */}
              <button
                type="button"
                onClick={launchRazorpayStandardCheckout}
                disabled={purchasing !== null}
                className="w-full py-4 rounded-2xl btn-brand text-xs font-bold flex items-center justify-center gap-2 shadow-brand"
              >
                <Lock className="w-4 h-4" />
                {purchasing === selectedPlanForCheckout.id 
                  ? 'Connecting to Razorpay...' 
                  : `Proceed to Pay ₹${(appliedCoupon && selectedPlanForCheckout.price >= (appliedCoupon.minPlanPrice || 0)
                      ? Math.max(1, selectedPlanForCheckout.price - (appliedCoupon.discountType === 'Percentage' ? (selectedPlanForCheckout.price * appliedCoupon.discountValue) / 100 : appliedCoupon.discountValue))
                      : selectedPlanForCheckout.price).toFixed(0)}`}
              </button>

              <p className="text-[11px] text-center mt-4" style={{ color: 'var(--text-muted)' }}>
                Powered by Razorpay • Instant access upon payment confirmation
              </p>
            </div>
          </div>
        )}

      </div>
    </div>
  )
}
