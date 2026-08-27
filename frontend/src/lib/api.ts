/* eslint-disable @typescript-eslint/no-explicit-any */
const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'https://careerpath-api-bharat-gqbngkhmhbhzdrb8.centralindia-01.azurewebsites.net';

async function apiFetch<T>(path: string, options?: RequestInit): Promise<T> {
  const token = typeof window !== 'undefined' ? localStorage.getItem('access_token') : null;
  const res = await fetch(`${API_BASE}${path}`, {
    cache: 'no-store',
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options?.headers,
    },
  });

  if (!res.ok) {
    const err = await res.json().catch(() => ({ title: res.statusText }));
    const errorMsg = err.detail || err.message || err.title || (err.errors ? Object.values(err.errors).flat().join(', ') : null) || `HTTP ${res.status}: ${res.statusText}`;
    throw new Error(errorMsg);
  }

  if (res.status === 204) return undefined as unknown as T;
  const text = await res.text();
  return text ? JSON.parse(text) : (undefined as unknown as T);
}

// ── Types ─────────────────────────────────────────────────────────────────────

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CategoryDto {
  id: string;
  name: string;
  parentId: string | null;
  sortOrder: number;
}

export interface CareerSummary {
  id: string;
  slug: string;
  title: string;
  summary: string;
  categoryId: string;
  imageUrl: string | null;
  isFeatured: boolean;
  salaryRangeLabel: string | null;
  publishedAt: string;
}

export interface SkillDto {
  id: number;
  name: string;
  slug: string;
  category: string;
}

export interface ExamDto {
  id: number;
  slug: string;
  name: string;
  fullName: string | null;
  conductingBody: string | null;
  level: string | null;
  frequency: string | null;
  description: string | null;
  officialUrl: string | null;
}

export interface CourseDto {
  id: number;
  slug: string;
  name: string;
  shortName: string | null;
  degreeLevel: string;
  durationYears: number;
  categoryId: string | null;
  description: string | null;
}

export interface ScholarshipDto {
  id: number;
  slug: string;
  name: string;
  providerName: string;
  level: string | null;
  amountLabel: string | null;
  eligibilitySummary: string | null;
  officialUrl: string | null;
  disclaimer: string | null;
}

export interface CareerDetail {
  id: string;
  slug: string;
  title: string;
  summary: string | null;
  description: string | null;
  categoryId: string | null;
  categoryName: string | null;
  isFeatured: boolean;
  salaryRangeLabel: string | null;
  minEducationYears: number;
  maxEducationYears: number;
  imageUrl: string | null;
  disclaimer: string | null;
  publishedAt: string | null;
  skills: SkillDto[];
  exams: { id: number; slug: string; name: string; conductingBody: string | null; level: string | null }[];
  courses: { id: number; slug: string; name: string; shortName: string | null; degreeLevel: string; durationYears: number }[];
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
  user: { id: string; email: string; displayName: string | null; isEmailVerified: boolean; roles: string[] };
}

export interface StudentProfileResponse {
  userId: string;
  displayName: string | null;
  avatarUrl: string | null;
  currentEducationLevel: string | null;
  stateOfResidence: string | null;
  preferredLocale: string | null;
  schoolBoard: string | null;
  streamOrSubjects: string | null;
  interests: string[] | null;
  isOnboardingComplete: boolean;
  updatedAt: string;
}

export interface SavedCareerResponse {
  id: string;
  careerId: string;
  careerTitle: string;
  careerSlug: string;
  savedAt: string;
}

export interface SavedCourseResponse {
  id: string;
  courseId: number;
  courseName: string;
  courseSlug: string;
  degreeLevel: string;
  durationYears: number;
  savedAt: string;
}

// ── Roadmap & Milestone Contracts ─────────────────────────────────────────────

export interface TaskDto {
  id: number;
  title: string;
  description: string | null;
  taskType: string;
  externalUrl: string | null;
  sortOrder: number;
  isCompleted: boolean;
  completedAt: string | null;
  dueDate: string | null;
  linkedExamId: number | null;
  linkedCourseId: number | null;
  linkedSkillId: number | null;
}

export interface MilestoneDto {
  id: number;
  title: string;
  description: string | null;
  sortOrder: number;
  isCompleted: boolean;
  completedAt: string | null;
  tasks: TaskDto[];
}

export interface RoadmapSummaryDto {
  id: string;
  title: string;
  description: string | null;
  status: string;
  careerId: string | null;
  careerTitle: string | null;
  createdAt: string;
  totalTasks: number;
  completedTasks: number;
  progressPercent: number;
}

export interface RoadmapDetailDto {
  id: string;
  title: string;
  description: string | null;
  status: string;
  careerId: string | null;
  careerTitle: string | null;
  targetDate: string | null;
  completedAt: string | null;
  createdAt: string;
  milestones: MilestoneDto[];
}

// ── AI Assistant Contracts ───────────────────────────────────────────────────

export interface CitationDto {
  documentId: string;
  documentTitle: string;
  docType: string;
  chunkIndex: number;
  content: string;
}

export interface ChatResponse {
  reply: string;
  conversationId: string;
  citations: CitationDto[];
  tokensUsed: number;
}

export interface QuotaStatusDto {
  maxDailyTokens: number;
  usedDailyTokens: number;
  resetAt: string;
}

// ── API Functions ─────────────────────────────────────────────────────────────


export const api = {
  // Profile
  getProfile: () => apiFetch<StudentProfileResponse>('/api/v1/me/profile'),
  upsertProfile: (data: {
    displayName?: string;
    currentEducationLevel?: string;
    stateOfResidence?: string;
    preferredLocale?: string;
    schoolBoard?: string;
    streamOrSubjects?: string;
    interests?: string[];
  }) =>
    apiFetch<StudentProfileResponse>('/api/v1/me/profile', {
      method: 'PUT',
      body: JSON.stringify(data),
    }),

  // Saved Careers
  getSavedCareers: (locale?: string) => apiFetch<SavedCareerResponse[]>(`/api/v1/me/saved-careers?locale=${locale ?? 'en'}`),
  saveCareer: (careerId: string) => apiFetch<unknown>(`/api/v1/me/saved-careers/${careerId}`, { method: 'POST' }),
  unsaveCareer: (careerId: string) => apiFetch<unknown>(`/api/v1/me/saved-careers/${careerId}`, { method: 'DELETE' }),

  // Saved Courses
  getSavedCourses: (locale?: string) => apiFetch<SavedCourseResponse[]>(`/api/v1/me/saved-courses?locale=${locale ?? 'en'}`),
  saveCourse: (courseId: number) => apiFetch<unknown>(`/api/v1/me/saved-courses/${courseId}`, { method: 'POST' }),
  unsaveCourse: (courseId: number) => apiFetch<unknown>(`/api/v1/me/saved-courses/${courseId}`, { method: 'DELETE' }),

  // Careers
  getCareers: (params?: { categoryId?: string; search?: string; page?: number; pageSize?: number; locale?: string }) => {
    const q = new URLSearchParams();
    if (params?.categoryId) q.set('category', params.categoryId);
    if (params?.search)     q.set('search',     params.search);
    if (params?.page)       q.set('page',        String(params.page));
    if (params?.pageSize)   q.set('pageSize',    String(params.pageSize));
    const loc = params?.locale ?? 'en';
    return apiFetch<PagedResponse<CareerSummary>>(`/api/v1/careers?locale=${loc}&${q}`);
  },

  getCareerDetail: (slug: string, locale?: string) => {
    const loc = locale ?? 'en';
    return apiFetch<CareerDetail>(`/api/v1/careers/${slug}/detail?locale=${loc}`);
  },

  // Categories
  getCategories: () => apiFetch<CategoryDto[]>('/api/v1/categories'),

  // Exams
  getExams: (params?: { level?: string; search?: string; page?: number; locale?: string }) => {
    const q = new URLSearchParams();
    if (params?.level)  q.set('level',  params.level);
    if (params?.search) q.set('search', params.search);
    if (params?.page)   q.set('page',   String(params.page));
    const loc = params?.locale ?? 'en';
    return apiFetch<PagedResponse<ExamDto>>(`/api/v1/exams?locale=${loc}&${q}`);
  },

  // Courses
  getCourses: (params?: { degreeLevel?: string; categoryId?: string; search?: string; page?: number; locale?: string }) => {
    const q = new URLSearchParams();
    if (params?.degreeLevel) q.set('degreeLevel', params.degreeLevel);
    if (params?.categoryId)  q.set('categoryId',  params.categoryId);
    if (params?.search)      q.set('search',       params.search);
    if (params?.page)        q.set('page',          String(params.page));
    const loc = params?.locale ?? 'en';
    return apiFetch<PagedResponse<CourseDto>>(`/api/v1/courses?locale=${loc}&${q}`);
  },

  // Scholarships
  getScholarships: (params?: { level?: string; search?: string; page?: number; locale?: string }) => {
    const q = new URLSearchParams();
    if (params?.level)  q.set('level',  params.level);
    if (params?.search) q.set('search', params.search);
    if (params?.page)   q.set('page',   String(params.page));
    const loc = params?.locale ?? 'en';
    return apiFetch<PagedResponse<ScholarshipDto>>(`/api/v1/scholarships?locale=${loc}&${q}`);
  },

  // Auth
  register: (email: string, password: string, displayName?: string) =>
    apiFetch<AuthResponse>('/api/v1/auth/register', {
      method: 'POST',
      body: JSON.stringify({ email, password, displayName }),
    }),

  login: (email: string, password: string) =>
    apiFetch<AuthResponse>('/api/v1/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),

  // Imports
  getImportJobs: () =>
    apiFetch<unknown[]>('/api/v1/imports/jobs'),

  getImportJobDetail: (id: string) =>
    apiFetch<unknown>(`/api/v1/imports/jobs/${id}`),

  submitImportReview: (id: string, isApproved: boolean, notes?: string) =>
    apiFetch<unknown>(`/api/v1/imports/jobs/${id}/review`, {
      method: 'POST',
      body: JSON.stringify({ isApproved, notes }),
    }),

  // Knowledge Base
  getDocuments: () =>
    apiFetch<unknown[]>('/api/v1/knowledge'),

  getDocumentDetail: (id: string) =>
    apiFetch<unknown>(`/api/v1/knowledge/${id}`),

  updateDocumentChunk: (chunkId: number, content: string, isReviewed: boolean) =>
    apiFetch<unknown>(`/api/v1/knowledge/chunks/${chunkId}`, {
      method: 'PUT',
      body: JSON.stringify({ content, isReviewed }),
    }),

  submitDocumentReview: (id: string, isApproved: boolean, notes?: string) =>
    apiFetch<unknown>(`/api/v1/knowledge/${id}/review`, {
      method: 'POST',
      body: JSON.stringify({ isApproved, notes }),
    }),

  // Editorial & Articles
  getEditorialArticles: (params?: { status?: string; search?: string; page?: number; pageSize?: number }) => {
    const q = new URLSearchParams();
    if (params?.status) q.set('status', params.status);
    if (params?.search) q.set('search', params.search);
    if (params?.page) q.set('page', String(params.page));
    if (params?.pageSize) q.set('pageSize', String(params.pageSize));
    return apiFetch<unknown[]>(`/api/v1/editorial/articles?${q}`);
  },

  getEditorialArticleDetail: (id: string) =>
    apiFetch<unknown>(`/api/v1/editorial/articles/${id}`),

  submitEditorialReviewDecision: (articleId: string, reviewId: number, decision: string, feedback?: string) =>
    apiFetch<unknown>(`/api/v1/editorial/articles/${articleId}/reviews/${reviewId}/decision`, {
      method: 'POST',
      body: JSON.stringify({ decision, feedback }),
    }),

  publishEditorialArticle: (articleId: string) =>
    apiFetch<unknown>(`/api/v1/editorial/articles/${articleId}/publish`, {
      method: 'POST',
      body: JSON.stringify({}),
    }),

  // Billing & Subscriptions
  getPlans: () =>
    apiFetch<unknown[]>('/api/v1/billing/plans'),

  getActiveSubscription: () =>
    apiFetch<unknown>('/api/v1/billing/my-subscription'),

  subscribeToPlan: (planId: string, provider: string, cardToken: string, couponCode?: string) =>
    apiFetch<unknown>('/api/v1/billing/subscribe', {
      method: 'POST',
      body: JSON.stringify({ planId, paymentProvider: provider, cardToken, couponCode }),
    }),

  cancelSubscriptionRenewal: () =>
    apiFetch<unknown>('/api/v1/billing/cancel', {
      method: 'POST',
    }),

  // Roadmaps & Milestones
  getRoadmaps: () =>
    apiFetch<RoadmapSummaryDto[]>('/api/v1/me/roadmaps'),

  getRoadmapDetail: (id: string) =>
    apiFetch<RoadmapDetailDto>(`/api/v1/me/roadmaps/${id}`),

  createRoadmap: (data: { title: string; description?: string; careerId?: string; targetDate?: string }) =>
    apiFetch<{ id: string }>('/api/v1/me/roadmaps', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  deleteRoadmap: (id: string) =>
    apiFetch<void>(`/api/v1/me/roadmaps/${id}`, {
      method: 'DELETE',
    }),

  addMilestone: (roadmapId: string, data: { title: string; description?: string; sortOrder?: number }) =>
    apiFetch<{ milestoneId: number }>(`/api/v1/me/roadmaps/${roadmapId}/milestones`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  completeMilestone: (roadmapId: string, milestoneId: number) =>
    apiFetch<void>(`/api/v1/me/roadmaps/${roadmapId}/milestones/${milestoneId}/complete`, {
      method: 'POST',
    }),

  addTask: (roadmapId: string, milestoneId: number, data: {
    title: string;
    description?: string;
    taskType?: string;
    externalUrl?: string;
    sortOrder?: number;
    dueDate?: string;
    linkedExamId?: number;
    linkedCourseId?: number;
    linkedSkillId?: number;
  }) =>
    apiFetch<{ taskId: number }>(`/api/v1/me/roadmaps/${roadmapId}/milestones/${milestoneId}/tasks`, {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  completeTask: (roadmapId: string, milestoneId: number, taskId: number) =>
    apiFetch<void>(`/api/v1/me/roadmaps/${roadmapId}/milestones/${milestoneId}/tasks/${taskId}/complete`, {
      method: 'POST',
    }),

  // AI Assistant (RAG)
  sendAiChat: (message: string, conversationId?: string) =>
    apiFetch<ChatResponse>('/api/v1/ai/chat', {
      method: 'POST',
      body: JSON.stringify({ message, conversationId }),
    }),

  getAiQuota: () =>
    apiFetch<QuotaStatusDto>('/api/v1/ai/quota'),

  // Coupons & Discounts
  getPublicCoupons: () =>
    apiFetch<any[]>('/api/v1/billing/coupons/public'),

  validateCoupon: (code: string, planId: string) =>
    apiFetch<{
      isValid: boolean;
      message: string;
      code?: string;
      discountType?: string;
      discountValue?: number;
      originalPrice: number;
      discountAmount: number;
      finalPrice: number;
    }>('/api/v1/billing/coupons/validate', {
      method: 'POST',
      body: JSON.stringify({ code, planId }),
    }),

  // Admin Super Controls & Analytics
  getAdminOverview: () =>
    apiFetch<{
      totalUsers: number;
      activeUsersToday: number;
      totalSubscriptions: number;
      activeSubscriptions: number;
      monthlyRecurringRevenue: number;
      totalRevenue: number;
      totalRoadmapsGenerated: number;
      totalAiQueriesServed: number;
      tierBreakdown: Array<{ tierName: string; subscriberCount: number; monthlyRevenue: number }>;
    }>('/api/v1/admin/overview'),

  getAdminUsers: (params?: { search?: string; role?: string }) => {
    const q = new URLSearchParams()
    if (params?.search) q.set('search', params.search)
    if (params?.role) q.set('role', params.role)
    return apiFetch<any[]>(`/api/v1/admin/users?${q.toString()}`)
  },

  toggleUserSuspension: (userId: string, isActive: boolean) =>
    apiFetch<{ success: boolean }>(`/api/v1/admin/users/${userId}/suspension`, {
      method: 'PATCH',
      body: JSON.stringify({ isActive }),
    }),

  getAdminCoupons: () =>
    apiFetch<any[]>('/api/v1/admin/coupons'),

  createAdminCoupon: (data: {
    code: string;
    description?: string;
    discountType: string;
    discountValue: number;
    minPlanPrice: number;
    maxRedemptions: number;
    isActive: boolean;
    isVisiblePublicly: boolean;
    targetUserId?: string;
  }) =>
    apiFetch<any>('/api/v1/admin/coupons', {
      method: 'POST',
      body: JSON.stringify(data),
    }),

  toggleAdminCoupon: (couponId: string, data: { isActive?: boolean; isVisiblePublicly?: boolean }) =>
    apiFetch<{ success: boolean }>(`/api/v1/admin/coupons/${couponId}/toggle`, {
      method: 'PATCH',
      body: JSON.stringify(data),
    }),
};

