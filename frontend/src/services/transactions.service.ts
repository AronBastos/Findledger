import api from '@/lib/api'
import type { DepositInput, TransferInput } from '@/types/transaction'

export const transactionsService = {
  deposit: (data: DepositInput) =>
    api.post<{ transactionId: string }>('/transactions/deposit', data).then((r) => r.data),

  withdraw: (data: DepositInput) =>
    api.post<{ transactionId: string }>('/transactions/withdraw', data).then((r) => r.data),

  transfer: (data: TransferInput) =>
    api
      .post<{ debitTransactionId: string; creditTransactionId: string }>(
        '/transactions/transfer',
        data,
      )
      .then((r) => r.data),
}
