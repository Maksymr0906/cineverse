/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
  theme: {
    extend: {
      colors: {
        // Темно-вугільна кінематографічна палітра (п.2.3 ТЗ)
        charcoal: {
          950: "#0b0b0d",
          900: "#131316",
          850: "#17171b",
          800: "#1c1c21",
          700: "#28282f",
          600: "#3a3a42"
        },
        accent: {
          DEFAULT: "#e63946", // маркізний червоний — акцент №1 (CTA, статуси "продано/заброньовано")
          dim: "#a52834"
        },
        marquee: {
          DEFAULT: "#f2c14e" // теплий "лампочковий" жовтий — акцент №2 (бейджі, ціни, вибрані місця)
        },
        ink: {
          DEFAULT: "#f5f5f0", // теплий майже-білий для основного тексту
          muted: "#9a99a2"
        }
      },
      fontFamily: {
        display: ["'Bebas Neue'", "sans-serif"], // афішні заголовки — ефект кінотеатральної маркізи
        sans: ["'Inter'", "sans-serif"]
      }
    }
  },
  plugins: []
};
