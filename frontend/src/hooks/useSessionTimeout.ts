import { useEffect, useRef } from 'react'
import { useAuth } from '../context/AuthContext'

const TIMEOUT_DURATION = 15 * 60 * 1000 // 15 minutes in milliseconds

export function useSessionTimeout() {
  const { user, logout } = useAuth()
  const timerRef = useRef<number | null>(null)

  const resetTimer = () => {
    if (timerRef.current) {
      window.clearTimeout(timerRef.current)
    }

    if (user) {
      timerRef.current = window.setTimeout(() => {
        logout()
      }, TIMEOUT_DURATION)
    }
  }

  useEffect(() => {
    if (!user) {
      if (timerRef.current) {
        window.clearTimeout(timerRef.current)
        timerRef.current = null
      }
      return
    }

    const events = ['mousemove', 'keypress', 'click', 'scroll', 'touchstart']

    // Setup listeners
    events.forEach((event) => {
      window.addEventListener(event, resetTimer)
    })

    // Initial timer set
    resetTimer()

    return () => {
      // Cleanup listeners
      events.forEach((event) => {
        window.removeEventListener(event, resetTimer)
      })

      if (timerRef.current) {
        window.clearTimeout(timerRef.current)
      }
    }
  }, [user, logout])
}
