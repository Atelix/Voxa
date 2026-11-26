/**
 * main.ts
 *
 * Bootstraps Vuetify and the other plugins, then mounts the app.
 */
import App from "./App.vue"
import "./registerServiceWorker"

import { createApp } from "vue"
import { registerPlugins } from "@/plugins"

const app = createApp(App)

registerPlugins(app)

app.mount("#app")
