import { z } from 'zod'

export const loginSchema = z.object({
  email: z.string().trim().min(1, 'Enter your email').max(254),
  password: z.string().min(1, 'Enter your password').max(128),
})

export type LoginValues = z.infer<typeof loginSchema>

const DISPLAY_NAME = /^[\p{L}\p{N}][\p{L}\p{N} ._'-]{1,39}$/u

export const registerSchema = z.object({
  displayName: z.string().trim().regex(DISPLAY_NAME, "Use 2 to 40 letters, digits, spaces or . _ ' -"),
  email: z.email('Enter a valid email address').max(254),
  password: z
    .string()
    .min(8, 'Use at least 8 characters')
    .max(128, 'Use at most 128 characters')
    .regex(/[A-Za-z]/, 'Include at least one letter')
    .regex(/[0-9]/, 'Include at least one digit'),
})

export type RegisterValues = z.infer<typeof registerSchema>
