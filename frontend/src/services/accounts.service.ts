import api from '@/lib/api'
import type { Account, CreateAccountInput } from '@/types/account'
import type { PagedResult, StatementLine } from '@/types/transaction'

export const accountsService = {
  getAll: (ownerName: string) =>
    api.get<Account[]>('/accounts', { params: { ownerName } }).then((r) => r.data),

  create: (data: CreateAccountInput) =>
    api.post<{ id: string }>('/accounts', data).then((r) => r.data),

  close: (id: string) =>
    api.delete(`/accounts/${id}`),

  getStatement: (accountId: string, page: number, pageSize = 20) =>
    api
      .get<PagedResult<StatementLine>>(`/accounts/${accountId}/statement`, {
        params: { page, pageSize },
      })
      .then((r) => r.data),
}
