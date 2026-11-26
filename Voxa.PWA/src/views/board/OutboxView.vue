<template>
	<h1 class="voxa-heading text-primary mb-1" style="font-size: 1.8rem">{{ $t("board.outbox.title") }}</h1>
	<p class="text-body-medium text-medium-emphasis mb-6">{{ $t("board.outbox.lead") }}</p>
	<v-skeleton-loader v-if="loader.loading.value && !loader.data.value" type="list-item-three-line@4" />
	<v-alert v-else-if="loader.error.value" type="error" variant="tonal">{{ $t(loader.error.value.messageKey) }}</v-alert>
	<v-card v-else-if="!mails.length" variant="outlined" class="pa-6 text-center text-medium-emphasis">{{
		$t("board.outbox.empty")
	}}</v-card>
	<v-expansion-panels v-else>
		<v-expansion-panel v-for="(mail, index) in mails" :key="index">
			<v-expansion-panel-title>
				<div class="d-flex flex-column">
					<span class="font-weight-bold">{{ mail.to }}</span>
					<span class="text-body-small text-medium-emphasis"
						>{{ mail.subject }} · {{ formatDateTime(mail.sentAt) }}</span
					>
				</div>
			</v-expansion-panel-title>
			<v-expansion-panel-text>
				<pre class="text-body-medium" style="white-space: pre-wrap; font-family: inherit">{{ mail.text }}</pre>
				<v-btn
					v-if="link(mail.text)"
					class="mt-3"
					color="primary"
					:href="link(mail.text) ?? undefined"
					target="_blank"
					>{{ $t("board.outbox.open") }}</v-btn
				>
			</v-expansion-panel-text>
		</v-expansion-panel>
	</v-expansion-panels>
</template>

<script setup lang="ts">
import { computed, onMounted } from "vue"
import { formatDateTime } from "@/services/format"
import { useApi } from "@/services/useApi"
import { useLoader } from "@/services/useLoader"

const api = useApi()
const loader = useLoader(async () => (await api.admin.adminDataOutboxList()).data)
const mails = computed(() => loader.data.value ?? [])

onMounted(() => void loader.load())

/** The voting link in the mail, pointing at this app, so the board can try the code at once. */
function link(text: string): string | null {
	const match = /#code=([A-Z0-9]+)/.exec(text)
	return match ? `${window.location.origin}/#code=${match[1]}` : null
}
</script>
