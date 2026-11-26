<template>
	<v-menu>
		<template #activator="{ props: activator }">
			<v-btn v-bind="activator" icon="mdi-translate" variant="text" :size="size" :aria-label="$t('app.language')" />
		</template>
		<v-list density="compact" :selected="[preferences.language ?? 'device']" color="primary">
			<v-list-subheader>{{ $t("app.language") }}</v-list-subheader>
			<v-list-item
				v-for="locale in supportedLocales"
				:key="locale"
				:value="locale"
				:lang="locale"
				:title="languageName(locale)"
				@click="preferences.language = locale" />
			<v-list-item value="device" :title="$t('app.languageDevice')" @click="preferences.language = null" />
		</v-list>
	</v-menu>
</template>

<script setup lang="ts">
import { languageName, supportedLocales } from "@/plugins/i18n"
import { usePreferencesStore } from "@/store/PreferencesStore"

/** The language choice for this device. Every language is listed in its own name, so anyone can find theirs. */
defineProps<{ size?: string }>()
const preferences = usePreferencesStore()
</script>
