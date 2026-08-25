import { notFound } from 'next/navigation'
import Link from 'next/link'
import { ArrowLeft, TrendingUp, Clock, Award, BookOpen, GraduationCap, Compass } from 'lucide-react'
import { api } from '@/lib/api'
import { cookies } from 'next/headers'
import { translate } from '@/lib/i18n'
import SaveCareerButton from '@/components/SaveCareerButton'
import GenerateRoadmapButton from '@/components/GenerateRoadmapButton'

interface Props { params: { slug: string }; searchParams: { locale?: string } }

export async function generateMetadata({ params }: Props) {
  const cookieStore = cookies()
  const loc = cookieStore.get('locale')?.value ?? 'en'
  const career = await api.getCareerDetail(params.slug, loc).catch(() => null)
  if (!career) return {}
  return { title: career.title, description: career.summary ?? undefined }
}

export default async function CareerDetailPage({ params }: Props) {
  const cookieStore = cookies()
  const loc = cookieStore.get('locale')?.value ?? 'en'
  const career = await api.getCareerDetail(params.slug, loc).catch(() => null)
  if (!career) notFound()

  const skillColors: Record<string, string> = {
    Technical: 'badge-brand',
    Soft:      'badge-teal',
    Domain:    'badge-purple',
  }

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-12 page-enter">

      {/* Back */}
      <Link href={`/careers?locale=${loc}`} className="inline-flex items-center gap-2 text-white/50 hover:text-white mb-8 transition-colors">
        <ArrowLeft className="w-4 h-4" /> {translate('backToCareers', loc)}
      </Link>

      {/* Hero card */}
      <div className="glass rounded-3xl p-8 mb-8 bg-gradient-to-br from-brand-600/10 to-accent-purple/10 border border-brand-500/20">
        <div className="flex flex-wrap items-start justify-between gap-4 mb-6">
          <div className="flex items-start gap-4 flex-1">
            <div className="w-16 h-16 rounded-2xl bg-brand-gradient flex items-center justify-center shadow-brand animate-glow">
              <Compass className="w-8 h-8 text-white" />
            </div>
            <div className="flex-1">
              {career.categoryName && (
                <span className="badge-brand mb-2 inline-block">{career.categoryName}</span>
              )}
              <h1 className="font-display font-black text-3xl md:text-4xl text-white">{career.title}</h1>
            </div>
          </div>
          <div className="flex items-center gap-3 shrink-0 flex-wrap">
            <Link
              href={`/careers/compare?locale=${loc}`}
              className="glass-button text-xs px-4 py-2.5 font-bold border shadow-sm flex items-center gap-1.5"
              style={{ borderColor: 'var(--border-color)', color: 'var(--text-primary)' }}
            >
              ⚖️ {loc === 'hi' ? 'करियर तुलना' : 'Compare Careers'}
            </Link>
            <GenerateRoadmapButton
              careerId={career.id}
              careerTitle={career.title}
              careerSkills={career.skills}
              careerExams={career.exams}
              careerCourses={career.courses}
              locale={loc}
            />
            <SaveCareerButton careerId={career.id} initialSaved={false} loc={loc} />
          </div>
        </div>

        {/* Quick stats */}
        <div className="grid grid-cols-2 md:grid-cols-3 gap-4 mb-6">
          {career.salaryRangeLabel && (
            <div className="glass rounded-xl p-4">
              <div className="flex items-center gap-2 text-accent-teal text-xs font-medium mb-1">
                <TrendingUp className="w-3.5 h-3.5" /> {translate('salaryRange', loc)}
              </div>
              <div className="text-white font-semibold">{career.salaryRangeLabel}</div>
            </div>
          )}
          {(career.minEducationYears > 0 || career.maxEducationYears > 0) && (
            <div className="glass rounded-xl p-4">
              <div className="flex items-center gap-2 text-brand-400 text-xs font-medium mb-1">
                <Clock className="w-3.5 h-3.5" /> {translate('educationRequired', loc)}
              </div>
              <div className="text-white font-semibold">
                {career.minEducationYears}–{career.maxEducationYears} {translate('years', loc)}
              </div>
            </div>
          )}
          {career.isFeatured && (
            <div className="glass rounded-xl p-4">
              <div className="flex items-center gap-2 text-amber-400 text-xs font-medium mb-1">
                <Award className="w-3.5 h-3.5" /> {loc === 'hi' ? 'स्थिति' : 'Status'}
              </div>
              <div className="text-white font-semibold">{loc === 'hi' ? 'विशेष रुप से प्रदर्शित' : 'Featured Career'}</div>
            </div>
          )}
        </div>

        {career.summary && (
          <p className="text-white/70 text-lg leading-relaxed">{career.summary}</p>
        )}
      </div>

      <div className="grid md:grid-cols-3 gap-6">
        {/* Main content */}
        <div className="md:col-span-2 space-y-6">

          {/* Description */}
          {career.description && (
            <div className="glass-card">
              <h2 className="font-display font-bold text-xl text-white mb-4">{loc === 'hi' ? 'इस करियर के बारे में' : 'About This Career'}</h2>
              <p className="text-white/60 leading-relaxed whitespace-pre-wrap">{career.description}</p>
            </div>
          )}

          {/* Skills */}
          {career.skills.length > 0 && (
            <div className="glass-card">
              <h2 className="font-display font-bold text-xl text-white mb-4 flex items-center gap-2">
                <Award className="w-5 h-5 text-brand-400" /> {loc === 'hi' ? 'प्रमुख कौशल' : 'Key Skills'}
              </h2>
              <div className="flex flex-wrap gap-2">
                {career.skills.map(skill => (
                  <span key={skill.id} className={skillColors[skill.category] ?? 'badge'}>
                    {skill.name}
                  </span>
                ))}
              </div>
            </div>
          )}

          {/* Exams */}
          {career.exams.length > 0 && (
            <div className="glass-card">
              <h2 className="font-display font-bold text-xl text-white mb-4 flex items-center gap-2">
                <BookOpen className="w-5 h-5 text-accent-orange" /> {translate('entranceExams', loc)}
              </h2>
              <div className="space-y-3">
                {career.exams.map(exam => (
                  <Link key={exam.id} href={`/exams?locale=${loc}`}
                    className="flex items-center justify-between p-4 glass rounded-xl hover:bg-white/10 transition-colors group">
                    <div>
                      <div className="text-white font-semibold group-hover:text-brand-300 transition-colors">{exam.name}</div>
                      {exam.conductingBody && <div className="text-white/40 text-sm">{exam.conductingBody}</div>}
                    </div>
                    {exam.level && <span className="badge-orange">{exam.level}</span>}
                  </Link>
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Courses */}
          {career.courses.length > 0 && (
            <div className="glass-card">
              <h2 className="font-display font-bold text-lg text-white mb-4 flex items-center gap-2">
                <GraduationCap className="w-5 h-5 text-accent-purple" /> {loc === 'hi' ? 'अनुशंसित पाठ्यक्रम' : 'Recommended Courses'}
              </h2>
              <div className="space-y-3">
                {career.courses.map(course => (
                  <Link key={course.id} href={`/courses?locale=${loc}`}
                    className="block p-3 glass rounded-xl hover:bg-white/10 transition-colors group">
                    <div className="text-white text-sm font-medium group-hover:text-brand-300 transition-colors">
                      {course.name}
                    </div>
                    <div className="text-white/40 text-xs mt-1">
                      {course.degreeLevel} · {course.durationYears} {translate('years', loc)}
                    </div>
                  </Link>
                ))}
              </div>
            </div>
          )}

          {/* Disclaimer */}
          {career.disclaimer && (
            <div className="glass rounded-xl p-4 border border-amber-500/20 bg-amber-500/5">
              <p className="text-amber-200/70 text-xs leading-relaxed">⚠️ {career.disclaimer}</p>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
