import Badge from '@/components/ui/Badge'
import { transactionTypeLabel } from '@/lib/formatters'

const typeVariant: Record<string, 'green' | 'red' | 'blue'> = {
  Deposit: 'green',
  Withdrawal: 'red',
  TransferDebit: 'blue',
  TransferCredit: 'blue',
}

export default function TransactionBadge({ type }: { type: string }) {
  return (
    <Badge variant={typeVariant[type] ?? 'gray'}>
      {transactionTypeLabel[type] ?? type}
    </Badge>
  )
}
