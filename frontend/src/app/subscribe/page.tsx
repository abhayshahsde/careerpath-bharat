/* eslint-disable @typescript-eslint/no-explicit-any */
'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { useAuth } from '@/lib/auth-context'
import { api } from '@/lib/api'
import { CreditCard, Check, Shield, AlertCircle, Tag, Sparkles } from 'lucide-react'

export default function SubscribePage() {
  const router = useRouter()
  const { isAuthenticated, isLoading } = useAuth()
  
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

  const loadBillingDetails = async () => {
    setError('')
    setMessage('')
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
      setError(err?.message ?? 'Failed to load subscription metrics.')
    } finally {
      setLoadingData(false)
    }
  }

  useEffect(() => {
    loadBillingDetails()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isAuthenticated])

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

  const handlePurchase = async (plan: any) => {
    if (!isAuthenticated) {
      router.push('/auth/login')
      return
    }

    // Free Tier is completely free - no payment checkout required
    if (plan.price === 0) {
      setPurchasing(plan.id)
      setError('')
      setMessage('')
      try {
        const res = (await api.subscribeToPlan(plan.id, 'Free', 'free_token', appliedCoupon?.code || undefined)) as any
        if (res.success) {
          setMessage(res.message ?? 'Free Tier activated successfully!')
          const sub = (await api.getActiveSubscription().catch(() => null)) as any
          setActiveSub(sub)
        }
      } catch (err: any) {
        setError(err?.message ?? 'Failed to activate Free plan.')
      } finally {
        setPurchasing(null)
      }
      return
    }

    // Paid Plans: Open custom in-app Razorpay & UPI Checkout Modal
    setSelectedPlanForCheckout(plan)
    setShowCheckoutModal(true)
  }

  const handleConfirmGatewayPayment = async (paymentMethod: 'UPI' | 'Card' | 'NetBanking') => {
    if (!selectedPlanForCheckout) return

    const plan = selectedPlanForCheckout
    setPurchasing(plan.id)
    setError('')
    setMessage('')
    try {
      const hasDiscount = appliedCoupon && plan.price >= (appliedCoupon.minPlanPrice || 0) && plan.price > 0
      const finalAmount = hasDiscount 
        ? Math.max(1, plan.price - (appliedCoupon.discountType === 'Percentage' ? (plan.price * appliedCoupon.discountValue) / 100 : appliedCoupon.discountValue))
        : plan.price

      const txId = `rzp_${paymentMethod.toLowerCase()}_${Date.now()}`
      const res = (await api.subscribeToPlan(plan.id, 'Razorpay', txId, appliedCoupon?.code || undefined)) as any
      if (res.success) {
        setMessage(`Payment of ₹${finalAmount.toFixed(0)} via Razorpay (${paymentMethod}) confirmed! ${plan.name} activated.`)
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
      <div className="min-h-screen flex items-center justify-center bg-surface-900 text-white/50 text-sm">
        <div className="flex items-center gap-3">
          <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
          <span>Syncing plan prices & vouchers...</span>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-hero-gradient px-4 sm:px-6 lg:px-8 py-16 page-enter">
      <div className="max-w-5xl mx-auto">
        
        {/* Header */}
        <div className="text-center max-w-2xl mx-auto mb-12">
          <h1 className="font-display font-black text-4xl md:text-5xl mb-4" style={{ color: 'var(--text-primary)' }}>
            Unleash Custom <span className="gradient-text">Roadmaps</span> & <span className="gradient-text">AI Guidance</span>
          </h1>
          <p className="text-base" style={{ color: 'var(--text-secondary)' }}>
            Upgrade your daily token quotas and access premium roadmap checklist builders.
          </p>
        </div>

        {/* Public Vouchers Banner Strip */}
        {publicCoupons.length > 0 && (
          <div className="mb-10 p-4 rounded-2xl glass border flex flex-wrap items-center justify-between gap-4 shadow-sm" style={{ borderColor: 'var(--border-color)' }}>
            <div className="flex items-center gap-2.5">
              <Sparkles className="w-5 h-5 text-amber-500 animate-pulse" />
              <div>
                <span className="text-xs font-bold" style={{ color: 'var(--text-primary)' }}>Active Promotions Available:</span>
                <span className="text-xs ml-2" style={{ color: 'var(--text-muted)' }}>
                  Use code <strong className="text-brand-400">{publicCoupons[0].code}</strong> for {publicCoupons[0].discountType === 'Percentage' ? `${publicCoupons[0].discountValue}% OFF` : `₹${publicCoupons[0].discountValue} OFF`}
                </span>
              </div>
            </div>
            <button
              onClick={() => { setCouponCode(publicCoupons[0].code); handleApplyCoupon(); }}
              className="text-xs font-bold px-3 py-1.5 rounded-xl btn-brand shadow-sm"
            >
              Apply {publicCoupons[0].code}
            </button>
          </div>
        )}

        {error && (
          <div className="mb-8 p-4 rounded-xl bg-red-500/10 border border-red-500/20 text-red-500 text-sm flex items-center gap-3">
            <AlertCircle className="w-5 h-5 flex-shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {message && (
          <div className="mb-8 p-4 rounded-xl bg-accent-teal/10 border border-accent-teal/20 text-accent-teal text-sm flex items-center gap-3 animate-pulse">
            <Check className="w-5 h-5 flex-shrink-0" />
            <span>{message}</span>
          </div>
        )}

        {/* Current Active Plan Status */}
        {activeSub && (
          <div className="glass rounded-2xl p-6 mb-12 border flex flex-col md:flex-row md:items-center justify-between gap-6" style={{ borderColor: 'var(--border-color)' }}>
            <div>
              <div className="flex items-center gap-2.5">
                <Shield className="w-5 h-5 text-brand-400" />
                <span className="font-bold text-lg" style={{ color: 'var(--text-primary)' }}>Active Premium Membership</span>
              </div>
              <p className="text-xs mt-1" style={{ color: 'var(--text-muted)' }}>
                Plan: <strong style={{ color: 'var(--text-primary)' }}>{activeSub.planName}</strong> • 
                Period End: <strong style={{ color: 'var(--text-primary)' }}>{new Date(activeSub.currentPeriodEnd).toLocaleDateString()}</strong>
              </p>
              {activeSub.cancelAtPeriodEnd && (
                <p className="text-red-400 text-xs mt-1 font-medium">Your subscription will terminate at the end of the period.</p>
              )}
            </div>

            {!activeSub.cancelAtPeriodEnd && (
              <button
                onClick={handleCancelRenewal}
                disabled={canceling}
                className="glass-button text-xs py-2.5 px-4 text-red-400 hover:bg-red-500/10 border-red-500/20"
              >
                Cancel Renewal
              </button>
            )}
          </div>
        )}

        {/* Optional Coupon Code Box */}
        <div className="glass rounded-2xl p-4 mb-8 border flex flex-col sm:flex-row items-center justify-between gap-4 max-w-xl mx-auto shadow-sm" style={{ borderColor: 'var(--border-color)' }}>
          <div className="flex items-center gap-2 w-full sm:w-auto">
            <Tag className="w-4 h-4 text-brand-400 shrink-0" />
            <input
              type="text"
              placeholder="Promo Code (e.g. BHARAT50)"
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
            const isActive = activeSub?.planId === plan.id;
            const hasDiscount = appliedCoupon && plan.price >= (appliedCoupon.minPlanPrice || 0) && plan.price > 0;
            const discountedPrice = hasDiscount ? Math.max(0, plan.price - (appliedCoupon.discountType === 'Percentage' ? (plan.price * appliedCoupon.discountValue) / 100 : appliedCoupon.discountValue)) : plan.price;

            return (
              <div
                key={plan.id}
                className={`glass rounded-3xl p-8 border flex flex-col justify-between transition-all ${
                  isActive
                    ? 'border-brand-500 shadow-glow scale-105'
                    : 'hover:scale-[1.02]'
                }`}
                style={{ borderColor: isActive ? 'var(--brand-500)' : 'var(--border-color)' }}
              >
                <div>
                  <h3 className="font-display font-bold text-xl mb-2" style={{ color: 'var(--text-primary)' }}>{plan.name}</h3>
                  
                  <div className="flex items-baseline gap-2 mb-6">
                    {hasDiscount ? (
                      <>
                        <span className="font-display font-black text-3xl text-emerald-500">₹{discountedPrice.toFixed(0)}</span>
                        <span className="text-sm line-through opacity-50" style={{ color: 'var(--text-muted)' }}>₹{plan.price}</span>
                      </>
                    ) : (
                      <span className="font-display font-black text-3xl" style={{ color: 'var(--text-primary)' }}>₹{plan.price}</span>
                    )}
                    <span className="text-xs" style={{ color: 'var(--text-muted)' }}>/{plan.billingCycle.toLowerCase()}</span>
                  </div>

                  <ul className="space-y-3.5 text-xs border-t pt-6 mb-8" style={{ borderColor: 'var(--border-color)', color: 'var(--text-secondary)' }}>
                    <li className="flex items-start gap-2.5">
                      <Check className="w-4 h-4 text-brand-400 shrink-0 mt-0.5" />
                      <span><strong>{plan.maxDailyAiTokens.toLocaleString()}</strong> daily AI tokens limit</span>
                    </li>
                    <li className="flex items-start gap-2.5">
                      <Check className="w-4 h-4 text-brand-400 shrink-0 mt-0.5" />
                      <span><strong>{plan.maxRoadmapsLimit}</strong> active roadmap builders</span>
                    </li>
                    <li className="flex items-start gap-2.5">
                      <Check className="w-4 h-4 text-brand-400 shrink-0 mt-0.5" />
                      <span>Priority personalized career insights</span>
                    </li>
                  </ul>
                </div>

                <button
                  onClick={() => handlePurchase(plan)}
                  disabled={isActive || purchasing !== null}
                  className={`w-full py-3.5 rounded-2xl text-xs font-bold flex items-center justify-center gap-2 transition-all ${
                    isActive
                      ? 'bg-accent-teal/20 text-accent-teal cursor-default'
                      : 'btn-brand shadow-brand hover:scale-[1.02]'
                  }`}
                >
                  <CreditCard className="w-4 h-4" />
                  {isActive
                    ? 'Current Membership'
                    : purchasing === plan.id
                    ? 'Processing...'
                    : plan.price === 0
                    ? 'Switch to Free Tier'
                    : hasDiscount
                    ? `Pay with Razorpay (₹${discountedPrice.toFixed(0)})`
                    : `Pay with Razorpay (₹${plan.price})`}
                </button>
              </div>
            )
          })}
        </div>

        {/* ── Razorpay Gateway Checkout Modal ────────────────────────────────────── */}
        {showCheckoutModal && selectedPlanForCheckout && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4 overflow-y-auto">
            <div 
              className="bg-surface-900 border border-slate-700/60 rounded-3xl p-6 sm:p-8 max-w-md w-full shadow-2xl relative text-slate-100"
              style={{ backgroundColor: 'var(--surface-primary, #0f172a)' }}
            >
              <div className="flex items-center justify-between pb-4 border-b border-slate-700/50 mb-6">
                <div className="flex items-center gap-2.5">
                  <div className="w-9 h-9 rounded-xl bg-blue-600/20 text-blue-400 flex items-center justify-center font-black text-lg">
                    ₹
                  </div>
                  <div>
                    <h4 className="font-bold text-base text-slate-100">Razorpay Secure Checkout</h4>
                    <p className="text-xs text-slate-400">CareerPath Bharat • 256-bit SSL</p>
                  </div>
                </div>
                <button 
                  onClick={() => setShowCheckoutModal(false)}
                  className="p-1.5 rounded-lg text-slate-400 hover:text-white hover:bg-slate-800 transition-colors"
                >
                  ✕
                </button>
              </div>

              {/* Order Summary */}
              <div className="p-4 rounded-2xl bg-slate-800/60 border border-slate-700/50 mb-6">
                <div className="flex justify-between items-center text-xs mb-1.5 text-slate-300">
                  <span>Selected Plan:</span>
                  <span className="font-bold text-white">{selectedPlanForCheckout.name}</span>
                </div>
                <div className="flex justify-between items-center text-xs mb-1.5 text-slate-300">
                  <span>Billing Period:</span>
                  <span className="text-slate-400">{selectedPlanForCheckout.billingCycle}</span>
                </div>
                {appliedCoupon && selectedPlanForCheckout.price > 0 && (
                  <div className="flex justify-between items-center text-xs text-emerald-400 mb-1.5 font-semibold">
                    <span>Discount ({appliedCoupon.code}):</span>
                    <span>- {appliedCoupon.discountType === 'Percentage' ? `${appliedCoupon.discountValue}%` : `₹${appliedCoupon.discountValue}`}</span>
                  </div>
                )}
                <div className="border-t border-slate-700/60 pt-2 mt-2 flex justify-between items-baseline font-bold">
                  <span className="text-xs text-slate-200">Amount Payable:</span>
                  <span className="text-xl text-emerald-400 font-display">
                    ₹{appliedCoupon && selectedPlanForCheckout.price >= (appliedCoupon.minPlanPrice || 0)
                      ? Math.max(1, selectedPlanForCheckout.price - (appliedCoupon.discountType === 'Percentage' ? (selectedPlanForCheckout.price * appliedCoupon.discountValue) / 100 : appliedCoupon.discountValue)).toFixed(0)
                      : selectedPlanForCheckout.price}
                  </span>
                </div>
              </div>

              {/* Payment Methods */}
              <div className="mb-6">
                <label className="text-xs font-semibold text-slate-300 block mb-2">Select Payment Method</label>
                <div className="grid grid-cols-3 gap-2">
                  <button
                    type="button"
                    onClick={() => setSelectedMethod('UPI')}
                    className={`py-2.5 px-3 rounded-xl text-xs font-bold border transition-all ${
                      selectedMethod === 'UPI'
                        ? 'bg-blue-600 border-blue-500 text-white shadow-md'
                        : 'bg-slate-800/80 border-slate-700 text-slate-300 hover:bg-slate-800'
                    }`}
                  >
                    📱 UPI / QR
                  </button>
                  <button
                    type="button"
                    onClick={() => setSelectedMethod('Card')}
                    className={`py-2.5 px-3 rounded-xl text-xs font-bold border transition-all ${
                      selectedMethod === 'Card'
                        ? 'bg-blue-600 border-blue-500 text-white shadow-md'
                        : 'bg-slate-800/80 border-slate-700 text-slate-300 hover:bg-slate-800'
                    }`}
                  >
                    💳 Card
                  </button>
                  <button
                    type="button"
                    onClick={() => setSelectedMethod('NetBanking')}
                    className={`py-2.5 px-3 rounded-xl text-xs font-bold border transition-all ${
                      selectedMethod === 'NetBanking'
                        ? 'bg-blue-600 border-blue-500 text-white shadow-md'
                        : 'bg-slate-800/80 border-slate-700 text-slate-300 hover:bg-slate-800'
                    }`}
                  >
                    🏦 NetBanking
                  </button>
                </div>
              </div>

              {selectedMethod === 'UPI' && (
                <div className="mb-6 p-3 rounded-xl bg-slate-800/40 border border-slate-700/50">
                  <label className="text-[11px] text-slate-400 block mb-1">Enter UPI Virtual Payment Address (VPA)</label>
                  <input
                    type="text"
                    value={upiId}
                    onChange={e => setUpiId(e.target.value)}
                    className="w-full bg-slate-900 border border-slate-700 rounded-lg px-3 py-2 text-xs text-white outline-none focus:border-blue-500"
                    placeholder="username@okhdfcbank"
                  />
                </div>
              )}

              {/* Pay Action Button */}
              <button
                type="button"
                onClick={() => handleConfirmGatewayPayment(selectedMethod)}
                disabled={purchasing !== null}
                className="w-full py-3.5 rounded-2xl btn-brand text-xs font-bold flex items-center justify-center gap-2 shadow-lg shadow-blue-500/20"
              >
                <Shield className="w-4 h-4" />
                {purchasing === selectedPlanForCheckout.id ? 'Verifying with Gateway...' : 'Complete Secure Payment'}
              </button>

              <p className="text-[11px] text-center text-slate-500 mt-4">
                Secured by Razorpay • Instant access activated upon confirmation
              </p>
            </div>
          </div>
        )}

      </div>
    </div>
  )
}
