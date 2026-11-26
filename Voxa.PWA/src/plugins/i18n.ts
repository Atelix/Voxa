import { createI18n } from "vue-i18n"
import { en as vuetifyEn, nl as vuetifyNl } from "vuetify/locale"

import en from "@/languages/en"
import nl from "@/languages/nl"

/**
 * Every language the app speaks. To add one, add a file to src/languages (the test checks it has every key), an entry
 * here, and the same code in Voxa.Abstractions/Languages.cs with its Texts.<code>.resx (docs/languages.md).
 */
const languages = {
	nl: { name: "Nederlands", intl: "nl-NL", messages: nl, vuetify: vuetifyNl },
	en: { name: "English", intl: "en-GB", messages: en, vuetify: vuetifyEn },
} as const

export type AppLocale = keyof typeof languages
export const supportedLocales = Object.keys(languages) as AppLocale[]

/** Voxa is made for Dutch clubs: the language when nothing else decides. */
export const defaultLocale: AppLocale = "nl"

export function isAppLocale(value: unknown): value is AppLocale {
	return typeof value === "string" && supportedLocales.includes(value as AppLocale)
}

/** Each language in its own name, for the language menu. */
export function languageName(locale: AppLocale): string {
	return languages[locale].name
}

/** The first browser language the app has texts for. */
export function browserLocale(browserLanguages: readonly string[] | undefined): AppLocale | undefined {
	for (const language of browserLanguages ?? []) {
		const base = language.toLowerCase().split("-")[0]
		if (isAppLocale(base)) return base
	}
	return undefined
}

/** The person's own choice, else their device's language, else the club's, else Dutch. */
export function resolveLocale(
	preference: AppLocale | null,
	browserLanguages: readonly string[] | undefined,
	clubLanguage: string | null | undefined
): AppLocale {
	return preference ?? browserLocale(browserLanguages) ?? (isAppLocale(clubLanguage) ? clubLanguage : defaultLocale)
}

type Messages = typeof en & { $vuetify: typeof vuetifyEn }

// Typed by locale name (string) rather than by the keys, so Vuetify's locale adapter accepts it.
const messages: Record<string, Messages> = Object.fromEntries(
	supportedLocales.map((locale) => [locale, { ...languages[locale].messages, $vuetify: languages[locale].vuetify }])
)

export const i18n = createI18n({
	legacy: false,
	globalInjection: true,
	// App.vue refines this once the person's choice and the club's language are known.
	locale: resolveLocale(null, typeof navigator === "undefined" ? undefined : navigator.languages, undefined),
	fallbackLocale: defaultLocale,
	// $vuetify holds Vuetify's own texts (table paging, clear buttons), so they follow the app language too.
	messages,
})

/** The language the app shows now. */
export function currentLocale(): AppLocale {
	const locale = i18n.global.locale.value
	return isAppLocale(locale) ? locale : defaultLocale
}

/** The locale for dates and numbers, such as "nl-NL". Reactive: views re-render when the language changes. */
export function intlLocale(): string {
	return languages[currentLocale()].intl
}

/** Switches the app's texts, and tells the browser and screen readers which language the page is in. */
export function setLocale(locale: AppLocale): void {
	i18n.global.locale.value = locale
	if (typeof document !== "undefined") document.documentElement.lang = locale
}
