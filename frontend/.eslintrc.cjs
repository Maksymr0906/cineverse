// Виправлення після аудиту: npm run lint падав одразу з "ESLint couldn't find a configuration
// file" — пакет eslint@8.57.0 у devDependencies був, а самого конфіга не було жодного.
// Формат .eslintrc.cjs (legacy config), а не eslint.config.js (flat config), — саме тому,
// що встановлена версія ESLint 8, а flat config за замовчуванням увімкнено лише з ESLint 9.
module.exports = {
  root: true,
  env: { browser: true, es2020: true, node: true },
  extends: [
    "eslint:recommended",
    "plugin:@typescript-eslint/recommended",
    "plugin:react-hooks/recommended"
  ],
  ignorePatterns: ["dist", ".eslintrc.cjs", "vite.config.ts"],
  parser: "@typescript-eslint/parser",
  parserOptions: { ecmaVersion: "latest", sourceType: "module" },
  plugins: ["react-refresh"],
  rules: {
    "react-refresh/only-export-components": ["warn", { allowConstantExport: true }],
    "@typescript-eslint/no-unused-vars": ["warn", { argsIgnorePattern: "^_" }],
    "@typescript-eslint/no-explicit-any": "warn"
  }
};
