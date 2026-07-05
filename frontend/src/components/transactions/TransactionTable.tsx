import type { StatementLine } from '@/types/transaction'
import { formatCurrency, formatDate } from '@/lib/formatters'
import TransactionBadge from './TransactionBadge'
import { clsx } from 'clsx'

interface TransactionTableProps {
  lines: StatementLine[]
}

export default function TransactionTable({ lines }: TransactionTableProps) {
  if (lines.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500">
        Nenhuma transação encontrada.
      </div>
    )
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b border-gray-800">
            <th className="text-left pb-3 text-gray-500 font-medium">Data</th>
            <th className="text-left pb-3 text-gray-500 font-medium">Tipo</th>
            <th className="text-left pb-3 text-gray-500 font-medium">Descrição</th>
            <th className="text-right pb-3 text-gray-500 font-medium">Valor</th>
            <th className="text-right pb-3 text-gray-500 font-medium">Saldo</th>
          </tr>
        </thead>
        <tbody>
          {lines.map((line) => {
            const isCredit = line.type === 'Deposit' || line.type === 'TransferCredit'
            return (
              <tr key={line.transactionId} className="border-b border-gray-800/50 hover:bg-surface-800/30 transition-colors">
                <td className="py-3 text-gray-400 whitespace-nowrap">
                  {formatDate(line.createdAt)}
                </td>
                <td className="py-3">
                  <TransactionBadge type={line.type} />
                </td>
                <td className="py-3 text-gray-300 max-w-[200px] truncate">
                  {line.description ?? '—'}
                </td>
                <td className={clsx('py-3 text-right font-mono font-medium', isCredit ? 'text-emerald-400' : 'text-red-400')}>
                  {isCredit ? '+' : '-'}{formatCurrency(line.amount)}
                </td>
                <td className="py-3 text-right font-mono text-white">
                  {formatCurrency(line.runningBalance)}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
