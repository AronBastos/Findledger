import { z } from 'zod'

export const TransactionTypeSchema = z.enum([
  'Deposit',
  'Withdrawal',
  'TransferDebit',
  'TransferCredit',
])
export type TransactionType = z.infer<typeof TransactionTypeSchema>

export const StatementLineSchema = z.object({
  transactionId: z.string().uuid(),
  type: z.string(),
  amount: z.number(),
  runningBalance: z.number(),
  description: z.string().nullable(),
  createdAt: z.string(),
})
export type StatementLine = z.infer<typeof StatementLineSchema>

export const PagedResultSchema = <T extends z.ZodType>(itemSchema: T) =>
  z.object({
    items: z.array(itemSchema),
    totalCount: z.number(),
    page: z.number(),
    pageSize: z.number(),
    totalPages: z.number(),
    hasNextPage: z.boolean(),
    hasPreviousPage: z.boolean(),
  })

export type PagedResult<T> = {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export const TransferSchema = z.object({
  sourceAccountId: z.string().uuid(),
  destinationAccountId: z.string().uuid(),
  amount: z.number().positive('Valor deve ser positivo'),
  description: z.string().max(500).optional(),
})
export type TransferInput = z.infer<typeof TransferSchema>

export const DepositSchema = z.object({
  accountId: z.string().uuid(),
  amount: z.number().positive('Valor deve ser positivo'),
  description: z.string().max(500).optional(),
})
export type DepositInput = z.infer<typeof DepositSchema>
