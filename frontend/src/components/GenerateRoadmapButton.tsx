'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { Loader2, Sparkles, CheckCircle2 } from 'lucide-react'
import { api } from '@/lib/api'
import { useAuth } from '@/lib/auth-context'

interface GenerateRoadmapButtonProps {
  careerId: string
  careerTitle: string
  careerSkills?: { id: number; name: string }[]
  careerExams?: { id: number; name: string }[]
  careerCourses?: { id: number; name: string }[]
  locale: string
}

export default function GenerateRoadmapButton({
  careerId,
  careerTitle,
  careerSkills = [],
  careerExams = [],
  careerCourses = [],
  locale,
}: GenerateRoadmapButtonProps) {
  const router = useRouter()
  const { isAuthenticated } = useAuth()
  const [loading, setLoading] = useState(false)
  const [created, setCreated] = useState(false)

  async function handleGenerateRoadmap() {
    if (!isAuthenticated) {
      router.push(`/auth/login?redirect=/careers`)
      return
    }

    setLoading(true)
    try {
      // 1. Create Roadmap
      const roadmapTitle = locale === 'hi' ? `${careerTitle} सीखने का रोडमैप` : `${careerTitle} Career Roadmap`
      const roadmapDesc = locale === 'hi'
        ? `${careerTitle} में सफलता प्राप्त करने के लिए प्रमुख कौशल, परीक्षा और पाठ्यक्रम का संरचित मार्ग।`
        : `Structured milestones and tasks to achieve a career as ${careerTitle}.`

      const createdRoadmap = await api.createRoadmap({
        title: roadmapTitle,
        description: roadmapDesc,
        careerId,
      })

      const roadmapId = createdRoadmap.id

      // 2. Add Milestones & Tasks automatically from Skills, Exams, and Courses
      // Milestone 1: Foundation & Education
      const m1 = await api.addMilestone(roadmapId, {
        title: locale === 'hi' ? 'चरण 1: बुनियादी शिक्षा और योग्यता' : 'Phase 1: Academic Foundation & Degrees',
        description: locale === 'hi' ? 'इस करियर के लिए आवश्यक डिग्री और पात्रता पूरी करें' : 'Acquire prerequisite qualifications and degrees',
        sortOrder: 1,
      })

      if (careerCourses.length > 0) {
        for (const course of careerCourses) {
          await api.addTask(roadmapId, m1.milestoneId, {
            title: locale === 'hi' ? `${course.name} पाठ्यक्रम पूरा करें` : `Complete course: ${course.name}`,
            taskType: 'Course',
            linkedCourseId: course.id,
            sortOrder: 1,
          })
        }
      } else {
        await api.addTask(roadmapId, m1.milestoneId, {
          title: locale === 'hi' ? 'आवश्यक शैक्षणिक डिग्री पूरी करें' : 'Complete relevant undergraduate degree',
          taskType: 'General',
          sortOrder: 1,
        })
      }

      // Milestone 2: Entrance Exams & Certifications / Professional Milestones
      const m2 = await api.addMilestone(roadmapId, {
        title: locale === 'hi' ? 'चरण 2: प्रतियोगी परीक्षा और प्रमाणन' : 'Phase 2: Entrance Exams & Qualifications',
        description: locale === 'hi' ? 'प्रमुख प्रवेश और व्यावसायिक परीक्षाओं की तैयारी करें' : 'Prepare and qualify for competitive exams and certifications',
        sortOrder: 2,
      })

      if (careerExams.length > 0) {
        for (const exam of careerExams) {
          await api.addTask(roadmapId, m2.milestoneId, {
            title: locale === 'hi' ? `${exam.name} परीक्षा उत्तीर्ण करें` : `Prepare & clear: ${exam.name}`,
            taskType: 'Exam',
            linkedExamId: exam.id,
            sortOrder: 1,
          })
        }
      } else {
        await api.addTask(roadmapId, m2.milestoneId, {
          title: locale === 'hi' ? 'आवश्यक प्रमाणन और प्रतियोगी परीक्षा की तैयारी करें' : 'Clear professional certifications or entrance criteria',
          taskType: 'Exam',
          sortOrder: 1,
        })
      }

      // Milestone 3: Skill Mastery & Practice
      const m3 = await api.addMilestone(roadmapId, {
        title: locale === 'hi' ? 'चरण 3: मुख्य कौशल महारत' : 'Phase 3: Core Skill Mastery',
        description: locale === 'hi' ? 'उद्योग के लिए आवश्यक कौशल और व्यावहारिक ज्ञान विकसित करें' : 'Build industry-relevant technical and soft skills',
        sortOrder: 3,
      })

      if (careerSkills.length > 0) {
        for (const skill of careerSkills) {
          await api.addTask(roadmapId, m3.milestoneId, {
            title: locale === 'hi' ? `${skill.name} में दक्षता प्राप्त करें` : `Master skill: ${skill.name}`,
            taskType: 'Skill',
            linkedSkillId: skill.id,
            sortOrder: 1,
          })
        }
      } else {
        await api.addTask(roadmapId, m3.milestoneId, {
          title: locale === 'hi' ? 'उद्योग के लिए प्रासंगिक प्रोजेक्ट बनाएं' : 'Build domain portfolio project',
          taskType: 'General',
          sortOrder: 1,
        })
      }

      setCreated(true)
      setTimeout(() => {
        router.push(`/me/roadmaps/${roadmapId}`)
      }, 800)
    } catch (err) {
      console.error('Failed to generate roadmap', err)
    } finally {
      setLoading(false)
    }
  }

  return (
    <button
      onClick={handleGenerateRoadmap}
      disabled={loading || created}
      className={`inline-flex items-center gap-2 px-5 py-2.5 rounded-xl font-semibold text-sm transition-all shadow-brand ${
        created
          ? 'bg-emerald-600 text-white'
          : 'bg-brand-gradient text-white hover:shadow-glow hover:scale-[1.02] active:scale-[0.98]'
      }`}
    >
      {loading ? (
        <>
          <Loader2 className="w-4 h-4 animate-spin" />
          {locale === 'hi' ? 'रोडमैप तैयार किया जा रहा है...' : 'Generating Roadmap...'}
        </>
      ) : created ? (
        <>
          <CheckCircle2 className="w-4 h-4" />
          {locale === 'hi' ? 'रोडमैप तैयार!' : 'Roadmap Created!'}
        </>
      ) : (
        <>
          <Sparkles className="w-4 h-4" />
          {locale === 'hi' ? '🎯 इस करियर का रोडमैप बनाएं' : '🎯 Generate Career Roadmap'}
        </>
      )}
    </button>
  )
}
