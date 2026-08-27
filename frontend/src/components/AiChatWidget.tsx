'use client'

import { useState, useEffect, useRef } from 'react'
import { 
  Sparkles, X, Send, Bot, User, Loader2, BookOpen, 
  ChevronDown, RefreshCw, Zap
} from 'lucide-react'
import { api, ChatResponse, CitationDto, QuotaStatusDto } from '@/lib/api'
import { useAuth } from '@/lib/auth-context'

interface Message {
  id: string
  sender: 'user' | 'ai'
  text: string
  citations?: CitationDto[]
  timestamp: string
}

export default function AiChatWidget() {
  const { isAuthenticated, user } = useAuth()
  const [isOpen, setIsOpen] = useState(false)
  const [messages, setMessages] = useState<Message[]>([])
  const [inputMessage, setInputMessage] = useState('')
  const [conversationId, setConversationId] = useState<string | undefined>(undefined)
  const [sending, setSending] = useState(false)
  const [quota, setQuota] = useState<QuotaStatusDto | null>(null)
  const [locale, setLocale] = useState('en')
  const [expandedCitationId, setExpandedCitationId] = useState<string | null>(null)
  
  const messagesEndRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (typeof window !== 'undefined') {
      setLocale(localStorage.getItem('locale') ?? 'en')
    }
  }, [])

  useEffect(() => {
    if (isAuthenticated && isOpen) {
      api.getAiQuota().then(setQuota).catch(() => {})
    }
  }, [isAuthenticated, isOpen])

  useEffect(() => {
    if (isOpen && messages.length === 0) {
      const welcomeText = locale === 'hi'
        ? `नमस्ते ${user?.displayName || 'छात्र'}! मैं आपका करियर और शिक्षा एआई सहायक हूँ। आप मुझसे करियर, प्रवेश परीक्षा (JEE, NEET, UPSC), छात्रवृत्ति या अध्ययन पथ के बारे में कुछ भी पूछ सकते हैं!`
        : `Hello ${user?.displayName || 'Student'}! I'm your Career & Education AI Advisor. Ask me anything about career paths, entrance exams, syllabus requirements, or learning roadmaps!`
      
      setMessages([
        {
          id: 'welcome',
          sender: 'ai',
          text: welcomeText,
          timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
        }
      ])
    }
  }, [isOpen, messages.length, locale, user])

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, sending])

  async function handleSendMessage(e: React.FormEvent) {
    e.preventDefault()
    if (!inputMessage.trim() || sending) return

    const userText = inputMessage.trim()
    setInputMessage('')

    const userMsg: Message = {
      id: `usr-${Date.now()}`,
      sender: 'user',
      text: userText,
      timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    }

    setMessages(prev => [...prev, userMsg])
    setSending(true)

    try {
      const res: ChatResponse = await api.sendAiChat(userText, conversationId)
      if (res.conversationId) {
        setConversationId(res.conversationId)
      }

      const aiMsg: Message = {
        id: `ai-${Date.now()}`,
        sender: 'ai',
        text: res.reply,
        citations: res.citations ?? [],
        timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      }

      setMessages(prev => [...prev, aiMsg])
      // Refresh quota
      api.getAiQuota().then(setQuota).catch(() => {})
    } catch (err: unknown) {
      const errorMsg = (err as Error)?.message || (locale === 'hi' ? 'उत्तर प्राप्त करने में असमर्थ। कृपया पुनः प्रयास करें।' : 'Failed to retrieve response. Please try again.')
      setMessages(prev => [
        ...prev,
        {
          id: `err-${Date.now()}`,
          sender: 'ai',
          text: `⚠️ ${errorMsg}`,
          timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
        }
      ])
    } finally {
      setSending(false)
    }
  }

  function handleResetChat() {
    setConversationId(undefined)
    setMessages([])
  }

  // Only render for logged-in students
  if (!isAuthenticated) return null

  return (
    <div className="fixed bottom-6 right-6 z-50">
      {/* Floating Trigger Button */}
      {!isOpen && (
        <button
          onClick={() => setIsOpen(true)}
          className="group flex items-center gap-2.5 px-4 py-3 rounded-2xl bg-brand-gradient !text-white shadow-brand hover:shadow-glow hover:scale-105 active:scale-95 transition-all duration-300 backdrop-blur-md font-bold"
        >
          <div className="relative">
            <Sparkles className="w-5 h-5 animate-pulse text-white" />
            <span className="absolute -top-1 -right-1 w-2.5 h-2.5 bg-emerald-400 rounded-full border-2 border-surface-900" />
          </div>
          <span className="text-sm font-bold tracking-wide !text-white">
            {locale === 'hi' ? 'एआई करियर सहायक' : 'AI Career Guide'}
          </span>
        </button>
      )}

      {/* Chat Window Panel */}
      {isOpen && (
        <div className="w-[360px] sm:w-[420px] h-[580px] max-h-[85vh] glass rounded-3xl border shadow-2xl flex flex-col overflow-hidden animate-slide-up"
          style={{ borderColor: 'var(--border-color)', backgroundColor: 'var(--card-bg)' }}
        >
          {/* Header */}
          <div className="p-4 border-b bg-gradient-to-r from-brand-600/10 via-accent-purple/10 to-transparent flex items-center justify-between"
            style={{ borderColor: 'var(--border-color)' }}
          >
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 rounded-xl bg-brand-gradient flex items-center justify-center shadow-brand">
                <Bot className="w-5 h-5 text-white" />
              </div>
              <div>
                <h3 className="font-display font-bold text-sm flex items-center gap-1.5" style={{ color: 'var(--text-primary)' }}>
                  {locale === 'hi' ? 'करियर सहायक' : 'CareerPath AI Guide'}
                  <span className="text-[10px] px-1.5 py-0.2 rounded-md bg-emerald-500/10 text-emerald-500 font-semibold">RAG</span>
                </h3>
                {quota && (
                  <p className="text-[11px] flex items-center gap-1" style={{ color: 'var(--text-muted)' }}>
                    <Zap className="w-3 h-3 text-amber-500" />
                    {quota.usedDailyTokens.toLocaleString()} / {quota.maxDailyTokens.toLocaleString()} tokens
                  </p>
                )}
              </div>
            </div>

            <div className="flex items-center gap-1">
              <button
                onClick={handleResetChat}
                className="p-1.5 rounded-lg border hover:bg-black/5 dark:hover:bg-white/5 transition-colors"
                style={{ borderColor: 'var(--border-color)', color: 'var(--text-secondary)' }}
                title={locale === 'hi' ? 'बातचीत रीसेट करें' : 'Reset Conversation'}
              >
                <RefreshCw className="w-3.5 h-3.5" />
              </button>
              <button
                onClick={() => setIsOpen(false)}
                className="p-1.5 rounded-lg border hover:bg-black/5 dark:hover:bg-white/5 transition-colors"
                style={{ borderColor: 'var(--border-color)', color: 'var(--text-secondary)' }}
                aria-label="Close chat"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
          </div>

          {/* Messages Body */}
          <div className="flex-1 overflow-y-auto p-4 space-y-4">
            {messages.map((msg) => (
              <div
                key={msg.id}
                className={`flex gap-3 ${msg.sender === 'user' ? 'justify-end' : 'justify-start'}`}
              >
                {msg.sender === 'ai' && (
                  <div className="w-7 h-7 rounded-lg bg-brand-gradient flex items-center justify-center text-white shrink-0 mt-0.5 shadow-sm">
                    <Bot className="w-4 h-4" />
                  </div>
                )}

                <div className={`max-w-[82%] space-y-2`}>
                  <div
                    className={`p-3.5 rounded-2xl text-xs sm:text-sm leading-relaxed ${
                      msg.sender === 'user'
                        ? 'bg-brand-gradient text-white rounded-br-none shadow-brand'
                        : 'border rounded-tl-none shadow-sm'
                    }`}
                    style={
                      msg.sender === 'user'
                        ? {}
                        : { backgroundColor: 'var(--card-hover-bg)', borderColor: 'var(--border-color)', color: 'var(--text-primary)' }
                    }
                  >
                    <p className="whitespace-pre-wrap">{msg.text}</p>
                    <span
                      className={`block text-[10px] mt-1 text-right ${
                        msg.sender === 'user' ? 'text-white/70' : ''
                      }`}
                      style={msg.sender === 'user' ? {} : { color: 'var(--text-muted)' }}
                    >
                      {msg.timestamp}
                    </span>
                  </div>

                  {/* Citations list if any */}
                  {msg.citations && msg.citations.length > 0 && (
                    <div className="space-y-1.5 pt-1">
                      <p className="text-[11px] font-bold flex items-center gap-1" style={{ color: 'var(--text-secondary)' }}>
                        <BookOpen className="w-3 h-3 text-brand-500" />
                        {locale === 'hi' ? 'सत्यापित संदर्भ (स्रोत):' : 'Verified Citations:'}
                      </p>
                      <div className="flex flex-wrap gap-1.5">
                        {msg.citations.map((cite, cIdx) => (
                          <div key={cite.documentId + cIdx} className="w-full">
                            <button
                              onClick={() => setExpandedCitationId(expandedCitationId === `${cite.documentId}-${cIdx}` ? null : `${cite.documentId}-${cIdx}`)}
                              className="w-full text-left text-[11px] px-2.5 py-1.5 rounded-lg border flex items-center justify-between hover:bg-brand-500/5 transition-colors"
                              style={{ borderColor: 'var(--border-color)', backgroundColor: 'var(--card-bg)' }}
                            >
                              <span className="font-semibold text-brand-500 truncate max-w-[260px]">
                                📄 {cite.documentTitle}
                              </span>
                              <ChevronDown className={`w-3 h-3 transition-transform ${expandedCitationId === `${cite.documentId}-${cIdx}` ? 'rotate-180' : ''}`} />
                            </button>
                            {expandedCitationId === `${cite.documentId}-${cIdx}` && (
                              <div className="mt-1 p-2 rounded-lg text-[11px] border bg-black/5 dark:bg-white/5 leading-relaxed"
                                style={{ borderColor: 'var(--border-color)', color: 'var(--text-secondary)' }}
                              >
                                {cite.content}
                              </div>
                            )}
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </div>

                {msg.sender === 'user' && (
                  <div className="w-7 h-7 rounded-lg bg-black/10 dark:bg-white/10 flex items-center justify-center shrink-0 mt-0.5"
                    style={{ color: 'var(--text-primary)' }}
                  >
                    <User className="w-4 h-4" />
                  </div>
                )}
              </div>
            ))}

            {sending && (
              <div className="flex items-center gap-2 text-xs py-2 text-brand-500 font-medium animate-pulse">
                <Loader2 className="w-4 h-4 animate-spin" />
                {locale === 'hi' ? 'करियर ज्ञानकोष खोजा जा रहा है...' : 'Analyzing career path & syllabus requirements...'}
              </div>
            )}

            {/* Quick Suggestion Chips */}
            {messages.length <= 2 && !sending && (
              <div className="pt-2">
                <p className="text-[11px] font-semibold mb-2" style={{ color: 'var(--text-muted)' }}>
                  {locale === 'hi' ? '💡 लोकप्रिय प्रश्न:' : '💡 Quick questions you can ask:'}
                </p>
                <div className="flex flex-wrap gap-1.5">
                  {[
                    locale === 'hi' ? 'सॉफ्टवेयर इंजीनियर कैसे बनें?' : 'How to become a Software Engineer?',
                    locale === 'hi' ? 'IAS / UPSC की तैयारी कैसे करें?' : 'Roadmap for IAS / UPSC?',
                    locale === 'hi' ? 'डॉक्टर (NEET UG) की जानकारी' : 'Doctor & NEET UG Requirements',
                    locale === 'hi' ? '12वीं के बाद टॉप कोर्सेज' : 'Top Career Courses after 12th',
                  ].map((chip) => (
                    <button
                      key={chip}
                      type="button"
                      onClick={() => {
                        setInputMessage(chip)
                      }}
                      className="text-[11px] px-2.5 py-1 rounded-full border hover:border-brand-500 hover:text-brand-500 transition-colors text-left"
                      style={{ borderColor: 'var(--border-color)', backgroundColor: 'var(--card-bg)', color: 'var(--text-secondary)' }}
                    >
                      {chip}
                    </button>
                  ))}
                </div>
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>

          {/* Input Footer */}
          <form onSubmit={handleSendMessage} className="p-3 border-t flex items-center gap-2"
            style={{ borderColor: 'var(--border-color)', backgroundColor: 'var(--card-bg)' }}
          >
            <input
              type="text"
              value={inputMessage}
              onChange={(e) => setInputMessage(e.target.value)}
              placeholder={locale === 'hi' ? 'करियर, परीक्षा या पात्रता के बारे में पूछें...' : 'Ask about software, medical, civil services, exams...'}
              className="flex-1 input text-xs py-2.5"
              disabled={sending}
            />
            <button
              type="submit"
              disabled={sending || !inputMessage.trim()}
              className="btn-brand p-2.5 rounded-xl disabled:opacity-50 shrink-0"
              title={locale === 'hi' ? 'संदेश भेजें' : 'Send Message'}
            >
              <Send className="w-4 h-4" />
            </button>
          </form>
        </div>
      )}
    </div>
  )
}
