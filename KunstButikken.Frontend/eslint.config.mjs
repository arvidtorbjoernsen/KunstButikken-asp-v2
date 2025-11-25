import { defineConfig } from "eslint/config";
import tseslint from "typescript-eslint";
import reactPlugin from "eslint-plugin-react";

export default defineConfig([
	{
		ignores: [
			"**/node_modules/**",
			"**/.next/**",
			"**/coverage/**",
			"**/public/**",
			"**/test-results/**",
			"**/SeedImages/**",
			"**/dist/**",
			"**/build/**",
		],
	},
	// Apply typescript-eslint recommended strict configs
	...tseslint.configs.strict,
	// Add React plugin rules and JSX settings
	{
		files: ["**/*.{ts,tsx,js,jsx}"],
		plugins: { react: reactPlugin },
		languageOptions: {
			ecmaVersion: 2023,
			sourceType: "module",
			parserOptions: { ecmaFeatures: { jsx: true } },
		},
		rules: {
			"react/react-in-jsx-scope": "off",
			"react/jsx-uses-react": "off",
		},
		settings: {
			react: { version: "detect" },
		},
	},
]);
