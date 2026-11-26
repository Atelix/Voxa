/**
 * plugins/vuetify.ts
 *
 * The theme and component defaults: change the brand here, not per component (see the material-design skill).
 * Framework documentation: https://vuetifyjs.com
 */
import "@mdi/font/css/materialdesignicons.css"
import "vuetify/styles"
import "@/styles/main.css"
import "./fonts"

import { createVuetify } from "vuetify"
import { createVueI18nAdapter } from "vuetify/locale/adapters/vue-i18n"
import { useI18n } from "vue-i18n"
import { i18n } from "./i18n"

export default createVuetify({
	// Vuetify's own texts ("Items per page", "Clear") follow the app language.
	locale: { adapter: createVueI18nAdapter({ i18n, useI18n }) },
	defaults: {
		VBtn: { rounded: "lg" },
		VCard: { rounded: "lg" },
		VTextField: { variant: "outlined", density: "comfortable" },
		VTextarea: { variant: "outlined", density: "comfortable" },
		VSelect: { variant: "outlined", density: "comfortable" },
	},
	theme: {
		defaultTheme: "light",
		themes: {
			// The club's colours replace primary and secondary at start-up (App.vue, ClubStore).
			light: {
				dark: false,
				colors: {
					primary: "#1F2A44",
					secondary: "#4FB3D9",
					background: "#F4F6FA",
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
					primary: "#4FB3D9",
					secondary: "#4FB3D9",
					background: "#111318",
					surface: "#1C1F26",
				},
			},
		},
	},
})
