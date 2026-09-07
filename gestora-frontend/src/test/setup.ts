import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'

// Ogni test monta i propri componenti: senza questo, il DOM del test precedente resta appeso e le
// query trovano due volte lo stesso elemento.
afterEach(() => {
  cleanup()
})
