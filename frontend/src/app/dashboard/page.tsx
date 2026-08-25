import { cookies } from 'next/headers'
import DashboardContainer from '@/components/DashboardContainer'

export const metadata = {
  title: 'Dashboard — CareerPath Bharat',
  description: 'Your personalized student dashboard and recommendations.',
}

export default function DashboardPage() {
  const cookieStore = cookies()
  const loc = cookieStore.get('locale')?.value ?? 'en'

  return <DashboardContainer initialLocale={loc} />
}
