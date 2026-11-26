import eslint from "@eslint/js"
import eslintConfigPrettier from "eslint-config-prettier"
import eslintPluginVue from "eslint-plugin-vue"
import globals from "globals"
import typescriptEslint from "typescript-eslint"

export default typescriptEslint.config(
	// The generated API client is replaced on every regeneration (see AGENTS.md); the Worker has its own runtime.
	{ ignores: ["*.d.ts", "**/coverage", "**/dist", "src/logic/api.ts", "src/worker.ts"] },
	{
		extends: [
			eslint.configs.recommended,
			...typescriptEslint.configs.recommendedTypeChecked,
			...eslintPluginVue.configs["flat/recommended"],
		],
		files: ["**/*.{ts,vue}"],
		languageOptions: {
			ecmaVersion: "latest",
			sourceType: "module",
			globals: globals.browser,
			parserOptions: {
				parser: typescriptEslint.parser,
				// Type-aware rules such as no-floating-promises use the same TypeScript programs as type-check (the app)
				// and the Node config files, for .ts files and for <script lang="ts"> in .vue files.
				project: ["./tsconfig.app.json", "./tsconfig.node.json"],
				tsconfigRootDir: import.meta.dirname,
				extraFileExtensions: [".vue"],
			},
		},
		rules: {
			"vue/valid-v-slot": ["error", { allowModifiers: true }],
			"@typescript-eslint/no-unused-vars": "error",
			"@typescript-eslint/explicit-function-return-type": "error",
			"@typescript-eslint/explicit-module-boundary-types": "error",
		},
	},
	eslintConfigPrettier
)
