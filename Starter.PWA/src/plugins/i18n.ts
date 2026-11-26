import { createI18n } from "vue-i18n"
import { en as vuetifyEn, nl as vuetifyNl } from "vuetify/locale"

import en from "@/languages/en"
import nl from "@/languages/nl"

export const supportedLocales = ["en", "nl"] as const
export type AppLocale = (typeof supportedLocales)[number]

/** The first browser language the app has texts for, else English. */
export function browserLocale(languages: readonly string[] | undefined): AppLocale {
	for (const language of languages ?? []) {
		const base = language.toLowerCase().split("-")[0]
		const match = supportedLocales.find((locale) => locale === base)
		if (match) return match
	}
	return "en"
}

type Messages = typeof en & { $vuetify: typeof vuetifyEn }

// Typed by locale name (string) rather than by the two keys, so Vuetify's locale adapter accepts it.
const messages: Record<string, Messages> = {
	en: { ...en, $vuetify: vuetifyEn },
	nl: { ...nl, $vuetify: vuetifyNl },
}

export const i18n = createI18n({
	legacy: false,
	globalInjection: true,
	locale: browserLocale(typeof navigator === "undefined" ? undefined : navigator.languages),
	fallbackLocale: "en",
	// $vuetify holds Vuetify's own texts (table paging, clear buttons), so they follow the app language too.
	messages,
})
