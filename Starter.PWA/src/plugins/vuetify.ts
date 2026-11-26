/**
 * plugins/vuetify.ts
 *
 * The theme and component defaults: change the brand here, not per component (see the material-design skill).
 * Framework documentation: https://vuetifyjs.com
 */
import "@mdi/font/css/materialdesignicons.css"
import "vuetify/styles"
import "@/styles/main.css"

import { createVuetify } from "vuetify"
import { createVueI18nAdapter } from "vuetify/locale/adapters/vue-i18n"
import { useI18n } from "vue-i18n"
import { i18n } from "./i18n"

export default createVuetify({
	// Vuetify's own texts ("Items per page", "Clear") follow the app language.
	locale: { adapter: createVueI18nAdapter({ i18n, useI18n }) },
	defaults: {
		VBtn: { rounded: "sm" },
		VCard: { rounded: "sm" },
		VTextField: { variant: "outlined", density: "comfortable" },
		VTextarea: { variant: "outlined", density: "comfortable" },
		VSelect: { variant: "outlined", density: "comfortable" },
	},
	theme: {
		defaultTheme: "system",
		themes: {
			light: {
				dark: false,
				colors: {
					primary: "#111111",
					secondary: "#4A4A4A",
					background: "#FFFFFF",
					surface: "#FFFFFF",
				},
				variables: {
					// Vuetify's 0.6 is below 4.5:1 for secondary text on white; 0.7 keeps it readable.
					"medium-emphasis-opacity": 0.7,
				},
			},
			dark: {
				dark: true,
				colors: {
					primary: "#F2F2F2",
					secondary: "#BDBDBD",
					background: "#111111",
					surface: "#1C1C1C",
				},
			},
		},
	},
})
