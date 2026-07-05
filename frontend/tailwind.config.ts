import type { Config } from 'tailwindcss'

export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        surface: {
          950: '#0a0a0f',
          900: '#111118',
          800: '#1a1a24',
          700: '#242430',
        },
      },
    },
  },
  plugins: [],
} satisfies Config
