import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { useDebounce } from './useDebounce'

describe('useDebounce', () => {
  beforeEach(() => {
    vi.useFakeTimers()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('should return initial value immediately', () => {
    const { result } = renderHook(() => useDebounce('hello', 300))
    expect(result.current).toBe('hello')
  })

  it('should update the value only after the delay has passed', () => {
    const { result, rerender } = renderHook(
      ({ value }) => useDebounce(value, 300),
      { initialProps: { value: 'initial' } }
    )

    expect(result.current).toBe('initial')

    // Change value
    rerender({ value: 'changed' })
    
    // Should still be 'initial' immediately after change
    expect(result.current).toBe('initial')

    // Advance timer by 150ms (less than 300ms delay)
    act(() => {
      vi.advanceTimersByTime(150)
    })
    expect(result.current).toBe('initial')

    // Advance another 150ms (completes the 300ms delay)
    act(() => {
      vi.advanceTimersByTime(150)
    })
    expect(result.current).toBe('changed')
  })
})
