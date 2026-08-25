import { cookies } from 'next/headers'
import { api } from '@/lib/api'
import CourseGridContainer from '@/components/CourseGridContainer'

export const metadata = {
  title: 'Courses',
  description: 'Browse undergraduate, postgraduate and diploma courses for Indian students.'
}

interface Props {
  searchParams: { degreeLevel?: string; search?: string; page?: string; locale?: string }
}

export default async function CoursesPage({ searchParams }: Props) {
  const page = Number(searchParams.page ?? 1)
  const cookieStore = cookies()
  const loc = cookieStore.get('locale')?.value ?? 'en'

  const data = await api.getCourses({
    degreeLevel: searchParams.degreeLevel,
    search: searchParams.search,
    page,
    locale: loc
  }).catch(() => ({
    items: [],
    totalCount: 0,
    hasNextPage: false,
    hasPreviousPage: false,
    page: 1,
    pageSize: 20
  }))

  const degreeLevels = ['Undergraduate', 'Postgraduate', 'Diploma', 'Certificate', 'Doctoral']

  return (
    <CourseGridContainer
      initialData={data}
      degreeLevels={degreeLevels}
      searchParams={searchParams}
      loc={loc}
      page={page}
    />
  )
}
