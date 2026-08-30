/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        primary: '#142d55',
        accent: '#2980b9',
      },
    },
  },
  plugins: [],
};
