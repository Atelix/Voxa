<template>
	<v-dialog
		:model-value="modelValue"
		max-width="560"
		:fullscreen="$vuetify.display.mobile"
		@update:model-value="emit('update:modelValue', $event)">
		<v-card :title="item ? $t('items.edit') : $t('items.add')">
			<v-form ref="form" @submit.prevent="submit">
				<v-card-text>
					<v-text-field
						v-model="name"
						:label="$t('items.name')"
						:rules="[(value: string) => !!value?.trim() || $t('items.nameRequired')]"
						:error-messages="nameError"
						maxlength="200"
						autofocus />
					<v-textarea v-model="description" :label="$t('items.description')" maxlength="2000" rows="3" auto-grow />
					<v-checkbox v-model="done" :label="$t('items.done')" hide-details />
					<v-alert v-if="action.error.value && !nameError" type="error" variant="tonal" class="mt-4">
						{{ $t(action.error.value.messageKey) }}
					</v-alert>
				</v-card-text>
				<v-card-actions>
					<v-spacer />
					<v-btn variant="text" @click="emit('update:modelValue', false)">{{ $t("items.cancel") }}</v-btn>
					<v-btn type="submit" color="primary" variant="flat" :loading="action.pending.value">
						{{ $t("items.save") }}
					</v-btn>
				</v-card-actions>
			</v-form>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue"
import type { VForm } from "vuetify/components"
import type { ItemModel } from "@/logic/api"
import { useApi } from "@/services/useApi"
import { useAsyncAction } from "@/services/useAsyncAction"

const props = defineProps<{ modelValue: boolean; item: ItemModel | null }>()
const emit = defineEmits<{ "update:modelValue": [open: boolean]; saved: [item: ItemModel] }>()

const api = useApi()
const action = useAsyncAction<ItemModel>()
const form = ref<VForm>()
const name = ref("")
const description = ref("")
const done = ref(false)

watch(
	() => props.modelValue,
	(open) => {
		if (!open) return
		name.value = props.item?.name ?? ""
		description.value = props.item?.description ?? ""
		done.value = props.item?.done ?? false
		action.error.value = null
	}
)

// A duplicate name (409) or a server-side validation error belongs next to the name field.
const nameError = computed(() => {
	const error = action.error.value
	if (!error) return undefined
	if (error.kind === "conflict") return error.problem?.detail
	return error.fieldError("name")
})

async function submit(): Promise<void> {
	const { valid } = (await form.value?.validate()) ?? { valid: false }
	if (!valid) return
	const request = { name: name.value, description: description.value || null, done: done.value }
	const saved = await action.run(async () => {
		const id = props.item?.id
		if (id === undefined) return (await api.items.itemsCreate(request)).data.data
		return (await api.items.itemsUpdate(id, request)).data.data
	})
	if (saved) emit("saved", saved)
}
</script>
