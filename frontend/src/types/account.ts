import { z } from 'zod'

export const AccountTypeSchema = z.enum(['Checking', 'Savings'])
export type AccountType = z.infer<typeof AccountTypeSchema>

export const AccountSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  type: AccountTypeSchema,
  balance: z.number(),
  createdAt: z.string(),
})
export type Account = z.infer<typeof AccountSchema>

export const CreateAccountSchema = z.object({
  ownerName: z.string().min(1, 'Nome obrigatório').max(150),
  name: z.string().min(1, 'Nome da conta obrigatório').max(100),
  type: AccountTypeSchema,
})
export type CreateAccountInput = z.infer<typeof CreateAccountSchema>
