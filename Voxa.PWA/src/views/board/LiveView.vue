<template>
	<v-skeleton-loader v-if="!live" type="heading, card, table" />
	<template v-else>
		<v-btn variant="text" prepend-icon="mdi-arrow-left" class="px-0 mb-2" :to="{ name: 'meeting', params: { id } }">{{
			live.meeting.name
		}}</v-btn>
		<div class="d-flex flex-wrap align-center ga-2 mb-1">
			<h1 class="voxa-heading text-primary" style="font-size: 1.8rem">
				{{ archived ? $t("board.live.resultsTitle") : $t("board.live.title") }}
			</h1>
			<v-chip v-if="!archived" color="secondary" variant="flat" size="small">{{
				live.advanceOpenNow ? $t("board.live.advanceRunning") : $t("board.live.live")
			}}</v-chip>
			<v-spacer />
			<v-btn v-if="!archived" variant="outlined" prepend-icon="mdi-monitor" :href="hallUrl" target="_blank">{{
				$t("board.live.openScreen")
			}}</v-btn>
		</div>
		<p class="text-body-medium text-medium-emphasis mb-6">
			{{ formatDateTime(live.meeting.startsAt)
			}}<span v-if="live.meeting.location"> · {{ live.meeting.location }}</span>
		</p>

		<v-row class="mb-2">
			<v-col v-for="kpi in kpis" :key="kpi.label" cols="6" md="3">
				<v-card class="pa-4 h-100">
					<div class="text-body-small text-medium-emphasis">{{ kpi.label }}</div>
					<div class="voxa-heading text-primary" style="font-size: 2rem">{{ kpi.value }}</div>
					<div class="text-body-small text-medium-emphasis">{{ kpi.sub }}</div>
				</v-card>
			</v-col>
		</v-row>

		<v-row>
			<v-col cols="12" md="8">
				<v-card class="pa-5">
					<div class="d-flex align-center mb-3">
						<h2 class="voxa-heading" style="font-size: 1.1rem">{{ $t("board.item.section") }}</h2>
						<v-spacer />
						<span class="text-body-small text-medium-emphasis">{{ $t("board.live.hidden") }}</span>
					</div>
					<div
						v-for="item in live.items"
						:key="item.id"
						class="py-3"
						style="border-top: 1px solid rgba(var(--v-border-color), 0.12)">
						<div class="d-flex flex-wrap align-center ga-2">
							<span class="voxa-heading text-primary" style="width: 1.5rem">{{ item.position }}</span>
							<span class="font-weight-bold flex-grow-1">{{ item.title }}</span>
							<v-chip
								size="small"
								:color="item.state === AgendaItemState.Open ? 'secondary' : undefined"
								:variant="item.state === AgendaItemState.Open ? 'flat' : 'tonal'">
								{{ stateLabel(item) }}
							</v-chip>
							<template v-if="!archived">
								<v-btn
									v-if="item.state === AgendaItemState.Open"
									color="primary"
									size="small"
									:loading="busy === item.id"
									@click="toggle(item.id, false)"
									>{{ $t("board.live.close") }}</v-btn
								>
								<v-btn
									v-else-if="item.state === AgendaItemState.Pending"
									variant="outlined"
									size="small"
									:loading="busy === item.id"
									@click="toggle(item.id, true)"
									>{{ $t("board.live.open") }}</v-btn
								>
							</template>
							<v-btn
								v-if="item.state === AgendaItemState.Closed"
								variant="text"
								size="small"
								:to="{ name: 'result', params: { id: item.id } }"
								>{{ $t("board.live.result") }}</v-btn
							>
						</div>
						<div class="d-flex align-center ga-3 mt-2 pl-8">
							<v-progress-linear
								:model-value="turnout(item.ballots.total)"
								color="primary"
								bg-color="secondary"
								height="6"
								rounded
								aria-hidden="true" />
							<span class="text-body-small text-no-wrap">{{
								$t("board.live.turnout", { n: item.ballots.total, total: live.meeting.codesIssued })
							}}</span>
						</div>
					</div>
				</v-card>
			</v-col>
			<v-col v-if="!archived" cols="12" md="4">
				<v-card class="pa-5 text-center">
					<h2 class="voxa-heading mb-3" style="font-size: 1.1rem">{{ $t("board.live.qrTitle") }}</h2>
					<div class="d-flex justify-center mb-2"><qr-code :value="origin" :size="180" /></div>
					<p class="voxa-heading text-primary">{{ host }}</p>
				</v-card>
				<p class="text-body-small text-medium-emphasis mt-4">{{ $t("board.live.secrecy") }}</p>
			</v-col>
		</v-row>
		<v-snackbar v-model="snackbar" timeout="4000">{{ snackbarText }}</v-snackbar>
	</template>
</template>

<script setup lang="ts">
import { computed, ref } from "vue"
import { useI18n } from "vue-i18n"
import QrCode from "@/components/QrCode.vue"
import { AgendaItemState, MeetingStatus, type LiveItemModel, type LiveModel } from "@/logic/api"
import { normalizeApiError } from "@/logic/ApiError"
import { formatDateTime } from "@/services/format"
import { useApi } from "@/services/useApi"
import { usePolling } from "@/services/usePolling"

const props = defineProps<{ id: string }>()
const api = useApi()
const { t } = useI18n()
const live = ref<LiveModel | null>(null)
const busy = ref<number | null>(null)
const snackbar = ref(false)
const snackbarText = ref("")
const origin = window.location.origin
const host = window.location.host
const hallUrl = computed(() => `/hall/${props.id}`)
const archived = computed(() => live.value?.meeting.status === MeetingStatus.Archived)

async function load(): Promise<void> {
	try {
		live.value = (await api.admin.adminMeetingsLiveList(Number(props.id))).data
	} catch (error) {
		snackbarText.value = t(normalizeApiError(error).messageKey)
		snackbar.value = true
	}
}

usePolling(load, 3000)

const kpis = computed(() => {
	const value = live.value
	if (!value) return []
	const open = value.items.find((i) => i.state === AgendaItemState.Open)
	const closed = value.items.filter((i) => i.state === AgendaItemState.Closed).length
	return [
		{
			label: t("board.live.kpiEligible"),
			value: String(value.meeting.codesIssued),
			sub: t("board.live.kpiEligibleSub"),
		},
		{ label: t("board.live.kpiUsed"), value: String(value.meeting.codesUsed), sub: t("board.live.kpiUsedSub") },
		{
			label: t("board.live.kpiOpen"),
			value: open ? String(open.ballots.total) : "–",
			sub: open ? t("board.live.kpiOpenSub", { n: open.position }) : t("board.live.kpiNoneOpen"),
		},
		{ label: t("board.live.kpiClosed"), value: `${closed} / ${value.items.length}`, sub: t("board.live.kpiClosedSub") },
	]
})

function turnout(n: number): number {
	const total = live.value?.meeting.codesIssued ?? 0
	return total ? (n / total) * 100 : 0
}

function stateLabel(item: LiveItemModel): string {
	if (item.state === AgendaItemState.Pending && live.value?.advanceOpenNow && item.advanceVoting)
		return t("board.live.stateAdvance")
	return t(`board.live.state.${item.state}`)
}

async function toggle(itemId: number, open: boolean): Promise<void> {
	busy.value = itemId
	try {
		if (open) await api.admin.adminItemsOpenCreate(itemId)
		else await api.admin.adminItemsCloseCreate(itemId)
		await load()
	} catch (error) {
		const normalized = normalizeApiError(error)
		snackbarText.value = normalized.problem?.detail ?? t(normalized.messageKey)
		snackbar.value = true
	} finally {
		busy.value = null
	}
}
</script>
