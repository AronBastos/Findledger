import { useMutation, useQueryClient } from '@tanstack/react-query'
import { transactionsService } from '@/services/transactions.service'
import type { DepositInput, TransferInput } from '@/types/transaction'

export function useDeposit() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: DepositInput) => transactionsService.deposit(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounts'] })
      qc.invalidateQueries({ queryKey: ['statement'] })
    },
  })
}

export function useWithdraw() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: DepositInput) => transactionsService.withdraw(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounts'] })
      qc.invalidateQueries({ queryKey: ['statement'] })
    },
  })
}

export function useTransfer() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: TransferInput) => transactionsService.transfer(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['accounts'] })
      qc.invalidateQueries({ queryKey: ['statement'] })
    },
  })
}
