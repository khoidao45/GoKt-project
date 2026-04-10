import { useState, useEffect, useRef } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { tripsApi } from '../api'
import type { TripMessageDto } from '../types'

interface Props {
  tripId: string
  currentUserId: string
  hubRef: React.MutableRefObject<HubConnection | null>
  externalMessages: TripMessageDto[]   // messages received via SignalR from parent
}

export default function TripChat({ tripId, currentUserId, hubRef, externalMessages }: Props) {
  const [messages, setMessages] = useState<TripMessageDto[]>([])
  const [input, setInput] = useState('')
  const [sending, setSending] = useState(false)
  const bottomRef = useRef<HTMLDivElement>(null)

  // Load history on mount
  useEffect(() => {
    tripsApi.getMessages(tripId).then(setMessages).catch(() => {})
  }, [tripId])

  // Merge external messages (from SignalR parent listener)
  useEffect(() => {
    if (externalMessages.length === 0) return
    setMessages(prev => {
      const existingIds = new Set(prev.map(m => m.id))
      const newOnes = externalMessages.filter(m => !existingIds.has(m.id))
      return newOnes.length ? [...prev, ...newOnes] : prev
    })
  }, [externalMessages])

  // Auto-scroll to bottom
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const send = async () => {
    const body = input.trim()
    if (!body || !hubRef.current) return
    setSending(true)
    try {
      await hubRef.current.invoke('SendMessage', tripId, body)
      setInput('')
    } catch {
      // Fallback to HTTP if SignalR fails
      try {
        const msg = await tripsApi.sendMessage(tripId, body)
        setMessages(prev => [...prev, msg])
        setInput('')
      } catch { /* ignore */ }
    } finally {
      setSending(false)
    }
  }

  const handleKey = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send() }
  }

  const fmtTime = (s: string) =>
    new Date(s).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })

  return (
    <div style={{
      background: '#0f172a', borderRadius: 12, border: '1px solid #334155',
      display: 'flex', flexDirection: 'column', overflow: 'hidden',
    }}>
      <div style={{
        padding: '10px 16px', borderBottom: '1px solid #1e293b',
        fontSize: 12, fontWeight: 700, color: '#64748b', letterSpacing: 1,
      }}>
        NHẮN TIN VỚI {currentUserId ? 'TÀI XẾ / KHÁCH' : ''}
      </div>

      {/* Message list */}
      <div style={{
        flex: 1, overflowY: 'auto', padding: '12px 16px',
        display: 'flex', flexDirection: 'column', gap: 8,
        minHeight: 160, maxHeight: 260,
      }}>
        {messages.length === 0 && (
          <div style={{ textAlign: 'center', color: '#475569', fontSize: 13, paddingTop: 24 }}>
            Chưa có tin nhắn. Hãy bắt đầu cuộc trò chuyện!
          </div>
        )}
        {messages.map(msg => {
          const isMine = msg.senderId === currentUserId
          return (
            <div key={msg.id} style={{
              display: 'flex', flexDirection: 'column',
              alignItems: isMine ? 'flex-end' : 'flex-start',
            }}>
              <div style={{
                maxWidth: '78%', padding: '8px 12px', borderRadius: isMine ? '12px 12px 4px 12px' : '12px 12px 12px 4px',
                background: isMine ? '#10b981' : '#1e293b',
                color: isMine ? '#fff' : '#e2e8f0',
                fontSize: 14, lineHeight: 1.4, wordBreak: 'break-word',
              }}>
                {msg.body}
              </div>
              <div style={{ fontSize: 10, color: '#475569', marginTop: 2 }}>
                {msg.senderRole === 'Driver' ? 'Tài xế' : 'Khách'} · {fmtTime(msg.sentAt)}
              </div>
            </div>
          )
        })}
        <div ref={bottomRef} />
      </div>

      {/* Input */}
      <div style={{
        display: 'flex', gap: 8, padding: '10px 12px',
        borderTop: '1px solid #1e293b',
      }}>
        <input
          value={input}
          onChange={e => setInput(e.target.value)}
          onKeyDown={handleKey}
          placeholder="Nhập tin nhắn..."
          style={{
            flex: 1, background: '#1e293b', border: '1px solid #334155',
            borderRadius: 8, padding: '8px 12px', color: '#f1f5f9', fontSize: 14,
            outline: 'none', fontFamily: 'inherit',
          }}
        />
        <button
          onClick={send}
          disabled={!input.trim() || sending}
          style={{
            padding: '8px 16px', borderRadius: 8, border: 'none',
            background: input.trim() ? '#10b981' : '#1e293b',
            color: input.trim() ? '#fff' : '#475569',
            cursor: input.trim() ? 'pointer' : 'default',
            fontSize: 14, fontWeight: 600, fontFamily: 'inherit', flexShrink: 0,
          }}
        >
          {sending ? '...' : 'Gửi'}
        </button>
      </div>
    </div>
  )
}
