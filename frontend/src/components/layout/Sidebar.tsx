import { NavLink } from 'react-router-dom'
import { clsx } from 'clsx'

const navItems = [
  { to: '/', label: 'Dashboard', end: true },
  { to: '/accounts', label: 'Contas', end: false },
]

export default function Sidebar() {
  return (
    <aside className="w-56 shrink-0 bg-surface-900 border-r border-gray-800 flex flex-col">
      <div className="p-5 border-b border-gray-800">
        <div className="flex items-center gap-2">
          <div className="w-7 h-7 bg-indigo-600 rounded-lg flex items-center justify-center text-xs font-bold">
            FL
          </div>
          <span className="font-semibold text-white">FinLedger</span>
        </div>
        <p className="text-xs text-gray-500 mt-1">Financial Ledger API</p>
      </div>

      <nav className="flex-1 p-3 space-y-1">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            end={item.end}
            className={({ isActive }) =>
              clsx(
                'flex items-center gap-2 px-3 py-2 rounded-lg text-sm transition-colors',
                isActive
                  ? 'bg-indigo-600/20 text-indigo-400'
                  : 'text-gray-400 hover:text-white hover:bg-surface-800',
              )
            }
          >
            {item.label}
          </NavLink>
        ))}
      </nav>

      <div className="p-4 border-t border-gray-800">
        <p className="text-xs text-gray-600">C# .NET 9 + TypeScript</p>
        <p className="text-xs text-gray-600">Clean Architecture + CQRS</p>
      </div>
    </aside>
  )
}
