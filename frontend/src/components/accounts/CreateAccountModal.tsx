import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { CreateAccountSchema, type CreateAccountInput } from '@/types/account'
import { useCreateAccount } from '@/hooks/useAccounts'
import Button from '@/components/ui/Button'

interface CreateAccountModalProps {
  onClose: () => void
}

export default function CreateAccountModal({ onClose }: CreateAccountModalProps) {
  const create = useCreateAccount()
  const { register, handleSubmit, formState: { errors } } = useForm<Omit<CreateAccountInput, 'ownerName'>>({
    resolver: zodResolver(CreateAccountSchema.omit({ ownerName: true })),
  })

  const onSubmit = async (data: Omit<CreateAccountInput, 'ownerName'>) => {
    await create.mutateAsync(data)
    onClose()
  }

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
      <div className="bg-surface-900 border border-gray-800 rounded-xl p-6 w-full max-w-md">
        <h2 className="text-lg font-semibold mb-5">Nova Conta</h2>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm text-gray-400 mb-1">Nome da conta</label>
            <input
              {...register('name')}
              className="w-full bg-surface-800 border border-gray-700 rounded-lg px-3 py-2 text-white focus:outline-none focus:border-indigo-500"
              placeholder="Ex: Conta Principal, Reserva..."
            />
            {errors.name && <p className="text-xs text-red-400 mt-1">{errors.name.message}</p>}
          </div>

          <div>
            <label className="block text-sm text-gray-400 mb-1">Tipo</label>
            <select
              {...register('type')}
              className="w-full bg-surface-800 border border-gray-700 rounded-lg px-3 py-2 text-white focus:outline-none focus:border-indigo-500"
            >
              <option value="Checking">Conta Corrente</option>
              <option value="Savings">Poupança</option>
            </select>
          </div>

          {create.error && (
            <p className="text-sm text-red-400">{(create.error as Error).message}</p>
          )}

          <div className="flex gap-3 pt-2">
            <Button type="button" variant="secondary" className="flex-1" onClick={onClose}>Cancelar</Button>
            <Button type="submit" className="flex-1" loading={create.isPending}>Criar conta</Button>
          </div>
        </form>
      </div>
    </div>
  )
}
