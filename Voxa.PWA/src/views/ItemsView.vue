<template>
	<v-container max-width="840" class="py-6">
		<div class="d-flex align-center mb-4 ga-2">
			<h1 class="text-headline-small">{{ $t("items.title") }}</h1>
			<v-spacer />
			<v-btn color="primary" prepend-icon="mdi-plus" @click="openDialog(null)">{{ $t("items.add") }}</v-btn>
		</div>

		<!-- Loading state rules: AGENTS.md, "PWA loading states". -->
		<v-skeleton-loader v-if="loader.loading.value && !loader.data.value" type="list-item-two-line@4" />

		<v-alert v-else-if="loader.error.value" type="error" variant="tonal">
			{{ $t(loader.error.value.messageKey) }}
			<template #append>
				<v-btn variant="text" @click="loader.load()">{{ $t("items.retry") }}</v-btn>
			</template>
		</v-alert>

		<v-card v-else-if="!items.length" variant="outlined" class="pa-8 text-center">
			<v-icon icon="mdi-checkbox-multiple-blank-outline" size="48" class="mb-2 text-medium-emphasis" />
			<p class="text-body-large text-medium-emphasis">{{ $t("items.empty") }}</p>
		</v-card>

		<v-card v-else variant="outlined">
			<v-list lines="two">
				<v-list-item
					v-for="item in items"
					:key="item.id"
					:title="item.name"
					:subtitle="item.description ?? undefined"
					@click="openDialog(item)">
					<template #prepend>
						<v-checkbox-btn
							:model-value="item.done"
							:aria-label="$t('items.done')"
							@click.stop
							@update:model-value="toggle(item, $event)" />
					</template>
					<template #append>
						<v-btn
							icon="mdi-delete-outline"
							variant="text"
							:aria-label="$t('items.delete')"
							@click.stop="remove(item)" />
					</template>
				</v-list-item>
			</v-list>
		</v-card>

		<item-dialog v-model="dialogOpen" :item="editing" @saved="onSaved" />

		<v-snackbar v-model="snackbar" timeout="3000">{{ snackbarText }}</v-snackbar>
	</v-container>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useI18n } from "vue-i18n"
import ItemDialog from "@/components/ItemDialog.vue"
import type { ItemModel } from "@/logic/api"
import { normalizeApiError } from "@/logic/ApiError"
import { useApi } from "@/services/useApi"
import { useLoader } from "@/services/useLoader"

const api = useApi()
const { t } = useI18n()
const loader = useLoader(async () => (await api.items.itemsList()).data)
const items = computed(() => loader.data.value ?? [])

const dialogOpen = ref(false)
const editing = ref<ItemModel | null>(null)
const snackbar = ref(false)
const snackbarText = ref("")

onMounted(() => void loader.load())

function notify(text: string): void {
	snackbarText.value = text
	snackbar.value = true
}

function openDialog(item: ItemModel | null): void {
	editing.value = item
	dialogOpen.value = true
}

async function onSaved(): Promise<void> {
	dialogOpen.value = false
	notify(t("items.saved"))
	// The list stays visible while it reloads (AGENTS.md).
	await loader.load()
}

async function toggle(item: ItemModel, done: unknown): Promise<void> {
	try {
		await api.items.itemsUpdate(item.id, { name: item.name, description: item.description, done: done === true })
		await loader.load()
	} catch (error) {
		notify(t(normalizeApiError(error).messageKey))
	}
}

async function remove(item: ItemModel): Promise<void> {
	try {
		await api.items.itemsDelete(item.id)
		notify(t("items.deleted"))
		await loader.load()
	} catch (error) {
		notify(t(normalizeApiError(error).messageKey))
	}
}
</script>
