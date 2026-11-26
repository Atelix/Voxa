/**
 * plugins/index.ts
 *
 * Registers every plugin; called from `./src/main.ts`.
 */
import type { App } from "vue"

import vuetify from "./vuetify"
import pinia from "@/store"
import router from "@/router"
import { i18n } from "./i18n"

export function registerPlugins(app: App): void {
	app.use(i18n).use(vuetify).use(router).use(pinia)
}

export { i18n } from "./i18n"
