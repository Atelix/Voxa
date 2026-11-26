<template>
	<v-card class="pa-5">
		<h2 class="voxa-heading mb-1" style="font-size: 1.1rem">4. {{ $t("board.voters.section") }}</h2>
		<p class="text-body-medium text-medium-emphasis mb-4">{{ $t("board.voters.lead") }}</p>

		<v-skeleton-loader v-if="loader.loading.value && !list" type="list-item-two-line@3" />
		<template v-else-if="list">
			<div class="d-flex flex-wrap ga-6 mb-4">
				<div>
					<div class="text-h5 text-primary">{{ list.total }}</div>
					<div class="text-body-small">{{ $t("board.voters.total") }}</div>
				</div>
				<div>
					<div class="text-h5 text-primary">{{ list.viaGuardian }}</div>
					<div class="text-body-small">{{ $t("board.voters.viaGuardian") }}</div>
				</div>
				<div>
					<div class="text-h5 text-primary">{{ list.withoutEmail }}</div>
					<div class="text-body-small">{{ $t("board.voters.withoutEmail") }}</div>
				</div>
				<div>
					<div class="text-h5 text-primary">{{ list.codesSent }}</div>
					<div class="text-body-small">{{ $t("board.voters.sent") }}</div>
				</div>
			</div>

			<div v-if="!list.codesSent" class="d-flex flex-wrap ga-2 mb-2">
				<v-btn
					variant="outlined"
					prepend-icon="mdi-file-delimited-outline"
					:loading="busy === 'csv'"
					@click="fileInput?.click()"
					>{{ $t("board.voters.fromCsv") }}</v-btn
				>
				<input ref="fileInput" type="file" accept=".csv,text/csv,text/plain" hidden @change="importCsv" />
			</div>
			<p v-if="!list.codesSent" class="text-body-small text-medium-emphasis mb-4">{{ $t("board.voters.csvHelp") }}</p>

			<v-alert
				v-if="importResult"
				type="success"
				variant="tonal"
				class="mb-4"
				closable
				@click:close="importResult = null">
				{{
					$t("board.voters.imported", {
						n: importResult.imported,
						guardian: importResult.viaGuardian,
						noEmail: importResult.withoutEmail,
					})
				}}
				<div v-if="importResult.skipped.length" class="text-body-small mt-1">
					{{ $t("board.voters.skipped") }} {{ importResult.skipped.join(", ") }}
				</div>
			</v-alert>

			<v-expansion-panels variant="accordion" class="mb-4">
				<v-expansion-panel :title="$t('board.voters.list', { n: list.total })">
					<v-expansion-panel-text>
						<div class="d-flex ga-2 mb-2">
							<v-text-field v-model="newVoter.name" :label="$t('board.voters.name')" density="compact" hide-details />
							<v-text-field v-model="newVoter.email" :label="$t('board.voters.email')" density="compact" hide-details />
							<v-btn :disabled="!newVoter.name" :loading="busy === 'add'" @click="addVoter">{{
								$t("common.add")
							}}</v-btn>
						</div>
						<v-table density="compact">
							<tbody>
								<tr v-for="voter in list.voters" :key="voter.id">
									<td>{{ voter.name }}</td>
									<td class="text-medium-emphasis">
										{{ voter.email ?? $t("board.voters.noEmail") }}
										<span v-if="voter.guardianName">
											({{ $t("board.voters.guardian", { name: voter.guardianName }) }})</span
										>
									</td>
									<td class="text-right">
										<v-icon
											v-if="voter.codeSent"
											icon="mdi-check"
											color="success"
											:aria-label="$t('board.voters.sent')" />
										<v-btn
											v-else
											icon="mdi-delete-outline"
											size="small"
											variant="text"
											:aria-label="$t('common.delete')"
											@click="removeVoter(voter.id)" />
									</td>
								</tr>
							</tbody>
						</v-table>
					</v-expansion-panel-text>
				</v-expansion-panel>
			</v-expansion-panels>

			<h2 class="voxa-heading mb-1" style="font-size: 1.1rem">5. {{ $t("board.voters.codesSection") }}</h2>
			<p class="text-body-medium text-medium-emphasis mb-4">{{ $t("board.voters.codesLead") }}</p>
			<div class="d-flex flex-wrap ga-2 align-center mb-2">
				<v-text-field
					v-model="testEmail"
					:label="$t('board.voters.testEmail')"
					density="compact"
					hide-details
					style="max-width: 280px" />
				<v-btn variant="outlined" :disabled="!testEmail" :loading="busy === 'test'" @click="sendTest">{{
					$t("board.voters.sendTest")
				}}</v-btn>
			</div>
			<div class="d-flex flex-wrap ga-2 mt-4">
				<v-btn
					color="primary"
					prepend-icon="mdi-email-fast-outline"
					:disabled="pending === 0"
					:loading="busy === 'send'"
					@click="confirmSend = true">
					{{ $t("board.voters.send", { n: pending }) }}
				</v-btn>
				<v-btn
					variant="outlined"
					prepend-icon="mdi-printer-outline"
					:loading="busy === 'paper'"
					@click="paperDialog = true"
					>{{ $t("board.voters.paper") }}</v-btn
				>
			</div>
			<v-alert v-if="sendResult" :type="sendResult.failed ? 'warning' : 'success'" variant="tonal" class="mt-4">
				{{
					$t("board.voters.sendResult", {
						sent: sendResult.sent,
						failed: sendResult.failed,
						noEmail: sendResult.withoutEmail,
					})
				}}
			</v-alert>
		</template>

		<confirm-dialog
			v-model="confirmSend"
			:title="$t('board.voters.sendTitle')"
			:text="$t('board.voters.sendConfirm', { n: pending })"
			:confirm="$t('board.voters.sendNow')"
			@confirm="send" />

		<v-dialog v-model="paperDialog" max-width="520">
			<v-card :title="$t('board.voters.paperTitle')">
				<v-card-text>
					<template v-if="!paperCodes.length">
						<p class="mb-4">{{ $t("board.voters.paperText") }}</p>
						<v-text-field
							v-model.number="paperCount"
							type="number"
							min="1"
							max="500"
							:label="$t('board.voters.paperCount')" />
					</template>
					<template v-else>
						<v-alert type="warning" variant="tonal" class="mb-4">{{ $t("board.voters.paperOnce") }}</v-alert>
						<div class="voxa-paper">
							<div v-for="code in paperCodes" :key="code" class="voxa-slip">
								<div class="text-body-small">{{ meeting.name }}</div>
								<div class="voxa-heading" style="font-size: 1.4rem; letter-spacing: 0.15em">{{ code }}</div>
								<div class="text-body-small">{{ origin }}</div>
							</div>
						</div>
					</template>
				</v-card-text>
				<v-card-actions>
					<v-spacer />
					<template v-if="!paperCodes.length">
						<v-btn variant="text" @click="paperDialog = false">{{ $t("common.cancel") }}</v-btn>
						<v-btn
							color="primary"
							variant="flat"
							:disabled="paperCount < 1"
							:loading="busy === 'paper'"
							@click="createPaper"
							>{{ $t("board.voters.paperCreate") }}</v-btn
						>
					</template>
					<template v-else>
						<v-btn variant="text" prepend-icon="mdi-printer" @click="print">{{ $t("common.print") }}</v-btn>
						<v-btn color="primary" variant="flat" @click="closePaper">{{ $t("common.close") }}</v-btn>
					</template>
				</v-card-actions>
			</v-card>
		</v-dialog>
		<v-snackbar v-model="snackbar" timeout="5000">{{ snackbarText }}</v-snackbar>
	</v-card>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue"
import { useI18n } from "vue-i18n"
import ConfirmDialog from "@/components/ConfirmDialog.vue"
import type { MeetingModel, SendCodesResultModel, VoterImportResultModel } from "@/logic/api"
import { normalizeApiError } from "@/logic/ApiError"
import { useApi } from "@/services/useApi"
import { useLoader } from "@/services/useLoader"

const props = defineProps<{ meeting: MeetingModel }>()
const emit = defineEmits<{ changed: [] }>()
const api = useApi()
const { t } = useI18n()
const loader = useLoader(async () => (await api.admin.adminMeetingsVotersList(props.meeting.id)).data)
const list = computed(() => loader.data.value)
const pending = computed(() => list.value?.voters.filter((v) => !v.codeSent && v.email).length ?? 0)
const busy = ref<string | null>(null)
const fileInput = ref<HTMLInputElement>()
const importResult = ref<VoterImportResultModel | null>(null)
const sendResult = ref<SendCodesResultModel | null>(null)
const newVoter = reactive({ name: "", email: "" })
const testEmail = ref("")
const confirmSend = ref(false)
const paperDialog = ref(false)
const paperCount = ref(1)
const paperCodes = ref<string[]>([])
const snackbar = ref(false)
const snackbarText = ref("")
const origin = window.location.host

onMounted(async () => {
	await loader.load()
	paperCount.value = Math.max(1, list.value?.voters.filter((v) => !v.codeSent && !v.email).length ?? 1)
})

async function act<T>(name: string, action: () => Promise<T>, done?: string): Promise<T | undefined> {
	busy.value = name
	try {
		const result = await action()
		if (done) {
			snackbarText.value = done
			snackbar.value = true
		}
		return result
	} catch (error) {
		const normalized = normalizeApiError(error)
		snackbarText.value = normalized.problem?.detail ?? t(normalized.messageKey)
		snackbar.value = true
		return undefined
	} finally {
		busy.value = null
	}
}

async function importCsv(event: Event): Promise<void> {
	const file = (event.target as HTMLInputElement).files?.[0]
	if (!file) return
	const csv = await file.text()
	;(event.target as HTMLInputElement).value = ""
	const result = await act(
		"csv",
		async () => (await api.admin.adminMeetingsVotersImportCreate(props.meeting.id, { csv })).data
	)
	if (result) {
		importResult.value = result
		await loader.load()
	}
}

async function addVoter(): Promise<void> {
	const added = await act("add", () =>
		api.admin.adminMeetingsVotersCreate(props.meeting.id, { name: newVoter.name, email: newVoter.email || null })
	)
	if (added) {
		newVoter.name = ""
		newVoter.email = ""
		await loader.load()
	}
}

async function removeVoter(id: number): Promise<void> {
	if (await act("remove", () => api.admin.adminMeetingsVotersDelete(id, String(props.meeting.id)))) await loader.load()
}

async function sendTest(): Promise<void> {
	await act(
		"test",
		() => api.admin.adminMeetingsVotersTestCreate(props.meeting.id, { email: testEmail.value }),
		t("board.voters.testSent")
	)
}

async function send(): Promise<void> {
	const result = await act("send", async () => (await api.admin.adminMeetingsVotersSendCreate(props.meeting.id)).data)
	if (result) {
		sendResult.value = result
		await loader.load()
		emit("changed")
	}
}

async function createPaper(): Promise<void> {
	const result = await act(
		"paper",
		async () => (await api.admin.adminMeetingsVotersPaperCreate(props.meeting.id, { count: paperCount.value })).data
	)
	if (result) {
		paperCodes.value = [...result.codes]
		await loader.load()
		emit("changed")
	}
}

function closePaper(): void {
	paperCodes.value = []
	paperDialog.value = false
}

function print(): void {
	window.print()
}
</script>

<style scoped>
.voxa-paper {
	display: grid;
	grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
	gap: 8px;
}

.voxa-slip {
	border: 1px dashed rgba(var(--v-border-color), 0.5);
	border-radius: 8px;
	padding: 10px 12px;
	text-align: center;
	break-inside: avoid;
}
</style>
