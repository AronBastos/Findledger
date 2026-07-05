import { useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { useAccounts } from '@/hooks/useAccounts'
import { useStatement } from '@/hooks/useStatement'
import Card from '@/components/ui/Card'
import Badge from '@/components/ui/Badge'
import Button from '@/components/ui/Button'
import Spinner from '@/components/ui/Spinner'
import TransactionTable from '@/components/transactions/TransactionTable'
import DepositModal from '@/components/transactions/DepositModal'
import TransferModal from '@/components/transactions/TransferModal'
import { formatCurrency, accountTypeLabel } from '@/lib/formatters'

export default function AccountDetail() {
  const { id } = useParams<{ id: string }>()
  const { data: accounts = [] } = useAccounts()
  const account = accounts.find((a) => a.id === id)

  const [page, setPage] = useState(1)
  const { data: statement, isLoading } = useStatement(id!, page)

  const [showDeposit, setShowDeposit] = useState(false)
  const [showTransfer, setShowTransfer] = useState(false)

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2 text-sm text-gray-500">
        <Link to="/accounts" className="hover:text-white transition-colors">Contas</Link>
        <span>/</span>
        <span className="text-white">{account?.name ?? 'Carregando...'}</span>
      </div>

      {account && (
        <Card className="bg-gradient-to-r from-surface-900 to-surface-800">
          <div className="flex items-start justify-between">
            <div>
              <div className="flex items-center gap-2 mb-1">
                <h1 className="text-xl font-bold text-white">{account.name}</h1>
                <Badge variant={account.type === 'Savings' ? 'indigo' : 'gray'}>
                  {accountTypeLabel[account.type]}
                </Badge>
              </div>
              <p className="text-3xl font-bold text-white mt-2">{formatCurrency(account.balance)}</p>
              <p className="text-xs text-gray-500 mt-1">Saldo atual</p>
            </div>
            <div className="flex gap-2">
              <Button size="sm" variant="secondary" onClick={() => setShowDeposit(true)}>Depositar</Button>
              <Button size="sm" onClick={() => setShowTransfer(true)}>Transferir</Button>
            </div>
          </div>
        </Card>
      )}

      <Card noPadding>
        <div className="p-5 border-b border-gray-800 flex items-center justify-between">
          <div>
            <h2 className="font-semibold text-white">Extrato</h2>
            <p className="text-xs text-gray-500 mt-0.5">
              Saldo corrente calculado via window function SQL —{' '}
              <code className="text-indigo-400">SUM() OVER (ORDER BY created_at)</code>
            </p>
          </div>
          {statement && (
            <span className="text-xs text-gray-500">{statement.totalCount} transação(ões)</span>
          )}
        </div>

        <div className="p-5">
          {isLoading ? (
            <div className="flex justify-center py-8"><Spinner /></div>
          ) : (
            <TransactionTable lines={statement?.items ?? []} />
          )}
        </div>

        {statement && statement.totalPages > 1 && (
          <div className="px-5 pb-5 flex items-center justify-between">
            <Button
              size="sm"
              variant="secondary"
              disabled={!statement.hasPreviousPage}
              onClick={() => setPage((p) => p - 1)}
            >
              ← Anterior
            </Button>
            <span className="text-sm text-gray-400">
              Página {statement.page} de {statement.totalPages}
            </span>
            <Button
              size="sm"
              variant="secondary"
              disabled={!statement.hasNextPage}
              onClick={() => setPage((p) => p + 1)}
            >
              Próxima →
            </Button>
          </div>
        )}
      </Card>

      {account && showDeposit && (
        <DepositModal account={account} onClose={() => setShowDeposit(false)} />
      )}
      {showTransfer && (
        <TransferModal defaultSourceId={id} onClose={() => setShowTransfer(false)} />
      )}
    </div>
  )
}
