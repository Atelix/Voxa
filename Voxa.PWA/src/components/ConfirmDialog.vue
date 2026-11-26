<template>
	<v-dialog :model-value="modelValue" max-width="460" @update:model-value="emit('update:modelValue', $event)">
		<v-card :title="title" :text="text">
			<slot />
			<v-card-actions>
				<v-spacer />
				<v-btn variant="text" @click="emit('update:modelValue', false)">{{ $t("common.cancel") }}</v-btn>
				<v-btn color="error" variant="flat" :disabled="disabled" @click="confirmed">{{ confirm }}</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
defineProps<{ modelValue: boolean; title: string; text: string; confirm: string; disabled?: boolean }>()
const emit = defineEmits<{ "update:modelValue": [boolean]; confirm: [] }>()

function confirmed(): void {
	emit("update:modelValue", false)
	emit("confirm")
}
</script>
