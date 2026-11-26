<template>
	<v-skeleton-loader v-if="loader.loading.value && !result" type="heading, card, card" />
	<v-alert v-else-if="loader.error.value && !result" type="error" variant="tonal">
		{{ loader.error.value.kind === "authorization" ? $t("board.result.hidden") : $t(loader.error.value.messageKey) }}
	</v-alert>
	<template v-else-if="result">
		<v-btn variant="text" prepend-icon="mdi-arrow-left" class="px-0 mb-2 voxa-no-print" @click="$router.back()">{{
			$t("board.result.back")
		}}</v-btn>
		<p class="voxa-eyebrow text-medium-emphasis">{{ $t("board.result.eyebrow", { n: result.position }) }}</p>
		<div class="d-flex flex-wrap align-center ga-2 mb-6">
			<h1 class="voxa-heading text-primary" style="font-size: 1.8rem">{{ result.title }}</h1>
			<v-spacer />
			<v-btn variant="outlined" prepend-icon="mdi-printer-outline" class="voxa-no-print" @click="print">{{
				$t("board.result.print")
			}}</v-btn>
			<v-btn
				color="primary"
				prepend-icon="mdi-monitor"
				class="voxa-no-print"
				:loading="screen.pending.value"
				@click="showOnScreen"
				>{{ $t("board.result.toScreen") }}</v-btn
			>
		</div>

		<v-row>
			<v-col cols="12" md="7">
				<v-card class="pa-5">
					<v-chip :color="outcomeColor" variant="flat" class="mb-2">{{
						$t(`board.result.outcome.${result.outcome}`)
					}}</v-chip>
					<p class="text-body-medium mb-4">{{ summary }}</p>
					<result-bars :result="result" />
				</v-card>
			</v-col>
			<v-col cols="12" md="5">
				<v-card class="pa-5 mb-4">
					<div class="d-flex justify-space-between mb-1">
						<span>{{ $t("board.result.cast") }}</span><strong>{{ result.ballots.total }}</strong>
					</div>
					<div class="d-flex justify-space-between text-medium-emphasis">
						<span>{{ $t("board.result.advance") }}</span><span>{{ result.ballots.advance }}</span>
					</div>
					<div class="d-flex justify-space-between text-medium-emphasis">
						<span>{{ $t("board.result.hall") }}</span><span>{{ result.ballots.hall }}</span>
					</div>
					<div class="d-flex justify-space-between text-medium-emphasis">
						<span>{{ $t("board.result.paper") }}</span><span>{{ result.ballots.paper }}</span>
					</div>
				</v-card>
				<v-card class="pa-5 voxa-no-print">
					<h2 class="voxa-heading mb-3" style="font-size: 1.1rem">{{ $t("board.result.paperTitle") }}</h2>
					<v-text-field
						v-if="result.kind === AgendaItemKind.Election"
						v-model.number="paperBallots"
						type="number"
						min="0"
						:label="$t('board.result.paperBallots')"
						density="compact" />
					<v-text-field
						v-for="option in result.options"
						:key="option.optionId"
						v-model.number="paper[option.optionId]"
						type="number"
						min="0"
						density="compact"
						:label="option.role === OptionRole.Blank ? $t('vote.blank') : option.label" />
					<v-btn color="primary" block :loading="save.pending.value" @click="savePaper">{{
						$t("board.result.paperSave")
					}}</v-btn>
					<p class="text-body-small text-medium-emphasis mt-2">{{ $t("board.result.paperNote") }}</p>
				</v-card>
			</v-col>
		</v-row>
		<v-snackbar v-model="snackbar" timeout="4000">{{ snackbarText }}</v-snackbar>
	</template>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue"
import { useI18n } from "vue-i18n"
import ResultBars from "@/components/ResultBars.vue"
import { AgendaItemKind, OptionRole, ResultOutcome, type ResultModel } from "@/logic/api"
import { useApi } from "@/services/useApi"
import { useAsyncAction } from "@/services/useAsyncAction"
import { useLoader } from "@/services/useLoader"

const props = defineProps<{ id: string }>()
const api = useApi()
const { t } = useI18n()
const loader = useLoader(async () => (await api.admin.adminItemsResultList(Number(props.id))).data)
const result = ref<ResultModel | null>(null)
const paper = reactive<Record<number, number>>({})
const paperBallots = ref(0)
const save = useAsyncAction<ResultModel>()
const screen = useAsyncAction<unknown>()
const snackbar = ref(false)
const snackbarText = ref("")

onMounted(() => void loader.load())

watch(
	() => loader.data.value,
	(value) => {
		if (value) apply(value)
	}
)

function apply(value: ResultModel): void {
	result.value = value
	paperBallots.value = value.ballots.paper
	for (const option of value.options) paper[option.optionId] = option.votes.paper
}

const outcomeColor = computed(() => {
	const outcome = result.value?.outcome
	if (outcome === ResultOutcome.Adopted || outcome === ResultOutcome.Elected) return "success"
	if (outcome === ResultOutcome.Rejected) return "error"
	return "warning"
})

const summary = computed(() => {
	const value = result.value
	if (!value) return ""
	if (value.kind === AgendaItemKind.Proposal) {
		const forVotes = value.options.find((o) => o.role === OptionRole.For)?.votes.total ?? 0
		return t("board.result.proposalSummary", { for: forVotes, valid: value.validVotes, needed: value.votesNeeded })
	}
	return t("board.result.electionSummary", { valid: value.validVotes, needed: value.votesNeeded, seats: value.seats })
})

function notify(text: string): void {
	snackbarText.value = text
	snackbar.value = true
}

async function savePaper(): Promise<void> {
	const value = result.value
	if (!value) return
	const saved = await save.run(
		async () =>
			(
				await api.admin.adminItemsPaperUpdate(value.itemId, {
					ballots: paperBallots.value || 0,
					options: value.options.map((o) => ({ optionId: o.optionId, count: Number(paper[o.optionId]) || 0 })),
				})
			).data
	)
	if (saved) {
		apply(saved)
		notify(t("board.result.paperSaved"))
	} else if (save.error.value) {
		notify(save.error.value.problem?.detail ?? t(save.error.value.messageKey))
	}
}

async function showOnScreen(): Promise<void> {
	const value = result.value
	if (!value) return
	await screen.run(() => api.admin.adminMeetingsScreenUpdate(value.meetingId, { itemId: value.itemId }))
	notify(screen.error.value ? t(screen.error.value.messageKey) : t("board.result.onScreen"))
}

function print(): void {
	window.print()
}
</script>
