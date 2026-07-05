import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { accountsService } from '@/services/accounts.service'
import type { CreateAccountInput } from '@/types/account'

const DEMO_OWNER = 'demo-user'

export function useAccounts() {
  return useQuery({
    queryKey: ['accounts', DEMO_OWNER],
    queryFn: () => accountsService.getAll(DEMO_OWNER),
  })
}

export function useCreateAccount() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: Omit<CreateAccountInput, 'ownerName'>) =>
      accountsService.create({ ...data, ownerName: DEMO_OWNER }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['accounts'] }),
  })
}

export function useCloseAccount() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => accountsService.close(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['accounts'] }),
  })
}
