import { clsx } from 'clsx'

type BadgeVariant = 'green' | 'red' | 'blue' | 'gray' | 'indigo'

interface BadgeProps {
  variant?: BadgeVariant
  children: React.ReactNode
  className?: string
}

const variants: Record<BadgeVariant, string> = {
  green: 'bg-emerald-400/10 text-emerald-400 border border-emerald-400/20',
  red: 'bg-red-400/10 text-red-400 border border-red-400/20',
  blue: 'bg-blue-400/10 text-blue-400 border border-blue-400/20',
  gray: 'bg-gray-700/50 text-gray-400 border border-gray-700',
  indigo: 'bg-indigo-400/10 text-indigo-400 border border-indigo-400/20',
}

export default function Badge({ variant = 'gray', children, className }: BadgeProps) {
  return (
    <span
      className={clsx(
        'inline-flex items-center px-2 py-0.5 rounded-md text-xs font-medium',
        variants[variant],
        className,
      )}
    >
      {children}
    </span>
  )
}
