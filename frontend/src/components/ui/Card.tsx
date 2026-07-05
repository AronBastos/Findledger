import { clsx } from 'clsx'
import type { HTMLAttributes } from 'react'

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  noPadding?: boolean
}

export default function Card({ children, className, noPadding, ...props }: CardProps) {
  return (
    <div
      className={clsx(
        'bg-surface-900 border border-gray-800 rounded-xl',
        !noPadding && 'p-5',
        className,
      )}
      {...props}
    >
      {children}
    </div>
  )
}
