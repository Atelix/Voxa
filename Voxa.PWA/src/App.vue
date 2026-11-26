<template>
	<v-app>
		<v-app-bar flat border="b">
			<v-app-bar-title>{{ $t("app.title") }}</v-app-bar-title>
			<template #append>
				<v-menu>
					<template #activator="{ props: menu }">
						<v-btn v-bind="menu" icon="mdi-theme-light-dark" :aria-label="$t('app.theme')" />
					</template>
					<v-list density="compact" :selected="[preferences.theme]" @update:selected="selectTheme">
						<v-list-item value="system" prepend-icon="mdi-cellphone" :title="$t('app.themeSystem')" />
						<v-list-item value="light" prepend-icon="mdi-white-balance-sunny" :title="$t('app.themeLight')" />
						<v-list-item value="dark" prepend-icon="mdi-weather-night" :title="$t('app.themeDark')" />
					</v-list>
				</v-menu>
			</template>
		</v-app-bar>

		<v-main>
			<v-banner v-if="outdated" color="warning" icon="mdi-update" lines="one" sticky>
				<v-banner-text>{{ $t("app.versionMismatch") }}</v-banner-text>
				<template #actions>
					<v-btn variant="text" @click="reload">{{ $t("app.reload") }}</v-btn>
				</template>
			</v-banner>
			<router-view />
		</v-main>
	</v-app>
</template>

<script setup lang="ts">
import { computed, watchEffect } from "vue"
import { useTheme } from "vuetify"
import { APP_VERSION, apiVersion, versionsDiffer } from "@/services/appVersion"
import { usePreferencesStore, type ThemePreference } from "@/store/PreferencesStore"

const preferences = usePreferencesStore()
const theme = useTheme()

watchEffect(() => {
	void theme.change(preferences.theme)
})

const outdated = computed(() => versionsDiffer(APP_VERSION, apiVersion.value))

function selectTheme(selected: unknown): void {
	const [choice] = selected as ThemePreference[]
	if (choice) preferences.theme = choice
}

function reload(): void {
	window.location.reload()
}
</script>
