import { useQuery } from '@tanstack/react-query'
import { keepPreviousData } from '@tanstack/react-query'
import { accountsService } from '@/services/accounts.service'

export function useStatement(accountId: string, page: number) {
  return useQuery({
    queryKey: ['statement', accountId, page],
    queryFn: () => accountsService.getStatement(accountId, page),
    enabled: !!accountId,
    // Mantém os dados da página anterior enquanto a próxima carrega (transição suave)
    placeholderData: keepPreviousData,
  })
}
