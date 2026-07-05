import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useAccounts } from '@/hooks/useAccounts'
import Card from '@/components/ui/Card'
import Badge from '@/components/ui/Badge'
import Button from '@/components/ui/Button'
import Spinner from '@/components/ui/Spinner'
import CreateAccountModal from '@/components/accounts/CreateAccountModal'
import TransferModal from '@/components/transactions/TransferModal'
import { formatCurrency, formatDate, accountTypeLabel } from '@/lib/formatters'

export default function Dashboard() {
  const { data: accounts = [], isLoading } = useAccounts()
  const [showCreate, setShowCreate] = useState(false)
  const [showTransfer, setShowTransfer] = useState(false)

  const totalBalance = accounts.reduce((sum, a) => sum + a.balance, 0)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-white">Dashboard</h1>
          <p className="text-gray-400 text-sm mt-0.5">demo-user · {accounts.length} conta(s)</p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" size="sm" onClick={() => setShowTransfer(true)}>
            Transferir
          </Button>
          <Button size="sm" onClick={() => setShowCreate(true)}>
            + Nova Conta
          </Button>
        </div>
      </div>

      <Card className="bg-gradient-to-br from-indigo-600/20 to-surface-900 border-indigo-600/30">
        <p className="text-sm text-gray-400">Saldo total consolidado</p>
        <p className="text-4xl font-bold text-white mt-1">{formatCurrency(totalBalance)}</p>
        <p className="text-xs text-gray-500 mt-2">Soma de todas as contas ativas</p>
      </Card>

      <div>
        <h2 className="text-sm font-medium text-gray-400 uppercase tracking-wide mb-3">Contas</h2>
        {isLoading ? (
          <div className="flex justify-center py-12"><Spinner /></div>
        ) : accounts.length === 0 ? (
          <Card>
            <p className="text-center text-gray-500 py-6">
              Nenhuma conta criada ainda.{' '}
              <button className="text-indigo-400 hover:underline" onClick={() => setShowCreate(true)}>
                Criar primeira conta
              </button>
            </p>
          </Card>
        ) : (
          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            {accounts.map((account) => (
              <Link key={account.id} to={`/accounts/${account.id}`}>
                <Card className="hover:border-gray-700 transition-colors cursor-pointer">
                  <div className="flex items-start justify-between mb-3">
                    <div>
                      <p className="font-medium text-white">{account.name}</p>
                      <p className="text-xs text-gray-500 mt-0.5">{formatDate(account.createdAt)}</p>
                    </div>
                    <Badge variant={account.type === 'Savings' ? 'indigo' : 'gray'}>
                      {accountTypeLabel[account.type]}
                    </Badge>
                  </div>
                  <p className="text-2xl font-bold text-white">{formatCurrency(account.balance)}</p>
                  <p className="text-xs text-indigo-400 mt-2">Ver extrato →</p>
                </Card>
              </Link>
            ))}
          </div>
        )}
      </div>

      {showCreate && <CreateAccountModal onClose={() => setShowCreate(false)} />}
      {showTransfer && <TransferModal onClose={() => setShowTransfer(false)} />}
    </div>
  )
}
