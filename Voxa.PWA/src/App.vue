<template>
	<v-app>
		<v-banner v-if="outdated" color="warning" icon="mdi-update" lines="one" sticky class="voxa-no-print">
			<v-banner-text>{{ $t("app.versionMismatch") }}</v-banner-text>
			<template #actions>
				<v-btn variant="text" @click="reload">{{ $t("app.reload") }}</v-btn>
			</template>
		</v-banner>
		<router-view />
	</v-app>
</template>

<script setup lang="ts">
import { computed, onMounted, watchEffect } from "vue"
import { useTheme } from "vuetify"
import { APP_VERSION, apiVersion, versionsDiffer } from "@/services/appVersion"
import { onColor, tint } from "@/services/theme"
import { useClubStore } from "@/store/ClubStore"
import { usePreferencesStore } from "@/store/PreferencesStore"

const preferences = usePreferencesStore()
const club = useClubStore()
const theme = useTheme()

onMounted(() => void club.load())

watchEffect(() => {
	void theme.change(preferences.theme === "system" ? "light" : preferences.theme)
})

// The club's brand drives the theme: primary and secondary colours, fonts and the page title.
watchEffect(() => {
	const brand = club.brand
	const light = theme.themes.value.light
	if (light) {
		light.colors.primary = brand.primaryColor
		light.colors.secondary = brand.secondaryColor
		light.colors["on-primary"] = onColor(brand.primaryColor)
		light.colors["on-secondary"] = onColor(brand.secondaryColor)
	}
	const dark = theme.themes.value.dark
	if (dark) {
		dark.colors.primary = brand.secondaryColor
		dark.colors["on-primary"] = onColor(brand.secondaryColor)
	}
	const root = document.documentElement.style
	root.setProperty("--voxa-heading-font", `"${brand.headingFont}"`)
	root.setProperty("--voxa-body-font", `"${brand.bodyFont}"`)
	root.setProperty("--voxa-tint", tint(brand.secondaryColor))
	document.title = brand.clubName === "Voxa" ? "Voxa" : `${brand.clubName} · Voxa`
})

const outdated = computed(() => versionsDiffer(APP_VERSION, apiVersion.value))

function reload(): void {
	window.location.reload()
}
</script>
