import { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { getCriticalAlerts } from '../api/labOrders'
import type { LabOrderItemResponse } from '../types/labOrders'
import { useAuth } from '../context/AuthContext'

function playChime() {
  try {
    const AudioContextClass = window.AudioContext || (window as any).webkitAudioContext
    if (!AudioContextClass) return
    const audioCtx = new AudioContextClass()
    const osc = audioCtx.createOscillator()
    const gain = audioCtx.createGain()

    osc.type = 'sine'
    osc.frequency.setValueAtTime(587.33, audioCtx.currentTime) // D5
    osc.frequency.setValueAtTime(880, audioCtx.currentTime + 0.12) // A5

    gain.gain.setValueAtTime(0.15, audioCtx.currentTime)
    gain.gain.exponentialRampToValueAtTime(0.01, audioCtx.currentTime + 0.6)

    osc.connect(gain)
    gain.connect(audioCtx.destination)
    osc.start()
    osc.stop(audioCtx.currentTime + 0.6)
  } catch (e) {
    console.warn('Audio warning chime blocked by browser settings:', e)
  }
}

export function CriticalAlertBanner() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const [alerts, setAlerts] = useState<LabOrderItemResponse[]>([])
  const soundPlayedTracker = useRef<Record<string, boolean>>({})

  useEffect(() => {
    if (!user) return

    const fetchAlerts = async () => {
      try {
        const data = await getCriticalAlerts()
        // Only active alerts that do NOT have a critical call log recorded
        const activeAlerts = data.filter((item) => !item.criticalCallLogId)
        setAlerts(activeAlerts)

        // Trigger chime sound for new alerts
        let hasNew = false
        activeAlerts.forEach((alert) => {
          if (!soundPlayedTracker.current[alert.labOrderItemId]) {
            soundPlayedTracker.current[alert.labOrderItemId] = true
            hasNew = true
          }
        })

        if (hasNew) {
          playChime()
        }
      } catch (err) {
        console.error('Failed to retrieve critical alerts:', err)
      }
    }

    // Initial fetch
    fetchAlerts()

    // Poll every 12 seconds
    const interval = setInterval(fetchAlerts, 12000)
    return () => clearInterval(interval)
  }, [user])

  if (alerts.length === 0) return null

  const mainAlert = alerts[0]

  return (
    <div className="bg-gradient-to-r from-red-600 via-rose-600 to-red-700 text-white px-4 py-3 shadow-lg flex items-center justify-between animate-pulse">
      <div className="flex items-center gap-3">
        <span className="text-[10px] font-black uppercase tracking-widest px-2 py-0.5 rounded bg-white/20 border border-white/10 animate-bounce">Critical</span>
        <div>
          <p className="font-extrabold text-sm tracking-wide">
            CRITICAL LAB VALUE TRIGGERED ({alerts.length} Pending Alert{alerts.length > 1 ? 's' : ''})
          </p>
          <p className="text-red-100 text-xs mt-0.5 font-medium">
            Patient: <span className="font-bold underline">{mainAlert.patientName}</span> | Test: <span className="font-bold">{mainAlert.testName}</span> (Accession: {mainAlert.accessionNumber})
          </p>
        </div>
      </div>
      <div className="flex items-center gap-3">
        <button
          onClick={() => navigate(`/lab-orders/${mainAlert.labOrderId}`)}
          className="px-3 py-1 bg-white hover:bg-red-50 text-red-700 rounded-lg text-xs font-extrabold shadow-sm transition-all duration-150 cursor-pointer"
        >
          Record Call Log
        </button>
      </div>
    </div>
  )
}
