import { defineStore } from "pinia"
import { ref, watch } from "vue"
import { isAppLocale, type AppLocale } from "@/plugins/i18n"

export type ThemePreference = "system" | "light" | "dark"

const THEME_KEY = "preferences.theme"
const LANGUAGE_KEY = "preferences.language"

function read(key: string): string | null {
	try {
		return localStorage.getItem(key)
	} catch {
		// Storage can be blocked (private windows, strict settings); the app still works without it.
		return null
	}
}

function write(key: string, value: string | null): void {
	try {
		if (value === null) localStorage.removeItem(key)
		else localStorage.setItem(key, value)
	} catch {
		// Not saved; the choice still applies until the page reloads.
	}
}

function readTheme(): ThemePreference {
	const stored = read(THEME_KEY)
	return stored === "light" || stored === "dark" ? stored : "system"
}

function readLanguage(): AppLocale | null {
	const stored = read(LANGUAGE_KEY)
	return isAppLocale(stored) ? stored : null
}

/** Per-device preferences, kept in localStorage. Put shared or server-side state in the API instead. */
export const usePreferencesStore = defineStore("preferences", () => {
	const theme = ref<ThemePreference>(readTheme())
	/** The language the person chose on this device; null follows the device, then the club. */
	const language = ref<AppLocale | null>(readLanguage())

	watch(theme, (value) => write(THEME_KEY, value))
	watch(language, (value) => write(LANGUAGE_KEY, value))

	return { theme, language }
})
