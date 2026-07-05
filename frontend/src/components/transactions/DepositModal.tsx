import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { DepositSchema, type DepositInput } from '@/types/transaction'
import { useDeposit } from '@/hooks/useTransactions'
import Button from '@/components/ui/Button'
import type { Account } from '@/types/account'
import { formatCurrency } from '@/lib/formatters'

interface DepositModalProps {
  account: Account
  onClose: () => void
}

export default function DepositModal({ account, onClose }: DepositModalProps) {
  const deposit = useDeposit()
  const { register, handleSubmit, formState: { errors } } = useForm<DepositInput>({
    resolver: zodResolver(DepositSchema),
    defaultValues: { accountId: account.id },
  })

  const onSubmit = async (data: DepositInput) => {
    await deposit.mutateAsync(data)
    onClose()
  }

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
      <div className="bg-surface-900 border border-gray-800 rounded-xl p-6 w-full max-w-md">
        <h2 className="text-lg font-semibold mb-1">Depósito</h2>
        <p className="text-sm text-gray-400 mb-5">{account.name} · Saldo atual: {formatCurrency(account.balance)}</p>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <input type="hidden" {...register('accountId')} />

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
              placeholder="Ex: Salário, aluguel..."
            />
          </div>

          {deposit.error && (
            <p className="text-sm text-red-400">{(deposit.error as Error).message}</p>
          )}

          <div className="flex gap-3 pt-2">
            <Button type="button" variant="secondary" className="flex-1" onClick={onClose}>Cancelar</Button>
            <Button type="submit" className="flex-1" loading={deposit.isPending}>Depositar</Button>
          </div>
        </form>
      </div>
    </div>
  )
}
