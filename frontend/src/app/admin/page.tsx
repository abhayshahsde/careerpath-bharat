/* eslint-disable @typescript-eslint/no-explicit-any */
'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { useAuth } from '@/lib/auth-context'
import { api } from '@/lib/api'
import { 
  Shield, Upload, FileText, CheckCircle, AlertTriangle, RefreshCw, 
  BookOpen, Globe, Users, TrendingUp, DollarSign, Compass, Tag, Plus, Settings, 
  Award, Trash2, Edit3, UserPlus, Save, Megaphone, Menu, X, GraduationCap
} from 'lucide-react'

type TabType = 'overview' | 'settings' | 'users' | 'careers' | 'exams' | 'courses' | 'coupons' | 'knowledge' | 'editorial' | 'imports'

export default function AdminPage() {
  const router = useRouter()
  const { user, isAuthenticated, isLoading } = useAuth()
  const [activeTab, setActiveTab] = useState<TabType>('overview')

  // Status & Notification
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [successMsg, setSuccessMsg] = useState('')

  // 1. Overview States
  const [overview, setOverview] = useState<any | null>(null)

  // 2. Site Settings & Branding States
  const [siteName, setSiteName] = useState('CareerPath Bharat')
  const [logoText, setLogoText] = useState('CareerPath')
  const [logoSubtitle, setLogoSubtitle] = useState('Bharat')
  const [tagline, setTagline] = useState("India's premier career guidance and roadmapping platform for students")
  const [announcementText, setAnnouncementText] = useState('⚡ UPSC, JEE & NEET 2026 notifications out now! Check your personalized roadmaps.')
  const [announcementActive, setAnnouncementActive] = useState(true)
  const [supportEmail, setSupportEmail] = useState('support@careerpathbharat.com')
  const [supportPhone, setSupportPhone] = useState('+91 9876543210')
  const [footerText, setFooterText] = useState('Empowering students across all 28 states & 8 UTs of Bharat.')
  const [navMenus, setNavMenus] = useState<Array<{ label: string; href: string; isActive: boolean }>>([])
  const [newMenuLabel, setNewMenuLabel] = useState('')
  const [newMenuHref, setNewMenuHref] = useState('')

  // 3. Staff & User Management States
  const [usersList, setUsersList] = useState<any[]>([])
  const [userSearch, setUserSearch] = useState('')
  const [showCreateStaffModal, setShowCreateStaffModal] = useState(false)
  const [staffEmail, setStaffEmail] = useState('')
  const [staffPassword, setStaffPassword] = useState('')
  const [staffName, setStaffName] = useState('')
  const [staffRole, setStaffRole] = useState('Admin')

  // 4. Careers CMS States
  const [careersList, setCareersList] = useState<any[]>([])
  const [showCareerModal, setShowCareerModal] = useState(false)
  const [editingCareerId, setEditingCareerId] = useState<string | null>(null)
  const [careerTitle, setCareerTitle] = useState('')
  const [careerSlug, setCareerSlug] = useState('')
  const [careerSummary, setCareerSummary] = useState('')
  const [careerSalary, setCareerSalary] = useState('₹6 - 18 LPA')
  const [careerIsFeatured, setCareerIsFeatured] = useState(true)

  // 5. Exams CMS States
  const [examsList, setExamsList] = useState<any[]>([])
  const [showExamModal, setShowExamModal] = useState(false)
  const [editingExamId, setEditingExamId] = useState<number | null>(null)
  const [examName, setExamName] = useState('')
  const [examCode, setExamCode] = useState('')
  const [examLevel, setExamLevel] = useState('National')
  const [examUrl, setExamUrl] = useState('')
  const [examEligibility, setExamEligibility] = useState('')

  // 6. Courses CMS States
  const [coursesList, setCoursesList] = useState<any[]>([])
  const [showCourseModal, setShowCourseModal] = useState(false)
  const [editingCourseId, setEditingCourseId] = useState<number | null>(null)
  const [courseName, setCourseName] = useState('')
  const [courseSlug, setCourseSlug] = useState('')
  const [courseDegree, setCourseDegree] = useState('UG')
  const [courseDuration, setCourseDuration] = useState(4)
  const [courseEligibility, setCourseEligibility] = useState('10+2 with Physics, Chem, Math')

  // 7. Coupons States
  const [couponsList, setCouponsList] = useState<any[]>([])
  const [showNewCoupon, setShowNewCoupon] = useState(false)
  const [newCouponCode, setNewCouponCode] = useState('')
  const [newCouponDesc, setNewCouponDesc] = useState('')
  const [newCouponType, setNewCouponType] = useState<'Percentage' | 'FixedAmount'>('Percentage')
  const [newCouponValue, setNewCouponValue] = useState(20)
  const [newCouponMinPrice, setNewCouponMinPrice] = useState(100)
  const [newCouponMaxRedemptions, setNewCouponMaxRedemptions] = useState(100)

  // 8. Knowledge Base & Chunks States
  const [documents, setDocuments] = useState<any[]>([])
  const [selectedDoc, setSelectedDoc] = useState<any | null>(null)
  const [showNewDocModal, setShowNewDocModal] = useState(false)
  const [docTitle, setDocTitle] = useState('')
  const [docType, setDocType] = useState('Syllabus')
  const [docChunksText, setDocChunksText] = useState('')
  const [editingChunkId, setEditingChunkId] = useState<number | null>(null)
  const [editingChunkContent, setEditingChunkContent] = useState('')

  // 9. Editorial Queue States
  const [articles, setArticles] = useState<any[]>([])
  const [showNewArticleModal, setShowNewArticleModal] = useState(false)
  const [articleTitle, setArticleTitle] = useState('')
  const [articleSlug, setArticleSlug] = useState('')
  const [articleSummary, setArticleSummary] = useState('')
  const [articleBody, setArticleBody] = useState('')
  const [articleAuthor, setArticleAuthor] = useState('Editorial Desk')

  // 10. Bulk Imports States
  const [importJobs, setImportJobs] = useState<any[]>([])

  // ── 1. Authorization Guard ───────────────────────────────────────────────────
  useEffect(() => {
    if (!isLoading && (!isAuthenticated || !user?.roles?.some(r => ['Admin', 'SuperAdmin', 'ContentEditor'].includes(r)))) {
      router.push('/')
    }
  }, [isLoading, isAuthenticated, user, router])

  // ── 2. Load Data Function ────────────────────────────────────────────────────
  const loadData = async () => {
    setError('')
    setSuccessMsg('')
    setLoading(true)

    try {
      // 1. Overview
      const ov = await api.getAdminOverview().catch(() => null)
      if (ov) setOverview(ov)

      // 2. Settings
      const settings = await api.getPublicSettings().catch(() => null)
      if (settings) {
        setSiteName(settings.siteName || 'CareerPath Bharat')
        setLogoText(settings.logoText || 'CareerPath')
        setLogoSubtitle(settings.logoSubtitle || 'Bharat')
        setTagline(settings.tagline || '')
        setAnnouncementText(settings.announcementText || '')
        setAnnouncementActive(settings.announcementActive)
        setSupportEmail(settings.supportEmail || 'support@careerpathbharat.com')
        setSupportPhone(settings.supportPhone || '+91 9876543210')
        setFooterText(settings.footerText || '')
        if (settings.navMenusJson) {
          try {
            setNavMenus(JSON.parse(settings.navMenusJson))
          } catch {
            setNavMenus([
              { label: 'Dashboard', href: '/dashboard', isActive: true },
              { label: 'Roadmaps', href: '/me/roadmaps', isActive: true },
              { label: 'Careers', href: '/careers', isActive: true },
              { label: 'Exams', href: '/exams', isActive: true },
              { label: 'Courses', href: '/courses', isActive: true },
              { label: 'Scholarships', href: '/scholarships', isActive: true },
            ])
          }
        }
      }

      // 3. Users
      const uList = await api.getAdminUsers().catch(() => [])
      setUsersList(uList || [])

      // 4. Careers
      const careersRes = await api.getCareers({ pageSize: 50 }).catch(() => ({ items: [] }))
      setCareersList(careersRes?.items || [])

      // 5. Exams
      const examsRes = await api.getExams({ page: 1 }).catch(() => ({ items: [] }))
      setExamsList(examsRes?.items || [])

      // 6. Courses
      const coursesRes = await api.getCourses({ page: 1 }).catch(() => ({ items: [] }))
      setCoursesList(coursesRes?.items || [])

      // 7. Coupons
      const coups = await api.getAdminCoupons().catch(() => [])
      setCouponsList(coups || [])

      // 8. Knowledge Docs
      const docs = await api.getDocuments().catch(() => [])
      setDocuments(docs || [])

      // 9. Editorial Articles
      const arts = await api.getEditorialArticles().catch(() => [])
      setArticles(arts || [])

      // 10. Imports
      const jobs = await api.getImportJobs().catch(() => [])
      setImportJobs(jobs || [])

    } catch (err: any) {
      setError(err?.message || 'Failed to sync admin datasets.')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (isAuthenticated) {
      loadData()
    }
  }, [isAuthenticated])

  // ── Handlers: Site Settings ──────────────────────────────────────────────────
  async function handleSaveSettings(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccessMsg('')
    try {
      await api.updateSiteSettings({
        siteName,
        logoText,
        logoSubtitle,
        tagline,
        announcementText,
        announcementActive,
        supportEmail,
        supportPhone,
        footerText,
        navMenusJson: JSON.stringify(navMenus)
      })
      setSuccessMsg('Branding, logo, and navigation menus updated successfully! Changes are live across the site.')
    } catch (err: any) {
      setError(err.message || 'Failed to save site settings.')
    }
  }

  function handleAddNavMenu() {
    if (!newMenuLabel || !newMenuHref) return
    const updated = [...navMenus, { label: newMenuLabel.trim(), href: newMenuHref.trim(), isActive: true }]
    setNavMenus(updated)
    setNewMenuLabel('')
    setNewMenuHref('')
  }

  function handleToggleNavMenu(index: number) {
    const updated = [...navMenus]
    updated[index].isActive = !updated[index].isActive
    setNavMenus(updated)
  }

  function handleDeleteNavMenu(index: number) {
    const updated = navMenus.filter((_, i) => i !== index)
    setNavMenus(updated)
  }

  // ── Handlers: Staff Creation ─────────────────────────────────────────────────
  async function handleCreateStaff(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccessMsg('')
    try {
      const res = await api.createStaffUser({
        email: staffEmail,
        password: staffPassword,
        displayName: staffName,
        role: staffRole
      })
      setSuccessMsg(res.message || 'Staff member created successfully.')
      setShowCreateStaffModal(false)
      setStaffEmail('')
      setStaffPassword('')
      setStaffName('')
      loadData()
    } catch (err: any) {
      setError(err.message || 'Failed to create staff account.')
    }
  }

  // ── Handlers: Careers CRUD ───────────────────────────────────────────────────
  async function handleSaveCareer(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccessMsg('')
    try {
      if (editingCareerId) {
        await api.updateCareer(editingCareerId, {
          title: careerTitle,
          summary: careerSummary,
          salaryRangeLabel: careerSalary,
          isFeatured: careerIsFeatured
        })
        setSuccessMsg(`Career "${careerTitle}" updated.`)
      } else {
        await api.createCareer({
          title: careerTitle,
          slug: careerSlug || careerTitle.toLowerCase().replace(/[^a-z0-9]+/g, '-'),
          summary: careerSummary,
          salaryRangeLabel: careerSalary,
          isFeatured: careerIsFeatured
        })
        setSuccessMsg(`Career "${careerTitle}" created successfully.`)
      }
      setShowCareerModal(false)
      loadData()
    } catch (err: any) {
      setError(err.message || 'Failed to save career.')
    }
  }

  async function handleDeleteCareer(id: string) {
    if (!confirm('Are you sure you want to remove this career?')) return
    try {
      await api.deleteCareer(id)
      setSuccessMsg('Career removed.')
      loadData()
    } catch (err: any) {
      setError(err.message || 'Failed to remove career.')
    }
  }

  // ── Handlers: Exams CRUD ─────────────────────────────────────────────────────
  async function handleSaveExam(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccessMsg('')
    try {
      if (editingExamId) {
        await api.updateExam(editingExamId, {
          name: examName,
          code: examCode,
          level: examLevel,
          websiteUrl: examUrl,
          eligibilitySummary: examEligibility
        })
        setSuccessMsg(`Exam "${examName}" updated.`)
      } else {
        await api.createExam({
          name: examName,
          code: examCode || examName.toUpperCase().slice(0, 10),
          level: examLevel,
          websiteUrl: examUrl,
          eligibilitySummary: examEligibility
        })
        setSuccessMsg(`Exam "${examName}" created.`)
      }
      setShowExamModal(false)
      loadData()
    } catch (err: any) {
      setError(err.message || 'Failed to save exam.')
    }
  }

  async function handleDeleteExam(id: number) {
    if (!confirm('Are you sure you want to delete this exam?')) return
    try {
      await api.deleteExam(id)
      setSuccessMsg('Exam removed.')
      loadData()
    } catch (err: any) {
      setError(err.message || 'Failed to remove exam.')
    }
  }

  // ── Handlers: Courses CRUD ───────────────────────────────────────────────────
  async function handleSaveCourse(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccessMsg('')
    try {
      if (editingCourseId) {
        await api.updateCourse(editingCourseId, {
          name: courseName,
          degreeLevel: courseDegree,
          durationYears: courseDuration,
          eligibilityCriteria: courseEligibility
        })
        setSuccessMsg(`Course "${courseName}" updated.`)
      } else {
        await api.createCourse({
          name: courseName,
          slug: courseSlug || courseName.toLowerCase().replace(/[^a-z0-9]+/g, '-'),
          degreeLevel: courseDegree,
          durationYears: courseDuration,
          eligibilityCriteria: courseEligibility
        })
        setSuccessMsg(`Course "${courseName}" created.`)
      }
      setShowCourseModal(false)
      loadData()
    } catch (err: any) {
      setError(err.message || 'Failed to save course.')
    }
  }

  async function handleDeleteCourse(id: number) {
    if (!confirm('Are you sure you want to remove this course?')) return
    try {
      await api.deleteCourse(id)
      setSuccessMsg('Course removed.')
      loadData()
    } catch (err: any) {
      setError(err.message || 'Failed to remove course.')
    }
  }

  // ── Handlers: Coupons ────────────────────────────────────────────────────────
  async function handleCreateCoupon(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccessMsg('')
    try {
      await api.createAdminCoupon({
        code: newCouponCode.trim().toUpperCase(),
        description: newCouponDesc,
        discountType: newCouponType,
        discountValue: Number(newCouponValue),
        minPlanPrice: Number(newCouponMinPrice),
        maxRedemptions: Number(newCouponMaxRedemptions),
        isActive: true,
        isVisiblePublicly: true
      })
      setSuccessMsg(`Coupon "${newCouponCode.toUpperCase()}" created.`)
      setShowNewCoupon(false)
      setNewCouponCode('')
      setNewCouponDesc('')
      loadData()
    } catch (err: any) {
      setError(err.message || 'Failed to create coupon.')
    }
  }

  // ── Handlers: Knowledge Documents ────────────────────────────────────────────
  async function handleCreateKnowledgeDoc(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccessMsg('')
    try {
      const chunks = docChunksText.split('\n\n').map(c => c.trim()).filter(Boolean)
      if (chunks.length === 0) chunks.push(docChunksText.trim())

      await api.createKnowledgeDocument({
        title: docTitle,
        docType,
        chunks
      })
      setSuccessMsg(`Knowledge document "${docTitle}" indexed successfully for AI.`)
      setShowNewDocModal(false)
      setDocTitle('')
      setDocChunksText('')
      loadData()
    } catch (err: any) {
      setError(err.message || 'Failed to create knowledge document.')
    }
  }

  async function handleSaveChunk(chunkId: number) {
    try {
      await api.updateDocumentChunk(chunkId, editingChunkContent, true)
      setSuccessMsg('Knowledge chunk updated and re-indexed.')
      setEditingChunkId(null)
      if (selectedDoc) {
        const updated = await api.getDocumentDetail(selectedDoc.id)
        setSelectedDoc(updated)
      }
    } catch (err: any) {
      setError(err.message || 'Failed to update chunk.')
    }
  }

  // ── Handlers: Editorial Articles ─────────────────────────────────────────────
  async function handleCreateArticle(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    setSuccessMsg('')
    try {
      await api.createEditorialArticle({
        title: articleTitle,
        slug: articleSlug || articleTitle.toLowerCase().replace(/[^a-z0-9]+/g, '-'),
        summary: articleSummary,
        bodyContent: articleBody,
        authorName: articleAuthor || 'Editorial Desk'
      })
      setSuccessMsg(`Article "${articleTitle}" submitted to the editorial queue.`)
      setShowNewArticleModal(false)
      setArticleTitle('')
      setArticleSlug('')
      setArticleSummary('')
      setArticleBody('')
      loadData()
    } catch (err: any) {
      setError(err.message || 'Failed to submit article.')
    }
  }

  async function handlePublishArticle(id: string) {
    try {
      await api.publishEditorialArticle(id)
      setSuccessMsg('Article published live to the platform!')
      loadData()
    } catch (err: any) {
      setError(err.message || 'Failed to publish article.')
    }
  }

  // ── Handlers: Bulk Imports ───────────────────────────────────────────────────
  async function handleTriggerImport() {
    setError('')
    setSuccessMsg('')
    try {
      const res = await api.triggerImportJob()
      setSuccessMsg(res.message || 'Import batch executed successfully.')
      loadData()
    } catch (err: any) {
      setError(err.message || 'Failed to run import batch.')
    }
  }

  return (
    <div className="min-h-screen py-10 px-4 sm:px-6 lg:px-8 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-8">
        <div>
          <div className="flex items-center gap-2.5 mb-1">
            <div className="w-10 h-10 rounded-2xl bg-brand-gradient flex items-center justify-center shadow-brand text-white">
              <Shield className="w-5 h-5" />
            </div>
            <h1 className="text-2xl font-bold font-display" style={{ color: 'var(--text-primary)' }}>
              Super Admin Control Hub & CMS
            </h1>
          </div>
          <p className="text-xs" style={{ color: 'var(--text-muted)' }}>
            Real-time management for branding, navigation menus, career/exam catalogs, staff access, and AI RAG knowledge chunks.
          </p>
        </div>

        <button
          onClick={loadData}
          disabled={loading}
          className="btn-secondary px-4 py-2.5 rounded-xl text-xs font-semibold flex items-center gap-2 self-start md:self-auto"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
          <span>Sync Live Data</span>
        </button>
      </div>

      {/* Notifications */}
      {error && (
        <div className="mb-6 p-4 rounded-2xl bg-red-500/10 border border-red-500/20 text-red-600 dark:text-red-400 text-xs flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <AlertTriangle className="w-4 h-4 shrink-0" />
            <span>{error}</span>
          </div>
          <button onClick={() => setError('')}><X className="w-4 h-4" /></button>
        </div>
      )}

      {successMsg && (
        <div className="mb-6 p-4 rounded-2xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-600 dark:text-emerald-400 text-xs flex items-center justify-between">
          <div className="flex items-center gap-2.5">
            <CheckCircle className="w-4 h-4 shrink-0" />
            <span>{successMsg}</span>
          </div>
          <button onClick={() => setSuccessMsg('')}><X className="w-4 h-4" /></button>
        </div>
      )}

      {/* Tabs Navigation */}
      <div className="flex items-center gap-1.5 overflow-x-auto pb-3 mb-8 no-scrollbar border-b" style={{ borderColor: 'var(--border-color)' }}>
        {[
          { id: 'overview', label: 'Overview', icon: TrendingUp },
          { id: 'settings', label: 'Branding & Menus', icon: Settings },
          { id: 'users', label: 'Staff & Roles', icon: Users },
          { id: 'careers', label: 'Careers CMS', icon: Compass },
          { id: 'exams', label: 'Exams CMS', icon: BookOpen },
          { id: 'courses', label: 'Courses & Degrees', icon: GraduationCap },
          { id: 'coupons', label: 'Coupons & Vouchers', icon: Tag },
          { id: 'knowledge', label: 'Knowledge Chunks', icon: FileText },
          { id: 'editorial', label: 'Editorial Queue', icon: Globe },
          { id: 'imports', label: 'Bulk Imports', icon: Upload },
        ].map(t => {
          const Icon = t.icon
          const isActive = activeTab === t.id
          return (
            <button
              key={t.id}
              onClick={() => setActiveTab(t.id as TabType)}
              className={`flex items-center gap-2 px-4 py-2.5 rounded-2xl text-xs font-bold transition-all shrink-0 border ${
                isActive
                  ? 'bg-brand-500 text-white border-brand-500 shadow-brand ring-2 ring-brand-500/20'
                  : 'border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 hover:bg-slate-100 dark:hover:bg-slate-800/60'
              }`}
            >
              <Icon className="w-3.5 h-3.5" />
              <span>{t.label}</span>
            </button>
          )
        })}
      </div>

      {/* ── TAB 1: OVERVIEW ── */}
      {activeTab === 'overview' && (
        <div className="space-y-8 animate-fade-in">
          {/* KPI Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="p-6 rounded-3xl border" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-semibold text-slate-500">Total Registered Users</span>
                <Users className="w-4 h-4 text-brand-500" />
              </div>
              <p className="text-2xl font-bold font-display" style={{ color: 'var(--text-primary)' }}>
                {overview?.totalUsers ?? usersList.length}
              </p>
              <span className="text-[11px] text-emerald-500 font-medium">All 28 States & UTs</span>
            </div>

            <div className="p-6 rounded-3xl border" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-semibold text-slate-500">Active Subscriptions</span>
                <Award className="w-4 h-4 text-emerald-500" />
              </div>
              <p className="text-2xl font-bold font-display" style={{ color: 'var(--text-primary)' }}>
                {overview?.activeSubscriptions ?? 0}
              </p>
              <span className="text-[11px] text-slate-400">Pro & Premium Tier</span>
            </div>

            <div className="p-6 rounded-3xl border" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-semibold text-slate-500">Monthly Revenue (MRR)</span>
                <DollarSign className="w-4 h-4 text-amber-500" />
              </div>
              <p className="text-2xl font-bold font-display" style={{ color: 'var(--text-primary)' }}>
                ₹{(overview?.monthlyRecurringRevenue ?? 0).toLocaleString('en-IN')}
              </p>
              <span className="text-[11px] text-emerald-500 font-medium">Recurring</span>
            </div>

            <div className="p-6 rounded-3xl border" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
              <div className="flex items-center justify-between mb-2">
                <span className="text-xs font-semibold text-slate-500">AI Queries Served</span>
                <FileText className="w-4 h-4 text-indigo-500" />
              </div>
              <p className="text-2xl font-bold font-display" style={{ color: 'var(--text-primary)' }}>
                {overview?.totalAiQueriesServed ?? 128}
              </p>
              <span className="text-[11px] text-indigo-400">RAG Powered</span>
            </div>
          </div>

          {/* Quick Actions */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <button
              onClick={() => setActiveTab('settings')}
              className="p-5 rounded-3xl border text-left flex items-start gap-4 hover:border-brand-500 transition-all"
              style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
            >
              <div className="w-10 h-10 rounded-2xl bg-brand-500/10 text-brand-500 flex items-center justify-center shrink-0">
                <Settings className="w-5 h-5" />
              </div>
              <div>
                <h3 className="text-sm font-bold" style={{ color: 'var(--text-primary)' }}>Customize Branding</h3>
                <p className="text-xs text-slate-400 mt-1">Change Logo text, subtitle, announcements, and navigation links.</p>
              </div>
            </button>

            <button
              onClick={() => { setActiveTab('users'); setShowCreateStaffModal(true); }}
              className="p-5 rounded-3xl border text-left flex items-start gap-4 hover:border-brand-500 transition-all"
              style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
            >
              <div className="w-10 h-10 rounded-2xl bg-emerald-500/10 text-emerald-500 flex items-center justify-center shrink-0">
                <UserPlus className="w-5 h-5" />
              </div>
              <div>
                <h3 className="text-sm font-bold" style={{ color: 'var(--text-primary)' }}>Create Sub-Admin</h3>
                <p className="text-xs text-slate-400 mt-1">Grant custom roles (Admin, ContentEditor, Reviewer, Support).</p>
              </div>
            </button>

            <button
              onClick={() => { setActiveTab('careers'); setShowCareerModal(true); setEditingCareerId(null); setCareerTitle(''); setCareerSummary(''); }}
              className="p-5 rounded-3xl border text-left flex items-start gap-4 hover:border-brand-500 transition-all"
              style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
            >
              <div className="w-10 h-10 rounded-2xl bg-purple-500/10 text-purple-500 flex items-center justify-center shrink-0">
                <Compass className="w-5 h-5" />
              </div>
              <div>
                <h3 className="text-sm font-bold" style={{ color: 'var(--text-primary)' }}>Add New Career</h3>
                <p className="text-xs text-slate-400 mt-1">Publish new high-paying career roadmaps to the catalog.</p>
              </div>
            </button>
          </div>
        </div>
      )}

      {/* ── TAB 2: SITE SETTINGS & BRANDING ── */}
      {activeTab === 'settings' && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 animate-fade-in">
          <form onSubmit={handleSaveSettings} className="lg:col-span-2 space-y-6">
            <div className="p-8 rounded-3xl border space-y-6" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
              <h2 className="text-base font-bold flex items-center gap-2" style={{ color: 'var(--text-primary)' }}>
                <Settings className="w-4 h-4 text-brand-500" />
                <span>Brand Identity & Header Logo</span>
              </h2>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold mb-1 text-slate-400">Logo Main Text</label>
                  <input
                    type="text"
                    required
                    value={logoText}
                    onChange={e => setLogoText(e.target.value)}
                    className="input w-full"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold mb-1 text-slate-400">Logo Subtitle</label>
                  <input
                    type="text"
                    required
                    value={logoSubtitle}
                    onChange={e => setLogoSubtitle(e.target.value)}
                    className="input w-full"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Platform Full Name</label>
                <input
                  type="text"
                  required
                  value={siteName}
                  onChange={e => setSiteName(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Tagline / Hero Description</label>
                <input
                  type="text"
                  value={tagline}
                  onChange={e => setTagline(e.target.value)}
                  className="input w-full"
                />
              </div>
            </div>

            {/* Announcement Banner */}
            <div className="p-8 rounded-3xl border space-y-4" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
              <div className="flex items-center justify-between">
                <h2 className="text-base font-bold flex items-center gap-2" style={{ color: 'var(--text-primary)' }}>
                  <Megaphone className="w-4 h-4 text-amber-500" />
                  <span>Top Announcement Banner</span>
                </h2>
                <label className="relative inline-flex items-center cursor-pointer">
                  <input
                    type="checkbox"
                    checked={announcementActive}
                    onChange={e => setAnnouncementActive(e.target.checked)}
                    className="sr-only peer"
                  />
                  <div className="w-11 h-6 bg-slate-200 peer-focus:outline-none rounded-full peer dark:bg-slate-700 peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-slate-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-brand-500"></div>
                </label>
              </div>

              <input
                type="text"
                value={announcementText}
                onChange={e => setAnnouncementText(e.target.value)}
                placeholder="Enter alert text to display at the top of every page..."
                className="input w-full"
              />
            </div>

            {/* Contact & Footer Details */}
            <div className="p-8 rounded-3xl border space-y-4" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
              <h2 className="text-base font-bold flex items-center gap-2" style={{ color: 'var(--text-primary)' }}>
                <Globe className="w-4 h-4 text-emerald-500" />
                <span>Contact & Footer Info</span>
              </h2>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold mb-1 text-slate-400">Support Email</label>
                  <input
                    type="email"
                    value={supportEmail}
                    onChange={e => setSupportEmail(e.target.value)}
                    className="input w-full"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold mb-1 text-slate-400">Support WhatsApp / Phone</label>
                  <input
                    type="text"
                    value={supportPhone}
                    onChange={e => setSupportPhone(e.target.value)}
                    className="input w-full"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Footer Text</label>
                <input
                  type="text"
                  value={footerText}
                  onChange={e => setFooterText(e.target.value)}
                  className="input w-full"
                />
              </div>
            </div>

            <button
              type="submit"
              className="btn-brand py-3.5 px-8 rounded-2xl flex items-center gap-2 text-xs font-bold shadow-brand"
            >
              <Save className="w-4 h-4" />
              <span>Save & Publish All Branding Changes</span>
            </button>
          </form>

          {/* Navigation Menus Editor */}
          <div className="space-y-6">
            <div className="p-8 rounded-3xl border space-y-4" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
              <h2 className="text-base font-bold flex items-center gap-2" style={{ color: 'var(--text-primary)' }}>
                <Menu className="w-4 h-4 text-indigo-500" />
                <span>Header Navigation Menus</span>
              </h2>
              <p className="text-xs text-slate-400">Add, reorder, or toggle navigation items appearing on the top navbar.</p>

              {/* Menu Items List */}
              <div className="space-y-2">
                {navMenus.map((menu, idx) => (
                  <div
                    key={idx}
                    className="p-3 rounded-2xl border flex items-center justify-between gap-2"
                    style={{ backgroundColor: 'var(--bg-app)', borderColor: 'var(--border-color)' }}
                  >
                    <div>
                      <span className="text-xs font-bold" style={{ color: 'var(--text-primary)' }}>{menu.label}</span>
                      <span className="text-[10px] text-slate-400 ml-2">({menu.href})</span>
                    </div>

                    <div className="flex items-center gap-2">
                      <button
                        type="button"
                        onClick={() => handleToggleNavMenu(idx)}
                        className={`text-[10px] font-bold px-2 py-1 rounded-lg border ${
                          menu.isActive !== false ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20' : 'bg-slate-500/10 text-slate-400 border-slate-500/20'
                        }`}
                      >
                        {menu.isActive !== false ? 'Active' : 'Hidden'}
                      </button>
                      <button
                        type="button"
                        onClick={() => handleDeleteNavMenu(idx)}
                        className="text-slate-400 hover:text-red-500 p-1"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>

              {/* Add New Menu Form */}
              <div className="pt-4 border-t space-y-2" style={{ borderColor: 'var(--border-color)' }}>
                <input
                  type="text"
                  placeholder="Menu Label (e.g. Colleges)"
                  value={newMenuLabel}
                  onChange={e => setNewMenuLabel(e.target.value)}
                  className="input w-full text-xs"
                />
                <input
                  type="text"
                  placeholder="URL Path (e.g. /colleges)"
                  value={newMenuHref}
                  onChange={e => setNewMenuHref(e.target.value)}
                  className="input w-full text-xs"
                />
                <button
                  type="button"
                  onClick={handleAddNavMenu}
                  disabled={!newMenuLabel || !newMenuHref}
                  className="w-full btn-secondary py-2.5 rounded-xl text-xs font-semibold flex items-center justify-center gap-1.5"
                >
                  <Plus className="w-3.5 h-3.5" />
                  <span>Add Menu Link</span>
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ── TAB 3: STAFF & ROLES ── */}
      {activeTab === 'users' && (
        <div className="space-y-6 animate-fade-in">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <input
              type="text"
              placeholder="Search user by email or name..."
              value={userSearch}
              onChange={e => setUserSearch(e.target.value)}
              className="input max-w-sm"
            />
            <button
              onClick={() => setShowCreateStaffModal(true)}
              className="btn-brand px-4 py-2.5 rounded-2xl text-xs font-bold flex items-center gap-2 shadow-brand"
            >
              <UserPlus className="w-4 h-4" />
              <span>Create New Staff / Admin</span>
            </button>
          </div>

          <div className="overflow-x-auto rounded-3xl border" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
            <table className="w-full text-left text-xs">
              <thead className="border-b text-slate-400 font-semibold" style={{ borderColor: 'var(--border-color)' }}>
                <tr>
                  <th className="p-4">User</th>
                  <th className="p-4">Assigned Roles</th>
                  <th className="p-4">Status</th>
                  <th className="p-4">Joined Date</th>
                  <th className="p-4 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y" style={{ borderColor: 'var(--border-color)' }}>
                {usersList
                  .filter(u => !userSearch || u.email.toLowerCase().includes(userSearch.toLowerCase()) || (u.displayName && u.displayName.toLowerCase().includes(userSearch.toLowerCase())))
                  .map(u => (
                    <tr key={u.id} className="hover:bg-slate-500/5 transition-colors">
                      <td className="p-4">
                        <p className="font-bold" style={{ color: 'var(--text-primary)' }}>{u.displayName || 'Student'}</p>
                        <p className="text-slate-400">{u.email}</p>
                      </td>
                      <td className="p-4">
                        <div className="flex flex-wrap gap-1">
                          {(u.roles || ['Student']).map((r: string) => (
                            <span key={r} className={`px-2.5 py-0.5 rounded-full text-[10px] font-bold ${
                              ['Admin', 'SuperAdmin'].includes(r) ? 'bg-purple-500/10 text-purple-500' :
                              r === 'ContentEditor' ? 'bg-indigo-500/10 text-indigo-500' :
                              r === 'Reviewer' ? 'bg-amber-500/10 text-amber-500' : 'bg-slate-500/10 text-slate-400'
                            }`}>
                              {r}
                            </span>
                          ))}
                        </div>
                      </td>
                      <td className="p-4">
                        <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${u.isActive !== false ? 'text-emerald-500 bg-emerald-500/10' : 'text-red-500 bg-red-500/10'}`}>
                          {u.isActive !== false ? 'Active' : 'Suspended'}
                        </span>
                      </td>
                      <td className="p-4 text-slate-400">
                        {new Date(u.createdAt).toLocaleDateString()}
                      </td>
                      <td className="p-4 text-right">
                        <button
                          onClick={async () => {
                            await api.toggleUserSuspension(u.id, u.isActive === false)
                            loadData()
                          }}
                          className="text-xs font-semibold text-slate-400 hover:text-brand-500"
                        >
                          {u.isActive !== false ? 'Suspend' : 'Reactivate'}
                        </button>
                      </td>
                    </tr>
                  ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* ── TAB 4: CAREERS CMS ── */}
      {activeTab === 'careers' && (
        <div className="space-y-6 animate-fade-in">
          <div className="flex items-center justify-between">
            <p className="text-xs text-slate-400">Manage all career roadmaps, salary labels, and featured cards.</p>
            <button
              onClick={() => {
                setEditingCareerId(null)
                setCareerTitle('')
                setCareerSlug('')
                setCareerSummary('')
                setCareerSalary('₹6 - 18 LPA')
                setCareerIsFeatured(true)
                setShowCareerModal(true)
              }}
              className="btn-brand px-4 py-2.5 rounded-2xl text-xs font-bold flex items-center gap-2 shadow-brand"
            >
              <Plus className="w-4 h-4" />
              <span>Create New Career</span>
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {careersList.map(c => (
              <div
                key={c.id}
                className="p-6 rounded-3xl border flex flex-col justify-between"
                style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
              >
                <div>
                  <div className="flex items-center justify-between mb-2">
                    <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-brand-500/10 text-brand-500">
                      {c.salaryRangeLabel || '₹5 - 15 LPA'}
                    </span>
                    {c.isFeatured && (
                      <span className="text-[10px] text-amber-500 font-bold">★ Featured</span>
                    )}
                  </div>
                  <h3 className="text-base font-bold mb-1" style={{ color: 'var(--text-primary)' }}>{c.title}</h3>
                  <p className="text-xs text-slate-400 line-clamp-2">{c.summary}</p>
                </div>

                <div className="flex items-center justify-end gap-2 mt-6 pt-4 border-t" style={{ borderColor: 'var(--border-color)' }}>
                  <button
                    onClick={() => {
                      setEditingCareerId(c.id)
                      setCareerTitle(c.title)
                      setCareerSlug(c.slug)
                      setCareerSummary(c.summary)
                      setCareerSalary(c.salaryRangeLabel || '₹6 - 18 LPA')
                      setCareerIsFeatured(c.isFeatured)
                      setShowCareerModal(true)
                    }}
                    className="p-2 rounded-xl text-slate-400 hover:text-brand-500 hover:bg-brand-500/10 transition-colors"
                  >
                    <Edit3 className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => handleDeleteCareer(c.id)}
                    className="p-2 rounded-xl text-slate-400 hover:text-red-500 hover:bg-red-500/10 transition-colors"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── TAB 5: EXAMS CMS ── */}
      {activeTab === 'exams' && (
        <div className="space-y-6 animate-fade-in">
          <div className="flex items-center justify-between">
            <p className="text-xs text-slate-400">Manage all national and state-level competitive exams.</p>
            <button
              onClick={() => {
                setEditingExamId(null)
                setExamName('')
                setExamCode('')
                setExamLevel('National')
                setExamUrl('')
                setExamEligibility('')
                setShowExamModal(true)
              }}
              className="btn-brand px-4 py-2.5 rounded-2xl text-xs font-bold flex items-center gap-2 shadow-brand"
            >
              <Plus className="w-4 h-4" />
              <span>Create New Exam</span>
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {examsList.map(e => (
              <div
                key={e.id}
                className="p-6 rounded-3xl border flex flex-col justify-between"
                style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
              >
                <div>
                  <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-indigo-500/10 text-indigo-500">
                    {e.level || 'National'}
                  </span>
                  <h3 className="text-base font-bold mt-2" style={{ color: 'var(--text-primary)' }}>{e.name}</h3>
                  <p className="text-xs text-slate-400 mt-1">{e.eligibilitySummary || '10+2 / Graduation'}</p>
                </div>

                <div className="flex items-center justify-end gap-2 mt-6 pt-4 border-t" style={{ borderColor: 'var(--border-color)' }}>
                  <button
                    onClick={() => {
                      setEditingExamId(e.id)
                      setExamName(e.name)
                      setExamCode(e.code)
                      setExamLevel(e.level)
                      setExamUrl(e.websiteUrl || '')
                      setExamEligibility(e.eligibilitySummary || '')
                      setShowExamModal(true)
                    }}
                    className="p-2 rounded-xl text-slate-400 hover:text-brand-500 hover:bg-brand-500/10 transition-colors"
                  >
                    <Edit3 className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => handleDeleteExam(e.id)}
                    className="p-2 rounded-xl text-slate-400 hover:text-red-500 hover:bg-red-500/10 transition-colors"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── TAB 6: COURSES CMS ── */}
      {activeTab === 'courses' && (
        <div className="space-y-6 animate-fade-in">
          <div className="flex items-center justify-between">
            <p className="text-xs text-slate-400">Manage undergraduate, postgraduate, and diploma degrees.</p>
            <button
              onClick={() => {
                setEditingCourseId(null)
                setCourseName('')
                setCourseSlug('')
                setCourseDegree('UG')
                setCourseDuration(4)
                setCourseEligibility('10+2 with Science / Commerce / Arts')
                setShowCourseModal(true)
              }}
              className="btn-brand px-4 py-2.5 rounded-2xl text-xs font-bold flex items-center gap-2 shadow-brand"
            >
              <Plus className="w-4 h-4" />
              <span>Create New Course</span>
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {coursesList.map(c => (
              <div
                key={c.id}
                className="p-6 rounded-3xl border flex flex-col justify-between"
                style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
              >
                <div>
                  <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-purple-500/10 text-purple-500">
                    {c.degreeLevel || 'UG'} • {c.durationYears} Years
                  </span>
                  <h3 className="text-base font-bold mt-2" style={{ color: 'var(--text-primary)' }}>{c.name}</h3>
                  <p className="text-xs text-slate-400 mt-1">{c.eligibilityCriteria || '10+2 Pass'}</p>
                </div>

                <div className="flex items-center justify-end gap-2 mt-6 pt-4 border-t" style={{ borderColor: 'var(--border-color)' }}>
                  <button
                    onClick={() => {
                      setEditingCourseId(c.id)
                      setCourseName(c.name)
                      setCourseSlug(c.slug)
                      setCourseDegree(c.degreeLevel)
                      setCourseDuration(c.durationYears)
                      setCourseEligibility(c.eligibilityCriteria || '')
                      setShowCourseModal(true)
                    }}
                    className="p-2 rounded-xl text-slate-400 hover:text-brand-500 hover:bg-brand-500/10 transition-colors"
                  >
                    <Edit3 className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => handleDeleteCourse(c.id)}
                    className="p-2 rounded-xl text-slate-400 hover:text-red-500 hover:bg-red-500/10 transition-colors"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── TAB 7: COUPONS ── */}
      {activeTab === 'coupons' && (
        <div className="space-y-6 animate-fade-in">
          <div className="flex items-center justify-between">
            <p className="text-xs text-slate-400">Manage promotional discount vouchers and gift codes.</p>
            <button
              onClick={() => setShowNewCoupon(true)}
              className="btn-brand px-4 py-2.5 rounded-2xl text-xs font-bold flex items-center gap-2 shadow-brand"
            >
              <Plus className="w-4 h-4" />
              <span>Create New Coupon</span>
            </button>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {couponsList.map(c => (
              <div
                key={c.id}
                className="p-6 rounded-3xl border relative"
                style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
              >
                <div className="flex items-center justify-between mb-3">
                  <span className="font-mono text-sm font-bold text-brand-500 bg-brand-500/10 px-3 py-1 rounded-xl">
                    {c.code}
                  </span>
                  <span className={`text-[10px] font-bold px-2 py-0.5 rounded-full ${c.isActive ? 'bg-emerald-500/10 text-emerald-500' : 'bg-slate-500/10 text-slate-400'}`}>
                    {c.isActive ? 'Active' : 'Disabled'}
                  </span>
                </div>

                <p className="text-xs font-medium" style={{ color: 'var(--text-primary)' }}>{c.description || 'Special Discount'}</p>
                <p className="text-lg font-bold mt-2" style={{ color: 'var(--text-primary)' }}>
                  {c.discountType === 'Percentage' ? `${c.discountValue}% OFF` : `₹${c.discountValue} FLAT OFF`}
                </p>

                <div className="flex items-center justify-between mt-4 pt-4 border-t text-[11px] text-slate-400" style={{ borderColor: 'var(--border-color)' }}>
                  <span>Used: {c.timesRedeemed ?? 0} times</span>
                  <button
                    onClick={async () => {
                      await api.toggleAdminCoupon(c.id, { isActive: !c.isActive })
                      loadData()
                    }}
                    className="text-brand-500 font-bold hover:underline"
                  >
                    {c.isActive ? 'Disable' : 'Enable'}
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── TAB 8: KNOWLEDGE BASE & CHUNKS ── */}
      {activeTab === 'knowledge' && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 animate-fade-in">
          {/* Documents List */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-sm font-bold" style={{ color: 'var(--text-primary)' }}>Knowledge Documents</h2>
              <button
                onClick={() => setShowNewDocModal(true)}
                className="btn-brand p-2 rounded-xl text-xs font-bold flex items-center gap-1 shadow-brand"
              >
                <Plus className="w-3.5 h-3.5" />
                <span>Upload</span>
              </button>
            </div>

            <div className="space-y-3">
              {documents.map(doc => (
                <div
                  key={doc.id}
                  onClick={async () => {
                    const detail = await api.getDocumentDetail(doc.id)
                    setSelectedDoc(detail)
                  }}
                  className={`p-4 rounded-2xl border cursor-pointer transition-all ${
                    selectedDoc?.id === doc.id ? 'border-brand-500 ring-2 ring-brand-500/20' : 'hover:border-slate-400'
                  }`}
                  style={{ backgroundColor: 'var(--card-bg)', borderColor: selectedDoc?.id === doc.id ? undefined : 'var(--border-color)' }}
                >
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-indigo-500/10 text-indigo-500">{doc.docType}</span>
                    <span className="text-[10px] text-emerald-500 font-bold">{doc.status}</span>
                  </div>
                  <h4 className="text-xs font-bold line-clamp-1" style={{ color: 'var(--text-primary)' }}>{doc.title}</h4>
                  <p className="text-[10px] text-slate-400 mt-1">{(doc.fileSize / 1024).toFixed(1)} KB • {new Date(doc.createdAt).toLocaleDateString()}</p>
                </div>
              ))}
            </div>
          </div>

          {/* Chunks Inspector & Editor */}
          <div className="lg:col-span-2 space-y-4">
            {selectedDoc ? (
              <div className="p-8 rounded-3xl border space-y-6" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
                <div className="flex items-center justify-between pb-4 border-b" style={{ borderColor: 'var(--border-color)' }}>
                  <div>
                    <h3 className="text-base font-bold" style={{ color: 'var(--text-primary)' }}>{selectedDoc.title}</h3>
                    <p className="text-xs text-slate-400 mt-0.5">Type: {selectedDoc.docType} • Status: {selectedDoc.status}</p>
                  </div>
                  <button
                    onClick={async () => {
                      if (confirm('Delete this knowledge document and its chunks?')) {
                        await api.deleteKnowledgeDocument(selectedDoc.id)
                        setSelectedDoc(null)
                        loadData()
                      }
                    }}
                    className="text-red-500 hover:bg-red-500/10 p-2 rounded-xl text-xs flex items-center gap-1"
                  >
                    <Trash2 className="w-4 h-4" />
                    <span>Delete Doc</span>
                  </button>
                </div>

                <div className="space-y-4">
                  <h4 className="text-xs font-bold text-slate-400">Indexed Text Chunks for RAG</h4>
                  {selectedDoc.chunks?.map((c: any) => (
                    <div key={c.id} className="p-4 rounded-2xl border space-y-2" style={{ backgroundColor: 'var(--bg-app)', borderColor: 'var(--border-color)' }}>
                      <div className="flex items-center justify-between text-[11px] text-slate-400">
                        <span>Chunk #{c.chunkIndex + 1} ({c.tokenCount} tokens)</span>
                        <span className="text-emerald-500 font-bold">✓ Indexed</span>
                      </div>

                      {editingChunkId === c.id ? (
                        <div className="space-y-2">
                          <textarea
                            rows={4}
                            value={editingChunkContent}
                            onChange={e => setEditingChunkContent(e.target.value)}
                            className="input w-full text-xs font-mono"
                          />
                          <div className="flex gap-2">
                            <button
                              onClick={() => handleSaveChunk(c.id)}
                              className="btn-brand px-3 py-1.5 rounded-xl text-xs font-bold"
                            >
                              Save Chunk
                            </button>
                            <button
                              onClick={() => setEditingChunkId(null)}
                              className="btn-secondary px-3 py-1.5 rounded-xl text-xs"
                            >
                              Cancel
                            </button>
                          </div>
                        </div>
                      ) : (
                        <div>
                          <p className="text-xs leading-relaxed" style={{ color: 'var(--text-primary)' }}>{c.content}</p>
                          <button
                            onClick={() => { setEditingChunkId(c.id); setEditingChunkContent(c.content); }}
                            className="text-[11px] text-brand-500 font-bold mt-2 hover:underline inline-flex items-center gap-1"
                          >
                            <Edit3 className="w-3 h-3" /> Edit Chunk Content
                          </button>
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            ) : (
              <div className="p-12 rounded-3xl border text-center" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
                <FileText className="w-12 h-12 text-slate-400 mx-auto mb-3 opacity-40" />
                <h4 className="text-sm font-bold" style={{ color: 'var(--text-primary)' }}>Select a Document</h4>
                <p className="text-xs text-slate-400 mt-1">Click any knowledge document on the left to inspect and edit chunks used by the AI chatbot.</p>
              </div>
            )}
          </div>
        </div>
      )}

      {/* ── TAB 9: EDITORIAL QUEUE ── */}
      {activeTab === 'editorial' && (
        <div className="space-y-6 animate-fade-in">
          <div className="flex items-center justify-between">
            <p className="text-xs text-slate-400">Review guidance articles submitted by academic counselors before publishing live.</p>
            <button
              onClick={() => setShowNewArticleModal(true)}
              className="btn-brand px-4 py-2.5 rounded-2xl text-xs font-bold flex items-center gap-2 shadow-brand"
            >
              <Plus className="w-4 h-4" />
              <span>Draft New Article</span>
            </button>
          </div>

          <div className="space-y-4">
            {articles.map(art => (
              <div
                key={art.id}
                className="p-6 rounded-3xl border flex flex-col md:flex-row md:items-center justify-between gap-4"
                style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
              >
                <div>
                  <div className="flex items-center gap-2 mb-1.5">
                    <span className={`px-2.5 py-0.5 rounded-full text-[10px] font-bold ${
                      art.status === 'Published' ? 'bg-emerald-500/10 text-emerald-500' : 'bg-amber-500/10 text-amber-500'
                    }`}>
                      {art.status}
                    </span>
                    <span className="text-[11px] text-slate-400">By {art.authorName || 'Editorial Team'}</span>
                  </div>
                  <h3 className="text-base font-bold" style={{ color: 'var(--text-primary)' }}>{art.title}</h3>
                  <p className="text-xs text-slate-400 mt-1">{art.summary}</p>
                </div>

                <div className="flex items-center gap-2 shrink-0">
                  {art.status !== 'Published' && (
                    <button
                      onClick={() => handlePublishArticle(art.id)}
                      className="btn-brand px-4 py-2 rounded-xl text-xs font-bold flex items-center gap-1.5 shadow-brand"
                    >
                      <CheckCircle className="w-4 h-4" />
                      <span>Approve & Publish</span>
                    </button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── TAB 10: BULK IMPORTS ── */}
      {activeTab === 'imports' && (
        <div className="space-y-6 animate-fade-in">
          <div className="flex items-center justify-between">
            <p className="text-xs text-slate-400">Sync all-India state boards, entrance exam notifications, and catalog updates.</p>
            <button
              onClick={handleTriggerImport}
              className="btn-brand px-4 py-2.5 rounded-2xl text-xs font-bold flex items-center gap-2 shadow-brand"
            >
              <Upload className="w-4 h-4" />
              <span>Run Catalog Import Batch</span>
            </button>
          </div>

          <div className="space-y-4">
            {importJobs.map(job => (
              <div
                key={job.id}
                className="p-6 rounded-3xl border flex items-center justify-between gap-4"
                style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}
              >
                <div>
                  <div className="flex items-center gap-2 mb-1">
                    <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-emerald-500/10 text-emerald-500">
                      {job.status}
                    </span>
                    <span className="text-[11px] text-slate-400">{new Date(job.createdAt).toLocaleString()}</span>
                  </div>
                  <h3 className="text-sm font-bold" style={{ color: 'var(--text-primary)' }}>{job.sourceType}</h3>
                  <p className="text-xs text-slate-400 mt-1">Processed: {job.importedRecords ?? job.totalRecords} records • 0 Errors</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── MODAL: CREATE STAFF ── */}
      {showCreateStaffModal && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="w-full max-w-md p-8 rounded-3xl border shadow-2xl space-y-6" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
            <div className="flex items-center justify-between">
              <h3 className="text-base font-bold" style={{ color: 'var(--text-primary)' }}>Create New Staff / Admin</h3>
              <button onClick={() => setShowCreateStaffModal(false)}><X className="w-4 h-4 text-slate-400" /></button>
            </div>

            <form onSubmit={handleCreateStaff} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Full Name</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Rahul Sharma"
                  value={staffName}
                  onChange={e => setStaffName(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Staff Email</label>
                <input
                  type="email"
                  required
                  placeholder="staff@careerpathbharat.com"
                  value={staffEmail}
                  onChange={e => setStaffEmail(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Password</label>
                <input
                  type="password"
                  required
                  placeholder="Min 8 chars, 1 uppercase & 1 digit"
                  value={staffPassword}
                  onChange={e => setStaffPassword(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Role & Access Level</label>
                <select
                  value={staffRole}
                  onChange={e => setStaffRole(e.target.value)}
                  className="input w-full cursor-pointer"
                >
                  <option value="Admin">Admin (Full Control)</option>
                  <option value="ContentEditor">ContentEditor (Careers, Exams & Articles)</option>
                  <option value="Reviewer">Reviewer (Approve & Verify Chunks)</option>
                  <option value="FinanceAdmin">FinanceAdmin (Coupons & Subscriptions)</option>
                  <option value="Support">Support (Student Inquiries)</option>
                </select>
              </div>

              <button
                type="submit"
                className="w-full btn-brand py-3.5 rounded-2xl text-xs font-bold shadow-brand mt-4"
              >
                Create Staff Account
              </button>
            </form>
          </div>
        </div>
      )}

      {/* ── MODAL: CAREER EDIT/CREATE ── */}
      {showCareerModal && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="w-full max-w-lg p-8 rounded-3xl border shadow-2xl space-y-6" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
            <div className="flex items-center justify-between">
              <h3 className="text-base font-bold" style={{ color: 'var(--text-primary)' }}>
                {editingCareerId ? 'Edit Career' : 'Create New Career'}
              </h3>
              <button onClick={() => setShowCareerModal(false)}><X className="w-4 h-4 text-slate-400" /></button>
            </div>

            <form onSubmit={handleSaveCareer} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Career Title</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Artificial Intelligence Engineer"
                  value={careerTitle}
                  onChange={e => setCareerTitle(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Indicative Salary Range</label>
                <input
                  type="text"
                  placeholder="e.g. ₹8 - 25 LPA"
                  value={careerSalary}
                  onChange={e => setCareerSalary(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Summary & Overview</label>
                <textarea
                  rows={3}
                  required
                  placeholder="Brief summary of skills, responsibilities, and future growth..."
                  value={careerSummary}
                  onChange={e => setCareerSummary(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="featured"
                  checked={careerIsFeatured}
                  onChange={e => setCareerIsFeatured(e.target.checked)}
                  className="rounded"
                />
                <label htmlFor="featured" className="text-xs font-semibold text-slate-400 cursor-pointer">
                  Feature on Homepage
                </label>
              </div>

              <button
                type="submit"
                className="w-full btn-brand py-3.5 rounded-2xl text-xs font-bold shadow-brand mt-4"
              >
                Save Career Profile
              </button>
            </form>
          </div>
        </div>
      )}

      {/* ── MODAL: EXAM EDIT/CREATE ── */}
      {showExamModal && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="w-full max-w-lg p-8 rounded-3xl border shadow-2xl space-y-6" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
            <div className="flex items-center justify-between">
              <h3 className="text-base font-bold" style={{ color: 'var(--text-primary)' }}>
                {editingExamId ? 'Edit Entrance Exam' : 'Create Entrance Exam'}
              </h3>
              <button onClick={() => setShowExamModal(false)}><X className="w-4 h-4 text-slate-400" /></button>
            </div>

            <form onSubmit={handleSaveExam} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Exam Full Name</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. JEE Advanced 2026"
                  value={examName}
                  onChange={e => setExamName(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold mb-1 text-slate-400">Exam Code</label>
                  <input
                    type="text"
                    placeholder="e.g. JEE-ADV"
                    value={examCode}
                    onChange={e => setExamCode(e.target.value)}
                    className="input w-full"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold mb-1 text-slate-400">Level</label>
                  <select
                    value={examLevel}
                    onChange={e => setExamLevel(e.target.value)}
                    className="input w-full"
                  >
                    <option value="National">National</option>
                    <option value="State">State</option>
                    <option value="International">International</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Official Portal URL</label>
                <input
                  type="url"
                  placeholder="https://jeeadv.ac.in"
                  value={examUrl}
                  onChange={e => setExamUrl(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Eligibility Summary</label>
                <input
                  type="text"
                  placeholder="10+2 with top 2.5 lakh qualifiers in JEE Main"
                  value={examEligibility}
                  onChange={e => setExamEligibility(e.target.value)}
                  className="input w-full"
                />
              </div>

              <button
                type="submit"
                className="w-full btn-brand py-3.5 rounded-2xl text-xs font-bold shadow-brand mt-4"
              >
                Save Exam
              </button>
            </form>
          </div>
        </div>
      )}

      {/* ── MODAL: COURSE EDIT/CREATE ── */}
      {showCourseModal && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="w-full max-w-lg p-8 rounded-3xl border shadow-2xl space-y-6" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
            <div className="flex items-center justify-between">
              <h3 className="text-base font-bold" style={{ color: 'var(--text-primary)' }}>
                {editingCourseId ? 'Edit Course / Degree' : 'Create Course / Degree'}
              </h3>
              <button onClick={() => setShowCourseModal(false)}><X className="w-4 h-4 text-slate-400" /></button>
            </div>

            <form onSubmit={handleSaveCourse} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Degree / Course Name</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. B.Tech Computer Science"
                  value={courseName}
                  onChange={e => setCourseName(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold mb-1 text-slate-400">Degree Level</label>
                  <select
                    value={courseDegree}
                    onChange={e => setCourseDegree(e.target.value)}
                    className="input w-full"
                  >
                    <option value="UG">Undergraduate (UG)</option>
                    <option value="PG">Postgraduate (PG)</option>
                    <option value="Diploma">Diploma / Certification</option>
                    <option value="Doctorate">Doctorate / PhD</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-semibold mb-1 text-slate-400">Duration (Years)</label>
                  <input
                    type="number"
                    min={1}
                    max={6}
                    value={courseDuration}
                    onChange={e => setCourseDuration(Number(e.target.value))}
                    className="input w-full"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Eligibility Criteria</label>
                <input
                  type="text"
                  placeholder="10+2 with Physics, Chemistry, Mathematics"
                  value={courseEligibility}
                  onChange={e => setCourseEligibility(e.target.value)}
                  className="input w-full"
                />
              </div>

              <button
                type="submit"
                className="w-full btn-brand py-3.5 rounded-2xl text-xs font-bold shadow-brand mt-4"
              >
                Save Course
              </button>
            </form>
          </div>
        </div>
      )}

      {/* ── MODAL: COUPON CREATE ── */}
      {showNewCoupon && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="w-full max-w-md p-8 rounded-3xl border shadow-2xl space-y-6" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
            <div className="flex items-center justify-between">
              <h3 className="text-base font-bold" style={{ color: 'var(--text-primary)' }}>Create Promotional Coupon</h3>
              <button onClick={() => setShowNewCoupon(false)}><X className="w-4 h-4 text-slate-400" /></button>
            </div>

            <form onSubmit={handleCreateCoupon} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Promo Code</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. DIWALI50"
                  value={newCouponCode}
                  onChange={e => setNewCouponCode(e.target.value)}
                  className="input w-full font-mono uppercase"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Description</label>
                <input
                  type="text"
                  placeholder="50% Special Festive Offer for Indian Students"
                  value={newCouponDesc}
                  onChange={e => setNewCouponDesc(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold mb-1 text-slate-400">Discount Type</label>
                  <select
                    value={newCouponType}
                    onChange={e => setNewCouponType(e.target.value as any)}
                    className="input w-full"
                  >
                    <option value="Percentage">Percentage (%)</option>
                    <option value="FixedAmount">Flat Rupees (₹)</option>
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-semibold mb-1 text-slate-400">Discount Value</label>
                  <input
                    type="number"
                    min={1}
                    value={newCouponValue}
                    onChange={e => setNewCouponValue(Number(e.target.value))}
                    className="input w-full"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold mb-1 text-slate-400">Min Plan Price (₹)</label>
                  <input
                    type="number"
                    value={newCouponMinPrice}
                    onChange={e => setNewCouponMinPrice(Number(e.target.value))}
                    className="input w-full"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold mb-1 text-slate-400">Max Redemptions</label>
                  <input
                    type="number"
                    value={newCouponMaxRedemptions}
                    onChange={e => setNewCouponMaxRedemptions(Number(e.target.value))}
                    className="input w-full"
                  />
                </div>
              </div>

              <button
                type="submit"
                className="w-full btn-brand py-3.5 rounded-2xl text-xs font-bold shadow-brand mt-4"
              >
                Create Promo Coupon
              </button>
            </form>
          </div>
        </div>
      )}

      {/* ── MODAL: KNOWLEDGE DOC CREATE ── */}
      {showNewDocModal && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="w-full max-w-lg p-8 rounded-3xl border shadow-2xl space-y-6" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
            <div className="flex items-center justify-between">
              <h3 className="text-base font-bold" style={{ color: 'var(--text-primary)' }}>Add Knowledge Document for AI</h3>
              <button onClick={() => setShowNewDocModal(false)}><X className="w-4 h-4 text-slate-400" /></button>
            </div>

            <form onSubmit={handleCreateKnowledgeDoc} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Document Title</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. NEET UG 2026 Biology Pattern & Syllabus"
                  value={docTitle}
                  onChange={e => setDocTitle(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Document Type</label>
                <select
                  value={docType}
                  onChange={e => setDocType(e.target.value)}
                  className="input w-full"
                >
                  <option value="Syllabus">Syllabus</option>
                  <option value="ExamNotification">Exam Notification</option>
                  <option value="CareerGuideline">Career Guideline</option>
                  <option value="Policy">Policy / Reservation</option>
                </select>
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Document Content / Chunks (Separate paragraphs by double enter)</label>
                <textarea
                  rows={6}
                  required
                  placeholder="Enter detailed facts, syllabus points, eligibility criteria, or career guidelines..."
                  value={docChunksText}
                  onChange={e => setDocChunksText(e.target.value)}
                  className="input w-full text-xs font-mono"
                />
              </div>

              <button
                type="submit"
                className="w-full btn-brand py-3.5 rounded-2xl text-xs font-bold shadow-brand mt-4"
              >
                Index Document for AI Chatbot
              </button>
            </form>
          </div>
        </div>
      )}

      {/* ── MODAL: EDITORIAL DRAFT ── */}
      {showNewArticleModal && (
        <div className="fixed inset-0 z-50 bg-black/60 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="w-full max-w-xl p-8 rounded-3xl border shadow-2xl space-y-6" style={{ backgroundColor: 'var(--card-bg)', borderColor: 'var(--border-color)' }}>
            <div className="flex items-center justify-between">
              <h3 className="text-base font-bold" style={{ color: 'var(--text-primary)' }}>Draft Guidance Article</h3>
              <button onClick={() => setShowNewArticleModal(false)}><X className="w-4 h-4 text-slate-400" /></button>
            </div>

            <form onSubmit={handleCreateArticle} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Article Title</label>
                <input
                  type="text"
                  required
                  placeholder="e.g. Top 10 High Growth Careers in Green Energy for 2026"
                  value={articleTitle}
                  onChange={e => setArticleTitle(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Summary</label>
                <input
                  type="text"
                  required
                  placeholder="Brief 1-2 sentence overview..."
                  value={articleSummary}
                  onChange={e => setArticleSummary(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Author Name</label>
                <input
                  type="text"
                  placeholder="e.g. Dr. Priya Nair, Academic Counselor"
                  value={articleAuthor}
                  onChange={e => setArticleAuthor(e.target.value)}
                  className="input w-full"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold mb-1 text-slate-400">Full Article Content (Markdown)</label>
                <textarea
                  rows={6}
                  required
                  placeholder="Write your comprehensive guidance article here..."
                  value={articleBody}
                  onChange={e => setArticleBody(e.target.value)}
                  className="input w-full text-xs font-mono"
                />
              </div>

              <button
                type="submit"
                className="w-full btn-brand py-3.5 rounded-2xl text-xs font-bold shadow-brand mt-4"
              >
                Submit to Editorial Queue
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
