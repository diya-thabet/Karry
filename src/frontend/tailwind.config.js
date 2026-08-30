/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#eef3fb',
          100: '#d9e4f5',
          200: '#b5c9e8',
          300: '#8aa8d6',
          400: '#5d85bf',
          500: '#3a67a5',
          600: '#2a4d82',
          700: '#1f3c68',
          DEFAULT: '#142d55',
          900: '#0d1f3c',
          950: '#071327',
        },
        accent: {
          DEFAULT: '#2980b9',
          50: '#eef7fd',
          100: '#d5ecfa',
          200: '#aad8f4',
          300: '#77bfeb',
          400: '#45a4df',
          500: '#2980b9',
          600: '#1f6ba0',
          700: '#1a557d',
          800: '#17445f',
          900: '#14384e',
        },
        surface: {
          DEFAULT: '#f5f7fa',
          elevated: '#ffffff',
          muted: '#eef1f6',
        },
        ink: {
          DEFAULT: '#0f172a',
          muted: '#475569',
          faint: '#94a3b8',
        },
        success: { DEFAULT: '#16a34a', 50: '#f0fdf4', 600: '#15803d' },
        danger: { DEFAULT: '#dc2626', 50: '#fef2f2', 600: '#b91c1c' },
        warning: { DEFAULT: '#d97706', 50: '#fffbeb', 600: '#b45309' },
      },
      fontFamily: {
        sans: [
          'Inter',
          'system-ui',
          '-apple-system',
          'Segoe UI',
          'Roboto',
          'Helvetica Neue',
          'Arial',
          'sans-serif',
        ],
        mono: ['SFMono-Regular', 'Menlo', 'Consolas', 'monospace'],
      },
      boxShadow: {
        card: '0 1px 2px 0 rgb(15 23 42 / 0.05), 0 1px 3px 0 rgb(15 23 42 / 0.08)',
        pop: '0 10px 30px -6px rgb(15 23 42 / 0.18)',
      },
    },
  },
  plugins: [],
};
