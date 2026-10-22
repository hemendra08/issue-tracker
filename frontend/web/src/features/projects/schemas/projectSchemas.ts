import { z } from 'zod'

export const createProjectSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'Project name is required')
    .max(120, 'Project name must not exceed 120 characters'),
  key: z
    .string()
    .trim()
    .toUpperCase()
    .min(2, 'Project key must be at least 2 characters')
    .max(10, 'Project key must not exceed 10 characters')
    .regex(/^[A-Z0-9]+$/, 'Project key must contain only uppercase letters and numbers'),
  description: z
    .string()
    .trim()
    .max(2000, 'Description must not exceed 2000 characters')
    .optional()
    .or(z.literal('')),
})

export type CreateProjectFormData = z.infer<typeof createProjectSchema>

export const updateProjectSchema = z.object({
  name: z
    .string()
    .trim()
    .min(1, 'Project name cannot be empty')
    .max(120, 'Project name must not exceed 120 characters'),
  description: z
    .string()
    .trim()
    .max(2000, 'Description must not exceed 2000 characters')
    .optional()
    .or(z.literal('')),
})

export type UpdateProjectFormData = z.infer<typeof updateProjectSchema>

export const addMemberSchema = z.object({
  email: z.string().trim().email('Please enter a valid email address'),
  role: z.enum(['Admin', 'Member'], {
    message: 'Role must be either Admin or Member',
  }),
})

export type AddMemberFormData = z.infer<typeof addMemberSchema>
