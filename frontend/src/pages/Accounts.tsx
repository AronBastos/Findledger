import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useAccounts, useCloseAccount } from '@/hooks/useAccounts'
import Card from '@/components/ui/Card'
import Badge from '@/components/ui/Badge'
import Button from '@/components/ui/Button'
import Spinner from '@/components/ui/Spinner'
import CreateAccountModal from '@/components/accounts/CreateAccountModal'
import DepositModal from '@/components/transactions/DepositModal'
import TransferModal from '@/components/transactions/TransferModal'
import { formatCurrency, formatDate, accountTypeLabel } from '@/lib/formatters'
import type { Account } from '@/types/account'

export default function Accounts() {
  const { data: accounts = [], isLoading } = useAccounts()
  const closeAccount = useCloseAccount()
  const [showCreate, setShowCreate] = useState(false)
  const [depositAccount, setDepositAccount] = useState<Account | null>(null)
  const [transferSourceId, setTransferSourceId] = useState<string | null>(null)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-white">Contas</h1>
        <Button size="sm" onClick={() => setShowCreate(true)}>+ Nova Conta</Button>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-12"><Spinner /></div>
      ) : (
        <div className="space-y-3">
          {accounts.map((account) => (
            <Card key={account.id}>
              <div className="flex items-center gap-4">
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <Link to={`/accounts/${account.id}`} className="font-medium text-white hover:text-indigo-400 transition-colors">
                      {account.name}
                    </Link>
                    <Badge variant={account.type === 'Savings' ? 'indigo' : 'gray'}>
                      {accountTypeLabel[account.type]}
                    </Badge>
                  </div>
                  <p className="text-xs text-gray-500 mt-0.5">Criada em {formatDate(account.createdAt)}</p>
                </div>
                <div className="text-right">
                  <p className="text-xl font-bold text-white">{formatCurrency(account.balance)}</p>
                </div>
                <div className="flex gap-2 shrink-0">
                  <Button size="sm" variant="secondary" onClick={() => setDepositAccount(account)}>
                    Depositar
                  </Button>
                  <Button size="sm" variant="secondary" onClick={() => setTransferSourceId(account.id)}>
                    Transferir
                  </Button>
                  <Button
                    size="sm"
                    variant="danger"
                    loading={closeAccount.isPending}
                    onClick={() => {
                      if (confirm(`Encerrar conta "${account.name}"?`))
                        closeAccount.mutate(account.id)
                    }}
                  >
                    Encerrar
                  </Button>
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      {showCreate && <CreateAccountModal onClose={() => setShowCreate(false)} />}
      {depositAccount && <DepositModal account={depositAccount} onClose={() => setDepositAccount(null)} />}
      {transferSourceId && (
        <TransferModal defaultSourceId={transferSourceId} onClose={() => setTransferSourceId(null)} />
      )}
    </div>
  )
}
