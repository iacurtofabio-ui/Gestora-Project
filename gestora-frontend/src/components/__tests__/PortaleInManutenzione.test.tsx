import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import PortaleInManutenzione from '@/components/PortaleInManutenzione'

describe('PortaleInManutenzione', () => {
  it('dice che il portale è in manutenzione e che si torna presto', () => {
    render(<PortaleInManutenzione />)

    expect(screen.getByRole('heading', { name: 'Gestora è in manutenzione' })).toBeInTheDocument()
    expect(screen.getByText(/torna a trovarci presto/i)).toBeInTheDocument()
  })
})
