/* eslint-disable @typescript-eslint/no-explicit-any */
'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { useAuth } from '@/lib/auth-context'
import { api } from '@/lib/api'
import { 
  Shield, Upload, FileText, CheckCircle, AlertTriangle, Eye, RefreshCw, XCircle, 
  BookOpen, Globe, Users, TrendingUp, DollarSign, Compass, Tag, Plus 
} from 'lucide-react'

export default function AdminPage() {
  const router = useRouter()
  const { user, isAuthenticated, isLoading } = useAuth()
  const [activeTab, setActiveTab] = useState<'overview' | 'users' | 'coupons' | 'imports' | 'knowledge' | 'editorial'>('overview')

  // Analytics & Users Data States
  const [overview, setOverview] = useState<any | null>(null)
  const [usersList, setUsersList] = useState<any[]>([])
  const [userSearch, setUserSearch] = useState('')
  const [couponsList, setCouponsList] = useState<any[]>([])

  // New Coupon Form Modal
  const [showNewCoupon, setShowNewCoupon] = useState(false)
  const [newCouponCode, setNewCouponCode] = useState('')
  const [newCouponDesc, setNewCouponDesc] = useState('')
  const [newCouponType, setNewCouponType] = useState<'Percentage' | 'FixedAmount'>('Percentage')
  const [newCouponValue, setNewCouponValue] = useState(20)
  const [newCouponMinPrice, setNewCouponMinPrice] = useState(100)
  const [newCouponMaxRedemptions, setNewCouponMaxRedemptions] = useState(100)
  const [newCouponTargetUser, setNewCouponTargetUser] = useState('')
  const [newCouponIsPublic, setNewCouponIsPublic] = useState(true)

  // Catalog Data States
  const [importJobs, setImportJobs] = useState<any[]>([])
  const [documents, setDocuments] = useState<any[]>([])
  const [articles, setArticles] = useState<any[]>([])
  const [selectedJob, setSelectedJob] = useState<any | null>(null)
  const [selectedDoc, setSelectedDoc] = useState<any | null>(null)
  const [selectedArticle, setSelectedArticle] = useState<any | null>(null)

  // Loading/Status
  const [submitting, setSubmitting] = useState(false)
  const [actionNotes, setActionNotes] = useState('')
  const [error, setError] = useState('')
  const [successMsg, setSuccessMsg] = useState('')

  // 1. Authorization guard
  useEffect(() => {
    if (!isLoading && (!isAuthenticated || !user?.roles?.includes('Admin'))) {
      router.push('/')
    }
  }, [isLoading, isAuthenticated, user, router])

  // 2. Fetch lists
  const loadData = async () => {
    setError('')
    setSuccessMsg('')
    try {
      if (activeTab === 'overview') {
        const stats = await api.getAdminOverview()
        setOverview(stats)
      } else if (activeTab === 'users') {
        const uList = await api.getAdminUsers({ search: userSearch })
        setUsersList(uList || [])
      } else if (activeTab === 'coupons') {
        const cList = await api.getAdminCoupons()
        setCouponsList(cList || [])
      } else if (activeTab === 'imports') {
        const jobs = await api.getImportJobs()
        setImportJobs(jobs || [])
      } else if (activeTab === 'knowledge') {
        const docs = await api.getDocuments()
        setDocuments(docs || [])
      } else {
        const arts = await api.getEditorialArticles()
        setArticles(arts || [])
      }
    } catch (err: any) {
      console.error('Admin portal data load error:', err)
      setError(err?.message ?? 'Failed to load dashboard data.')
    }
  }

  useEffect(() => {
    if (isAuthenticated && user?.roles?.includes('Admin')) {
      loadData()
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeTab, isAuthenticated, user])

  // User Actions
  const handleToggleUserSuspension = async (targetUserId: string, currentStatus: boolean) => {
    try {
      await api.toggleUserSuspension(targetUserId, !currentStatus)
      setSuccessMsg(`User status updated to ${!currentStatus ? 'Active' : 'Suspended'}`)
      loadData()
    } catch (err: any) {
      setError(err?.message ?? 'Failed to update user status.')
    }
  }

  // Coupon Actions
  const handleCreateCoupon = async (e: React.FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    setError('')
    try {
      await api.createAdminCoupon({
        code: newCouponCode,
        description: newCouponDesc,
        discountType: newCouponType,
        discountValue: Number(newCouponValue),
        minPlanPrice: Number(newCouponMinPrice),
        maxRedemptions: Number(newCouponMaxRedemptions),
        isActive: true,
        isVisiblePublicly: newCouponIsPublic,
        targetUserId: newCouponTargetUser || undefined,
      })
      setShowNewCoupon(false)
      setNewCouponCode('')
      setNewCouponDesc('')
      setSuccessMsg('Promotional coupon created successfully!')
      loadData()
    } catch (err: any) {
      setError(err?.message ?? 'Failed to create coupon.')
    } finally {
      setSubmitting(false)
    }
  }

  const handleToggleCoupon = async (couponId: string, isActive?: boolean, isVisiblePublicly?: boolean) => {
    try {
      await api.toggleAdminCoupon(couponId, { isActive, isVisiblePublicly })
      setSuccessMsg('Coupon settings updated!')
      loadData()
    } catch (err: any) {
      setError(err?.message ?? 'Failed to update coupon.')
    }
  }

  // 3. View detail details
  const viewJobDetails = async (jobId: string) => {
    try {
      const detail = await api.getImportJobDetail(jobId)
      setSelectedJob(detail)
    } catch (err: any) {
      setError(err?.message ?? 'Failed to fetch job detail.')
    }
  }

  const viewDocDetails = async (docId: string) => {
    try {
      const detail = await api.getDocumentDetail(docId)
      setSelectedDoc(detail)
    } catch (err: any) {
      setError(err?.message ?? 'Failed to fetch document detail.')
    }
  }

  const viewArticleDetails = async (articleId: string) => {
    try {
      const detail = await api.getEditorialArticleDetail(articleId)
      setSelectedArticle(detail)
    } catch (err: any) {
      setError(err?.message ?? 'Failed to fetch article detail.')
    }
  }

  // 4. Submit actions
  const handleImportReview = async (jobId: string, approved: boolean) => {
    setSubmitting(true)
    try {
      await api.submitImportReview(jobId, approved, actionNotes || 'Admin decision.')
      setActionNotes('')
      setSelectedJob(null)
      loadData()
    } catch (err: any) {
      setError(err?.message ?? 'Review submit failed.')
    } finally {
      setSubmitting(false)
    }
  }

  const handleDocumentReview = async (docId: string, approved: boolean) => {
    setSubmitting(true)
    try {
      await api.submitDocumentReview(docId, approved, actionNotes || 'Document check.')
      setActionNotes('')
      setSelectedDoc(null)
      loadData()
    } catch (err: any) {
      setError(err?.message ?? 'Document review submit failed.')
    } finally {
      setSubmitting(false)
    }
  }

  const handleArticleReviewDecision = async (articleId: string, reviewId: number, approved: boolean) => {
    setSubmitting(true)
    try {
      const decision = approved ? 'Approve' : 'RequestChanges'
      await api.submitEditorialReviewDecision(articleId, reviewId, decision, actionNotes || 'Editorial decision.')
      setActionNotes('')
      setSelectedArticle(null)
      loadData()
    } catch (err: any) {
      setError(err?.message ?? 'Article review failed.')
    } finally {
      setSubmitting(false)
    }
  }

  const handlePublishArticle = async (articleId: string) => {
    setSubmitting(true)
    try {
      await api.publishEditorialArticle(articleId)
      setSelectedArticle(null)
      loadData()
    } catch (err: any) {
      setError(err?.message ?? 'Publishing failed.')
    } finally {
      setSubmitting(false)
    }
  }

  if (isLoading || !isAuthenticated || !user?.roles?.includes('Admin')) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-surface-900 text-white/50 text-sm">
        <div className="flex items-center gap-3">
          <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
          <span>Verifying administrator permissions...</span>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-hero-gradient px-4 sm:px-6 lg:px-8 py-12 page-enter">
      <div className="max-w-7xl mx-auto">

        {/* Header */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8 border-b pb-6" style={{ borderColor: 'var(--border-color)' }}>
          <div className="flex items-center gap-3.5">
            <div className="w-12 h-12 rounded-2xl bg-brand-gradient flex items-center justify-center shadow-brand">
              <Shield className="w-6 h-6 text-white" />
            </div>
            <div>
              <h1 className="font-display font-black text-3xl" style={{ color: 'var(--text-primary)' }}>Super Admin Control Hub</h1>
              <p className="text-xs mt-0.5" style={{ color: 'var(--text-muted)' }}>Real-time user analytics, subscriptions, promo vouchers, and content moderation</p>
            </div>
          </div>
          <button onClick={loadData} className="glass-button text-xs py-2 px-3.5 flex items-center gap-1.5 self-start md:self-auto border shadow-sm" style={{ borderColor: 'var(--border-color)' }}>
            <RefreshCw className="w-3.5 h-3.5 text-brand-500" /> Reload Data
          </button>
        </div>

        {error && (
          <div className="mb-6 p-4 rounded-2xl bg-red-500/10 border border-red-500/20 text-red-500 text-xs font-semibold flex items-center gap-3">
            <AlertTriangle className="w-5 h-5 flex-shrink-0" />
            <span>{error}</span>
          </div>
        )}

        {successMsg && (
          <div className="mb-6 p-4 rounded-2xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-500 text-xs font-semibold flex items-center gap-3">
            <CheckCircle className="w-5 h-5 flex-shrink-0" />
            <span>{successMsg}</span>
          </div>
        )}

        {/* Tab Selector */}
        <div className="flex gap-2 mb-8 glass p-1.5 rounded-2xl w-fit border shadow-sm flex-wrap" style={{ borderColor: 'var(--border-color)' }}>
          <button
            onClick={() => { setActiveTab('overview'); }}
            className={`px-4 py-2.5 rounded-xl text-xs font-bold transition-all ${activeTab === 'overview' ? 'btn-brand shadow-brand' : 'text-slate-500 hover:text-slate-900 dark:hover:text-white'}`}
          >
            <span className="flex items-center gap-1.5">
              <TrendingUp className="w-4 h-4" /> Live Overview
            </span>
          </button>
          <button
            onClick={() => { setActiveTab('users'); }}
            className={`px-4 py-2.5 rounded-xl text-xs font-bold transition-all ${activeTab === 'users' ? 'btn-brand shadow-brand' : 'text-slate-500 hover:text-slate-900 dark:hover:text-white'}`}
          >
            <span className="flex items-center gap-1.5">
              <Users className="w-4 h-4" /> Students & Users
            </span>
          </button>
          <button
            onClick={() => { setActiveTab('coupons'); }}
            className={`px-4 py-2.5 rounded-xl text-xs font-bold transition-all ${activeTab === 'coupons' ? 'btn-brand shadow-brand' : 'text-slate-500 hover:text-slate-900 dark:hover:text-white'}`}
          >
            <span className="flex items-center gap-1.5">
              <Tag className="w-4 h-4" /> Coupons & Vouchers
            </span>
          </button>
          <button
            onClick={() => { setActiveTab('imports'); setSelectedJob(null); setSelectedDoc(null); setSelectedArticle(null); }}
            className={`px-4 py-2.5 rounded-xl text-xs font-bold transition-all ${activeTab === 'imports' ? 'btn-brand shadow-brand' : 'text-slate-500 hover:text-slate-900 dark:hover:text-white'}`}
          >
            <span className="flex items-center gap-1.5">
              <Upload className="w-4 h-4" /> Bulk Imports
            </span>
          </button>
          <button
            onClick={() => { setActiveTab('knowledge'); setSelectedJob(null); setSelectedDoc(null); setSelectedArticle(null); }}
            className={`px-4 py-2.5 rounded-xl text-xs font-bold transition-all ${activeTab === 'knowledge' ? 'btn-brand shadow-brand' : 'text-slate-500 hover:text-slate-900 dark:hover:text-white'}`}
          >
            <span className="flex items-center gap-1.5">
              <FileText className="w-4 h-4" /> Knowledge Chunks
            </span>
          </button>
          <button
            onClick={() => { setActiveTab('editorial'); setSelectedJob(null); setSelectedDoc(null); setSelectedArticle(null); }}
            className={`px-4 py-2.5 rounded-xl text-xs font-bold transition-all ${activeTab === 'editorial' ? 'btn-brand shadow-brand' : 'text-slate-500 hover:text-slate-900 dark:hover:text-white'}`}
          >
            <span className="flex items-center gap-1.5">
      <BookOpen className="w-4 h-4" /> Editorial Queue
            </span>
          </button>
        </div>

        {/* Tab Specific Content */}
        {activeTab === 'overview' ? (
          /* ── Overview Analytics ── */
          <div className="space-y-8 animate-fade-in">
            {/* Stat Counters */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-5">
              <div className="glass rounded-3xl p-6 border shadow-sm" style={{ borderColor: 'var(--border-color)' }}>
                <div className="w-12 h-12 rounded-2xl bg-brand-gradient flex items-center justify-center mb-3 shadow-brand">
                  <Users className="w-6 h-6 text-white" />
                </div>
                <div className="text-3xl font-black" style={{ color: 'var(--text-primary)' }}>{overview?.totalUsers ?? '...'}</div>
                <div className="text-xs font-semibold mt-1" style={{ color: 'var(--text-muted)' }}>Registered Students</div>
              </div>

              <div className="glass rounded-3xl p-6 border shadow-sm" style={{ borderColor: 'var(--border-color)' }}>
                <div className="w-12 h-12 rounded-2xl bg-emerald-600 flex items-center justify-center mb-3 shadow-md">
                  <CheckCircle className="w-6 h-6 text-white" />
                </div>
                <div className="text-3xl font-black" style={{ color: 'var(--text-primary)' }}>{overview?.activeSubscriptions ?? '...'}</div>
                <div className="text-xs font-semibold mt-1" style={{ color: 'var(--text-muted)' }}>Active Paid Subscribers</div>
              </div>

              <div className="glass rounded-3xl p-6 border shadow-sm" style={{ borderColor: 'var(--border-color)' }}>
                <div className="w-12 h-12 rounded-2xl bg-indigo-600 flex items-center justify-center mb-3 shadow-md">
                  <DollarSign className="w-6 h-6 text-white" />
                </div>
                <div className="text-3xl font-black" style={{ color: 'var(--text-primary)' }}>₹{(overview?.monthlyRecurringRevenue ?? 0).toLocaleString()}</div>
                <div className="text-xs font-semibold mt-1" style={{ color: 'var(--text-muted)' }}>Monthly Recurring Revenue</div>
              </div>

              <div className="glass rounded-3xl p-6 border shadow-sm" style={{ borderColor: 'var(--border-color)' }}>
                <div className="w-12 h-12 rounded-2xl bg-brand-gradient flex items-center justify-center mb-3 shadow-brand">
                  <Compass className="w-6 h-6 text-white" />
                </div>
                <div className="text-3xl font-black" style={{ color: 'var(--text-primary)' }}>{overview?.totalRoadmapsGenerated ?? '...'}</div>
                <div className="text-xs font-semibold mt-1" style={{ color: 'var(--text-muted)' }}>Roadmaps Created</div>
              </div>
            </div>

            {/* Tier Revenue Breakdown */}
            <div className="glass-card">
              <h3 className="font-display font-bold text-lg mb-4" style={{ color: 'var(--text-primary)' }}>
                Active Plan Subscriptions & Entitlements
              </h3>
              <div className="grid sm:grid-cols-3 gap-4">
                {overview?.tierBreakdown?.map((tb: any) => (
                  <div key={tb.tierName} className="p-5 rounded-2xl border bg-white/5" style={{ borderColor: 'var(--border-color)' }}>
                    <div className="text-sm font-bold" style={{ color: 'var(--text-primary)' }}>{tb.tierName}</div>
                    <div className="text-2xl font-black text-brand-400 mt-2">{tb.subscriberCount} Subscribers</div>
                    <div className="text-xs mt-1" style={{ color: 'var(--text-muted)' }}>Est. Revenue: ₹{tb.monthlyRevenue.toLocaleString()}</div>
                  </div>
                )) ?? (
                  <div className="text-xs text-white/40">Loading plan subscription distribution...</div>
                )}
              </div>
            </div>
          </div>
        ) : activeTab === 'users' ? (
          /* ── Users Management ── */
          <div className="space-y-6 animate-fade-in">
            <div className="flex flex-col sm:flex-row gap-4 items-center justify-between">
              <div className="flex items-center gap-3 w-full sm:w-80 px-4 py-2 rounded-2xl border glass" style={{ borderColor: 'var(--border-color)' }}>
                <input
                  type="text"
                  placeholder="Search user by name or email..."
                  value={userSearch}
                  onChange={e => setUserSearch(e.target.value)}
                  onKeyDown={e => e.key === 'Enter' && loadData()}
                  className="bg-transparent text-xs outline-none w-full"
                  style={{ color: 'var(--text-primary)' }}
                />
              </div>
              <button onClick={loadData} className="btn-brand text-xs font-bold py-2.5 px-4 shadow-brand">
                Search Students
              </button>
            </div>

            <div className="glass rounded-3xl border overflow-hidden shadow-sm" style={{ borderColor: 'var(--border-color)' }}>
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead className="border-b bg-white/5 font-bold" style={{ borderColor: 'var(--border-color)', color: 'var(--text-muted)' }}>
                    <tr>
                      <th className="p-4">User</th>
                      <th className="p-4">Role</th>
                      <th className="p-4">Active Plan</th>
                      <th className="p-4">Status</th>
                      <th className="p-4">Registered</th>
                      <th className="p-4 text-right">Actions</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y" style={{ borderColor: 'var(--border-color)' }}>
                    {usersList.length === 0 ? (
                      <tr>
                        <td colSpan={6} className="p-8 text-center text-white/40">No registered students found.</td>
                      </tr>
                    ) : (
                      usersList.map(u => (
                        <tr key={u.id} className="hover:bg-white/5 transition-colors">
                          <td className="p-4">
                            <div className="font-bold" style={{ color: 'var(--text-primary)' }}>{u.displayName || 'Unnamed Student'}</div>
                            <div className="text-[11px]" style={{ color: 'var(--text-muted)' }}>{u.email}</div>
                          </td>
                          <td className="p-4">
                            <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-brand-500/10 text-brand-400">
                              {u.role}
                            </span>
                          </td>
                          <td className="p-4">
                            <span className="font-semibold" style={{ color: 'var(--text-secondary)' }}>
                              {u.subscriptionTier}
                            </span>
                          </td>
                          <td className="p-4">
                            <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${
                              u.isActive ? 'bg-emerald-500/15 text-emerald-400' : 'bg-red-500/15 text-red-400'
                            }`}>
                              {u.isActive ? 'Active' : 'Suspended'}
                            </span>
                          </td>
                          <td className="p-4" style={{ color: 'var(--text-muted)' }}>
                            {new Date(u.createdAt).toLocaleDateString()}
                          </td>
                          <td className="p-4 text-right">
                            <button
                              onClick={() => handleToggleUserSuspension(u.id, u.isActive)}
                              className={`px-3 py-1.5 rounded-xl text-xs font-bold border transition-all ${
                                u.isActive
                                  ? 'border-red-500/20 text-red-400 hover:bg-red-500/10'
                                  : 'border-emerald-500/20 text-emerald-400 hover:bg-emerald-500/10'
                              }`}
                            >
                              {u.isActive ? 'Suspend' : 'Reactivate'}
                            </button>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        ) : activeTab === 'coupons' ? (
          /* ── Coupons Management ── */
          <div className="space-y-6 animate-fade-in">
            <div className="flex justify-between items-center">
              <div>
                <h3 className="font-display font-bold text-lg" style={{ color: 'var(--text-primary)' }}>Promotional Discount Vouchers</h3>
                <p className="text-xs" style={{ color: 'var(--text-muted)' }}>Create global promo codes or target vouchers to individual student IDs</p>
              </div>
              <button
                onClick={() => setShowNewCoupon(true)}
                className="btn-brand text-xs font-bold py-2.5 px-4 flex items-center gap-1.5 shadow-brand"
              >
                <Plus className="w-4 h-4" /> Create Voucher
              </button>
            </div>

            {/* Create Coupon Modal */}
            {showNewCoupon && (
              <div className="glass rounded-3xl p-6 border shadow-2xl space-y-4 animate-slide-up" style={{ borderColor: 'var(--border-color)' }}>
                <div className="flex justify-between items-center">
                  <h4 className="font-bold text-sm" style={{ color: 'var(--text-primary)' }}>New Promo Voucher</h4>
                  <button onClick={() => setShowNewCoupon(false)} className="text-white/40 hover:text-white text-xs">✕ Close</button>
                </div>
                <form onSubmit={handleCreateCoupon} className="grid sm:grid-cols-3 gap-4 text-xs">
                  <div>
                    <label className="block mb-1 font-semibold" style={{ color: 'var(--text-secondary)' }}>Voucher Code</label>
                    <input
                      type="text"
                      required
                      placeholder="e.g. DIWALI50"
                      value={newCouponCode}
                      onChange={e => setNewCouponCode(e.target.value.toUpperCase())}
                      className="input uppercase"
                    />
                  </div>

                  <div>
                    <label className="block mb-1 font-semibold" style={{ color: 'var(--text-secondary)' }}>Discount Type</label>
                    <select
                      value={newCouponType}
                      onChange={e => setNewCouponType(e.target.value as any)}
                      className="input"
                    >
                      <option value="Percentage">Percentage (% Off)</option>
                      <option value="FixedAmount">Flat Amount (₹ Off)</option>
                    </select>
                  </div>

                  <div>
                    <label className="block mb-1 font-semibold" style={{ color: 'var(--text-secondary)' }}>Discount Value</label>
                    <input
                      type="number"
                      required
                      min={1}
                      value={newCouponValue}
                      onChange={e => setNewCouponValue(Number(e.target.value))}
                      className="input"
                    />
                  </div>

                  <div>
                    <label className="block mb-1 font-semibold" style={{ color: 'var(--text-secondary)' }}>Min Plan Price (₹)</label>
                    <input
                      type="number"
                      required
                      min={0}
                      value={newCouponMinPrice}
                      onChange={e => setNewCouponMinPrice(Number(e.target.value))}
                      className="input"
                    />
                  </div>

                  <div>
                    <label className="block mb-1 font-semibold" style={{ color: 'var(--text-secondary)' }}>Max Redemptions</label>
                    <input
                      type="number"
                      required
                      min={1}
                      value={newCouponMaxRedemptions}
                      onChange={e => setNewCouponMaxRedemptions(Number(e.target.value))}
                      className="input"
                    />
                  </div>

                  <div>
                    <label className="block mb-1 font-semibold" style={{ color: 'var(--text-secondary)' }}>Target User ID (Optional)</label>
                    <input
                      type="text"
                      placeholder="Leave blank for All Students"
                      value={newCouponTargetUser}
                      onChange={e => setNewCouponTargetUser(e.target.value)}
                      className="input"
                    />
                  </div>

                  <div className="sm:col-span-2">
                    <label className="block mb-1 font-semibold" style={{ color: 'var(--text-secondary)' }}>Description</label>
                    <input
                      type="text"
                      placeholder="e.g. 50% Festive Discount for Students"
                      value={newCouponDesc}
                      onChange={e => setNewCouponDesc(e.target.value)}
                      className="input"
                    />
                  </div>

                  <div className="flex items-center gap-2 mt-6">
                    <input
                      type="checkbox"
                      id="isPublic"
                      checked={newCouponIsPublic}
                      onChange={e => setNewCouponIsPublic(e.target.checked)}
                      className="w-4 h-4 cursor-pointer"
                    />
                    <label htmlFor="isPublic" className="font-semibold cursor-pointer" style={{ color: 'var(--text-secondary)' }}>
                      Show on Public Pricing Page
                    </label>
                  </div>

                  <div className="sm:col-span-3 flex justify-end gap-3 mt-4 border-t pt-4" style={{ borderColor: 'var(--border-color)' }}>
                    <button type="button" onClick={() => setShowNewCoupon(false)} className="glass-button text-xs py-2 px-4">
                      Cancel
                    </button>
                    <button type="submit" disabled={submitting} className="btn-brand text-xs font-bold py-2 px-6 shadow-brand">
                      {submitting ? 'Creating...' : 'Save & Publish Voucher'}
                    </button>
                  </div>
                </form>
              </div>
            )}

            {/* Coupons List Table */}
            <div className="glass rounded-3xl border overflow-hidden shadow-sm" style={{ borderColor: 'var(--border-color)' }}>
              <div className="overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead className="border-b bg-white/5 font-bold" style={{ borderColor: 'var(--border-color)', color: 'var(--text-muted)' }}>
                    <tr>
                      <th className="p-4">Code & Description</th>
                      <th className="p-4">Discount</th>
                      <th className="p-4">Audience</th>
                      <th className="p-4">Redemptions</th>
                      <th className="p-4">Status</th>
                      <th className="p-4 text-right">Visibility & Controls</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y" style={{ borderColor: 'var(--border-color)' }}>
                    {couponsList.length === 0 ? (
                      <tr>
                        <td colSpan={6} className="p-8 text-center text-white/40">No discount coupons configured.</td>
                      </tr>
                    ) : (
                      couponsList.map(c => (
                        <tr key={c.id} className="hover:bg-white/5 transition-colors">
                          <td className="p-4">
                            <div className="font-black text-sm font-display text-brand-400">{c.code}</div>
                            <div className="text-[11px]" style={{ color: 'var(--text-muted)' }}>{c.description || 'No description'}</div>
                          </td>
                          <td className="p-4 font-bold" style={{ color: 'var(--text-primary)' }}>
                            {c.discountType === 'Percentage' ? `${c.discountValue}% OFF` : `₹${c.discountValue} OFF`}
                          </td>
                          <td className="p-4">
                            {c.targetUserId ? (
                              <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-purple-500/10 text-purple-400">
                                User: {c.targetUserEmail || c.targetUserId.slice(0, 8)}
                              </span>
                            ) : (
                              <span className="px-2 py-0.5 rounded-md text-[10px] font-bold bg-emerald-500/10 text-emerald-400">
                                All Students (Global)
                              </span>
                            )}
                          </td>
                          <td className="p-4" style={{ color: 'var(--text-secondary)' }}>
                            {c.timesRedeemed} / {c.maxRedemptions}
                          </td>
                          <td className="p-4">
                            <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${
                              c.isActive ? 'bg-emerald-500/15 text-emerald-400' : 'bg-red-500/15 text-red-400'
                            }`}>
                              {c.isActive ? 'Active' : 'Disabled'}
                            </span>
                          </td>
                          <td className="p-4 text-right space-x-2">
                            <button
                              onClick={() => handleToggleCoupon(c.id, undefined, !c.isVisiblePublicly)}
                              className="px-2.5 py-1 rounded-lg text-[11px] font-semibold border glass"
                              style={{ borderColor: 'var(--border-color)', color: 'var(--text-secondary)' }}
                            >
                              {c.isVisiblePublicly ? '👁️ Public' : '🔒 Hidden'}
                            </button>
                            <button
                              onClick={() => handleToggleCoupon(c.id, !c.isActive, undefined)}
                              className={`px-2.5 py-1 rounded-lg text-[11px] font-bold border transition-all ${
                                c.isActive
                                  ? 'border-red-500/20 text-red-400 hover:bg-red-500/10'
                                  : 'border-emerald-500/20 text-emerald-400 hover:bg-emerald-500/10'
                              }`}
                            >
                              {c.isActive ? 'Deactivate' : 'Activate'}
                            </button>
                          </td>
                        </tr>
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        ) : (
          /* ── Content Auditing / Ingestion Tabs (Existing 3 Tabs) ── */
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            
            {/* Main List */}
            <div className="lg:col-span-2 space-y-4">
              
              {activeTab === 'imports' ? (
                /* Imports List */
                importJobs.length === 0 ? (
                  <div className="glass rounded-2xl p-10 text-center text-white/40 border border-white/5">
                    No catalog import jobs have been submitted.
                  </div>
                ) : (
                  importJobs.map(job => (
                    <div key={job.id} className="glass rounded-2xl p-5 border border-white/10 flex items-center justify-between gap-4 hover:border-white/20 transition-all">
                      <div>
                        <div className="flex items-center gap-2.5">
                          <span className="text-white font-medium text-base">{job.fileName}</span>
                          <span className={`text-[10px] uppercase font-bold px-2 py-0.5 rounded-full ${
                            job.status === 'Completed' ? 'bg-accent-teal/15 text-accent-teal' :
                            job.status === 'Staged' ? 'bg-yellow-500/15 text-yellow-400' :
                            job.status === 'Failed' ? 'bg-red-500/15 text-red-400' : 'bg-white/10 text-white/60'
                          }`}>{job.status}</span>
                        </div>
                        <p className="text-white/40 text-xs mt-1">
                          Type: <strong className="text-white/70">{job.importType}</strong> • 
                          Size: <strong className="text-white/70">{(job.fileSize / 1024).toFixed(1)} KB</strong> • 
                          Created: <strong className="text-white/70">{new Date(job.createdAt).toLocaleString()}</strong>
                        </p>
                      </div>
                      <button onClick={() => viewJobDetails(job.id)} className="glass-button text-xs py-2 px-3 flex items-center gap-1">
                        <Eye className="w-3.5 h-3.5" /> Audit Details
                      </button>
                    </div>
                  ))
                )
              ) : activeTab === 'knowledge' ? (
                /* Knowledge Base List */
                documents.length === 0 ? (
                  <div className="glass rounded-2xl p-10 text-center text-white/40 border border-white/5">
                    No syllabus or exam documents have been uploaded.
                  </div>
                ) : (
                  documents.map(doc => (
                    <div key={doc.id} className="glass rounded-2xl p-5 border border-white/10 flex items-center justify-between gap-4 hover:border-white/20 transition-all">
                      <div>
                        <div className="flex items-center gap-2.5">
                          <span className="text-white font-medium text-base">{doc.title}</span>
                          <span className={`text-[10px] uppercase font-bold px-2 py-0.5 rounded-full ${
                            doc.status === 'Indexed' ? 'bg-accent-teal/15 text-accent-teal' :
                            doc.status === 'Reviewing' ? 'bg-yellow-500/15 text-yellow-400' :
                            doc.status === 'Failed' ? 'bg-red-500/15 text-red-400' : 'bg-white/10 text-white/60'
                          }`}>{doc.status}</span>
                        </div>
                        <p className="text-white/40 text-xs mt-1">
                          Type: <strong className="text-white/70">{doc.docType}</strong> • 
                          Size: <strong className="text-white/70">{(doc.fileSize / 1024).toFixed(1)} KB</strong> • 
                          Updated: <strong className="text-white/70">{new Date(doc.updatedAt).toLocaleString()}</strong>
                        </p>
                      </div>
                      <button onClick={() => viewDocDetails(doc.id)} className="glass-button text-xs py-2 px-3 flex items-center gap-1">
                        <Eye className="w-3.5 h-3.5" /> Edit Chunks
                      </button>
                    </div>
                  ))
                )
              ) : (
                /* Editorial Review Queue List */
                articles.length === 0 ? (
                  <div className="glass rounded-2xl p-10 text-center text-white/40 border border-white/5">
                    No article drafts are pending review.
                  </div>
                ) : (
                  articles.map(art => (
                    <div key={art.id} className="glass rounded-2xl p-5 border border-white/10 flex items-center justify-between gap-4 hover:border-white/20 transition-all">
                      <div>
                        <div className="flex items-center gap-2.5">
                          <span className="text-white font-medium text-base">{art.title}</span>
                          <span className={`text-[10px] uppercase font-bold px-2 py-0.5 rounded-full ${
                            art.status === 'Published' ? 'bg-accent-teal/15 text-accent-teal' :
                            art.status === 'InReview' ? 'bg-yellow-500/15 text-yellow-400' :
                            art.status === 'Approved' ? 'bg-brand-500/15 text-brand-400' : 'bg-white/10 text-white/60'
                          }`}>{art.status}</span>
                        </div>
                        <p className="text-white/40 text-xs mt-1">
                          Locale: <strong className="text-white/70">{art.locale?.toUpperCase()}</strong> • 
                          Type: <strong className="text-white/70">{art.articleType}</strong> • 
                          Updated: <strong className="text-white/70">{new Date(art.updatedAt).toLocaleString()}</strong>
                        </p>
                      </div>
                      <button onClick={() => viewArticleDetails(art.id)} className="glass-button text-xs py-2 px-3 flex items-center gap-1">
                        <Eye className="w-3.5 h-3.5" /> Review Draft
                      </button>
                    </div>
                  ))
                )
              )}
            </div>

            {/* Details and Review Decisions Sidebar Panel */}
            <div>
              {selectedJob ? (
                /* Job audit review panel */
                <div className="glass rounded-2xl p-6 border border-white/15 space-y-6 sticky top-24 animate-slide-up">
                  <div>
                    <h3 className="font-display font-bold text-lg text-white">Import Job Details</h3>
                    <p className="text-white/40 text-xs mt-0.5">{selectedJob.fileName}</p>
                  </div>
                  
                  <div className="space-y-3 border-t border-white/10 pt-4 text-sm">
                    <div className="flex justify-between"><span className="text-white/40">Status:</span><span className="text-white font-semibold">{selectedJob.status}</span></div>
                    <div className="flex justify-between"><span className="text-white/40">Total Rows:</span><span className="text-white font-semibold">{selectedJob.totalRows}</span></div>
                    <div className="flex justify-between"><span className="text-white/40">Valid:</span><span className="text-accent-teal font-semibold">{selectedJob.validRows}</span></div>
                    <div className="flex justify-between"><span className="text-white/40">Errors:</span><span className="text-red-400 font-semibold">{selectedJob.errorRows}</span></div>
                  </div>

                  {/* Actions */}
                  <div className="space-y-3 border-t border-white/10 pt-4">
                    <div>
                      <label className="block text-white/60 text-xs font-medium mb-1.5">Review Decision Notes</label>
                      <textarea
                        value={actionNotes}
                        onChange={e => setActionNotes(e.target.value)}
                        placeholder="Verify against state qualification rules..."
                        className="w-full text-sm bg-white/5 border border-white/10 rounded-xl px-3 py-2 text-white placeholder-white/30 h-20 outline-none focus:border-brand-500"
                      />
                    </div>
                    <div className="grid grid-cols-2 gap-3">
                      <button
                        onClick={() => handleImportReview(selectedJob.id, false)}
                        disabled={submitting}
                        className="glass-button border-red-500/20 text-red-400 hover:bg-red-500/10 justify-center flex py-3 text-xs font-bold"
                      >
                        <XCircle className="w-4 h-4 mr-1.5" /> Reject
                      </button>
                      <button
                        onClick={() => handleImportReview(selectedJob.id, true)}
                        disabled={submitting}
                        className="btn-brand justify-center flex py-3 text-xs font-bold shadow-brand"
                      >
                        <CheckCircle className="w-4 h-4 mr-1.5" /> Approve Rows
                      </button>
                    </div>
                  </div>
                </div>
              ) : selectedDoc ? (
                /* Document audit chunks panel */
                <div className="glass rounded-2xl p-6 border border-white/15 space-y-6 sticky top-24 animate-slide-up">
                  <div>
                    <h3 className="font-display font-bold text-lg text-white">Document Chunks</h3>
                    <p className="text-white/40 text-xs mt-0.5">{selectedDoc.title}</p>
                  </div>
                  
                  <div className="space-y-3 border-t border-white/10 pt-4 text-sm">
                    <div className="flex justify-between"><span className="text-white/40">Status:</span><span className="text-white font-semibold">{selectedDoc.status}</span></div>
                    <div className="flex justify-between"><span className="text-white/40">Chunks Extracted:</span><span className="text-accent-teal font-semibold">{selectedDoc.chunkCount}</span></div>
                    <div className="flex justify-between"><span className="text-white/40">Subject:</span><span className="text-white font-medium">{selectedDoc.subjectArea ?? 'General'}</span></div>
                  </div>

                  {/* Actions */}
                  {selectedDoc.status === 'Reviewing' ? (
                    <div className="space-y-3 border-t border-white/10 pt-4">
                      <div>
                        <label className="block text-white/60 text-xs font-medium mb-1.5">Verification Notes</label>
                        <textarea
                          value={actionNotes}
                          onChange={e => setActionNotes(e.target.value)}
                          placeholder="Chunks verified for OCR fidelity."
                          className="w-full text-sm bg-white/5 border border-white/10 rounded-xl px-3 py-2 text-white placeholder-white/30 h-20 outline-none focus:border-brand-500"
                        />
                      </div>
                      <div className="grid grid-cols-2 gap-3">
                        <button
                          onClick={() => handleDocumentReview(selectedDoc.id, false)}
                          disabled={submitting}
                          className="glass-button border-red-500/20 text-red-400 hover:bg-red-500/10 justify-center flex py-3 text-xs font-bold"
                        >
                          <XCircle className="w-4 h-4 mr-1.5" /> Reject
                        </button>
                        <button
                          onClick={() => handleDocumentReview(selectedDoc.id, true)}
                          disabled={submitting}
                          className="btn-brand justify-center flex py-3 text-xs font-bold shadow-brand"
                        >
                          <CheckCircle className="w-4 h-4 mr-1.5" /> Approve & Index
                        </button>
                      </div>
                    </div>
                  ) : (
                    <div className="text-center p-4 rounded-xl bg-white/5 text-xs text-white/40 border-t border-white/10 pt-4">
                      No active decisions pending for this status.
                    </div>
                  )}
                </div>
              ) : selectedArticle ? (
                /* Article editorial audit and review panel */
                <div className="glass rounded-2xl p-6 border border-white/15 space-y-6 sticky top-24 animate-slide-up">
                  <div>
                    <h3 className="font-display font-bold text-lg text-white">Editorial Article Draft</h3>
                    <p className="text-white/40 text-xs mt-0.5">{selectedArticle.title}</p>
                  </div>

                  <div className="space-y-3 border-t border-white/10 pt-4 text-sm">
                    <div className="flex justify-between"><span className="text-white/40">Status:</span><span className="text-white font-semibold">{selectedArticle.status}</span></div>
                    <div className="flex justify-between"><span className="text-white/40">Locale:</span><span className="text-brand-400 font-semibold">{selectedArticle.locale?.toUpperCase()}</span></div>
                    <div className="flex justify-between"><span className="text-white/40">Type:</span><span className="text-white font-medium">{selectedArticle.articleType}</span></div>
                    {selectedArticle.currentVersion && (
                      <div className="flex justify-between"><span className="text-white/40">Words:</span><span className="text-accent-teal font-semibold">{selectedArticle.currentVersion.wordCount} words</span></div>
                    )}
                  </div>

                  {/* Article Body Preview */}
                  {selectedArticle.currentVersion?.body && (
                    <div className="p-3 bg-white/5 rounded-xl border border-white/5 text-xs max-h-48 overflow-y-auto leading-relaxed text-white/80 whitespace-pre-wrap">
                      {selectedArticle.currentVersion.body}
                    </div>
                  )}

                  {/* Actions */}
                  <div className="space-y-3 border-t border-white/10 pt-4">
                    {selectedArticle.status === 'InReview' && (
                      <>
                        <div>
                          <label className="block text-white/60 text-xs font-medium mb-1.5">Editorial Feedback</label>
                          <textarea
                            value={actionNotes}
                            onChange={e => setActionNotes(e.target.value)}
                            placeholder="Factual accuracy verified. Approved for publication."
                            className="w-full text-sm bg-white/5 border border-white/10 rounded-xl px-3 py-2 text-white placeholder-white/30 h-16 outline-none focus:border-brand-500"
                          />
                        </div>
                        <div className="grid grid-cols-2 gap-3">
                          <button
                            onClick={() => handleArticleReviewDecision(selectedArticle.id, selectedArticle.reviews?.[0]?.id || 1, false)}
                            disabled={submitting}
                            className="glass-button border-red-500/20 text-red-400 hover:bg-red-500/10 justify-center flex py-2.5 text-xs font-bold"
                          >
                            <XCircle className="w-4 h-4 mr-1.5" /> Request Changes
                          </button>
                          <button
                            onClick={() => handleArticleReviewDecision(selectedArticle.id, selectedArticle.reviews?.[0]?.id || 1, true)}
                            disabled={submitting}
                            className="btn-brand justify-center flex py-2.5 text-xs font-bold shadow-brand"
                          >
                            <CheckCircle className="w-4 h-4 mr-1.5" /> Approve Draft
                          </button>
                        </div>
                      </>
                    )}

                    {selectedArticle.status !== 'Published' && (
                      <button
                        onClick={() => handlePublishArticle(selectedArticle.id)}
                        disabled={submitting}
                        className="btn-brand w-full justify-center flex py-3 text-xs font-bold shadow-brand bg-emerald-600 hover:bg-emerald-500"
                      >
                        <Globe className="w-4 h-4 mr-1.5" /> Publish to Live Catalog
                      </button>
                    )}
                  </div>
                </div>
              ) : (
                /* No selection panel */
                <div className="glass rounded-2xl p-6 border border-white/5 text-center text-white/30 text-sm py-12 sticky top-24">
                  Select an audit item to trigger review actions or view staged segment chunks.
                </div>
              )}
            </div>

          </div>
        )}
      </div>
    </div>
  )
}
