import dayjs from 'dayjs'
import 'dayjs/locale/pt-br'
import relativeTime from 'dayjs/plugin/relativeTime'

dayjs.extend(relativeTime)
dayjs.locale('pt-br')

export const formatCurrency = (value: number): string =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)

export const formatDate = (iso: string): string =>
  dayjs(iso).format('DD/MM/YYYY HH:mm')

export const formatRelative = (iso: string): string =>
  dayjs(iso).fromNow()

export const transactionTypeLabel: Record<string, string> = {
  Deposit: 'Depósito',
  Withdrawal: 'Saque',
  TransferDebit: 'Transferência enviada',
  TransferCredit: 'Transferência recebida',
}

export const accountTypeLabel: Record<string, string> = {
  Checking: 'Conta Corrente',
  Savings: 'Poupança',
}
