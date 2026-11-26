import { defineStore } from "pinia"
import { ref, watch } from "vue"

export type ThemePreference = "system" | "light" | "dark"

const STORAGE_KEY = "preferences.theme"

function readTheme(): ThemePreference {
	try {
		const stored = localStorage.getItem(STORAGE_KEY)
		return stored === "light" || stored === "dark" ? stored : "system"
	} catch {
		// Storage can be blocked (private windows, strict settings); the app still works without it.
		return "system"
	}
}

/** Per-device preferences, kept in localStorage. Put shared or server-side state in the API instead. */
export const usePreferencesStore = defineStore("preferences", () => {
	const theme = ref<ThemePreference>(readTheme())

	watch(theme, (value) => {
		try {
			localStorage.setItem(STORAGE_KEY, value)
		} catch {
			// Not saved; the choice still applies until the page reloads.
		}
	})

	return { theme }
})
