/** @type {import('tailwindcss').Config} */
module.exports = {
  darkMode: 'class',
  content: [
    './src/pages/**/*.{js,ts,jsx,tsx,mdx}',
    './src/components/**/*.{js,ts,jsx,tsx,mdx}',
    './src/app/**/*.{js,ts,jsx,tsx,mdx}',
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['var(--font-inter)', 'system-ui', 'sans-serif'],
        display: ['var(--font-outfit)', 'system-ui', 'sans-serif'],
      },
      colors: {
        brand: {
          50:  '#f0f4ff',
          100: '#e0eaff',
          200: '#c0d3ff',
          300: '#93b4fd',
          400: '#6090fa',
          500: '#3d6cf5',
          600: '#264deb',
          700: '#1e3cd8',
          800: '#1e35af',
          900: '#1e308a',
          950: '#162055',
        },
        surface: {
          DEFAULT: '#0f1117',
          50:  '#f8f9fc',
          100: '#f1f3f9',
          200: '#e2e6f3',
          300: '#c8cfea',
          700: '#1c2235',
          800: '#141929',
          900: '#0f1117',
          950: '#090b12',
        },
        accent: {
          orange: '#ff6b35',
          teal:   '#00d4aa',
          purple: '#8b5cf6',
        }
      },
      backgroundImage: {
        'hero-gradient': 'var(--hero-bg-gradient)',
        'card-gradient': 'var(--card-bg-gradient)',
        'brand-gradient': 'var(--brand-gradient)',
      },
      animation: {
        'fade-in':    'fadeIn 0.5s ease-out',
        'slide-up':   'slideUp 0.5s ease-out',
        'slide-in':   'slideIn 0.4s ease-out',
        'toast-in':   'toastIn 0.35s cubic-bezier(0.16,1,0.3,1)',
        'pulse-slow': 'pulse 3s cubic-bezier(0.4,0,0.6,1) infinite',
        'float':      'float 6s ease-in-out infinite',
        'glow':       'glow 2s ease-in-out infinite alternate',
      },
      keyframes: {
        fadeIn:  { from: { opacity: '0' },                         to: { opacity: '1' } },
        slideUp: { from: { opacity: '0', transform: 'translateY(20px)' }, to: { opacity: '1', transform: 'translateY(0)' } },
        slideIn: { from: { opacity: '0', transform: 'translateX(-10px)' }, to: { opacity: '1', transform: 'translateX(0)' } },
        toastIn: { from: { opacity: '0', transform: 'translateX(110%)' }, to: { opacity: '1', transform: 'translateX(0)' } },
        float:   { '0%,100%': { transform: 'translateY(0)' },      '50%': { transform: 'translateY(-12px)' } },
        glow:    { from: { boxShadow: '0 0 20px rgba(61,108,245,0.3)' }, to: { boxShadow: '0 0 40px rgba(61,108,245,0.6)' } },
      },
      boxShadow: {
        'glass': '0 8px 32px rgba(0,0,0,0.3), inset 0 1px 0 rgba(255,255,255,0.1)',
        'card':  '0 4px 24px rgba(0,0,0,0.2)',
        'brand': 'var(--brand-shadow)',
        'glow':  'var(--brand-glow)',
      },
      backdropBlur: { xs: '2px' },
    },
  },
  plugins: [],
}
