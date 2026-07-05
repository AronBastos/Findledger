import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { TransferSchema, type TransferInput } from '@/types/transaction'
import { useTransfer } from '@/hooks/useTransactions'
import { useAccounts } from '@/hooks/useAccounts'
import Button from '@/components/ui/Button'
import { formatCurrency, accountTypeLabel } from '@/lib/formatters'

interface TransferModalProps {
  defaultSourceId?: string
  onClose: () => void
}

export default function TransferModal({ defaultSourceId, onClose }: TransferModalProps) {
  const transfer = useTransfer()
  const { data: accounts = [] } = useAccounts()

  const { register, handleSubmit, watch, formState: { errors } } = useForm<TransferInput>({
    resolver: zodResolver(TransferSchema),
    defaultValues: { sourceAccountId: defaultSourceId ?? '' },
  })

  const sourceId = watch('sourceAccountId')
  const destAccounts = accounts.filter((a) => a.id !== sourceId)

  const onSubmit = async (data: TransferInput) => {
    await transfer.mutateAsync(data)
    onClose()
  }

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
      <div className="bg-surface-900 border border-gray-800 rounded-xl p-6 w-full max-w-md">
        <h2 className="text-lg font-semibold mb-1">Transferência</h2>
        <p className="text-sm text-gray-400 mb-5">Transferência ACID — atômica e segura.</p>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm text-gray-400 mb-1">Conta de origem</label>
            <select
              {...register('sourceAccountId')}
              className="w-full bg-surface-800 border border-gray-700 rounded-lg px-3 py-2 text-white focus:outline-none focus:border-indigo-500"
            >
              <option value="">Selecione...</option>
              {accounts.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.name} ({accountTypeLabel[a.type]}) · {formatCurrency(a.balance)}
                </option>
              ))}
            </select>
            {errors.sourceAccountId && <p className="text-xs text-red-400 mt-1">{errors.sourceAccountId.message}</p>}
          </div>

          <div>
            <label className="block text-sm text-gray-400 mb-1">Conta de destino</label>
            <select
              {...register('destinationAccountId')}
              className="w-full bg-surface-800 border border-gray-700 rounded-lg px-3 py-2 text-white focus:outline-none focus:border-indigo-500"
            >
              <option value="">Selecione...</option>
              {destAccounts.map((a) => (
                <option key={a.id} value={a.id}>
                  {a.name} ({accountTypeLabel[a.type]})
                </option>
              ))}
            </select>
            {errors.destinationAccountId && <p className="text-xs text-red-400 mt-1">{errors.destinationAccountId.message}</p>}
          </div>

          <div>
            <label className="block text-sm text-gray-400 mb-1">Valor (R$)</label>
            <input
              type="number"
              step="0.01"
              min="0.01"
              {...register('amount', { valueAsNumber: true })}
              className="w-full bg-surface-800 border border-gray-700 rounded-lg px-3 py-2 text-white focus:outline-none focus:border-indigo-500"
              placeholder="0,00"
            />
            {errors.amount && <p className="text-xs text-red-400 mt-1">{errors.amount.message}</p>}
          </div>

          <div>
            <label className="block text-sm text-gray-400 mb-1">Descrição (opcional)</label>
            <input
              {...register('description')}
              className="w-full bg-surface-800 border border-gray-700 rounded-lg px-3 py-2 text-white focus:outline-none focus:border-indigo-500"
            />
          </div>

          {transfer.error && (
            <p className="text-sm text-red-400">{(transfer.error as Error).message}</p>
          )}

          <div className="flex gap-3 pt-2">
            <Button type="button" variant="secondary" className="flex-1" onClick={onClose}>Cancelar</Button>
            <Button type="submit" className="flex-1" loading={transfer.isPending}>Transferir</Button>
          </div>
        </form>
      </div>
    </div>
  )
}
