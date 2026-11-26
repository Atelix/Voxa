<template>
	<div class="d-flex flex-wrap align-center ga-2 mb-2">
		<h1 class="voxa-heading text-primary" style="font-size: 1.8rem">{{ $t("board.meetings.title") }}</h1>
		<v-spacer />
		<v-btn
			v-if="board.trialMode"
			variant="outlined"
			prepend-icon="mdi-flask-outline"
			:loading="demo.pending.value"
			@click="createDemo"
			>{{ $t("board.meetings.demo") }}</v-btn
		>
		<v-btn color="primary" prepend-icon="mdi-plus" @click="dialog = true">{{ $t("board.meetings.new") }}</v-btn>
	</div>
	<p class="text-body-medium text-medium-emphasis mb-6">{{ $t("board.meetings.lead") }}</p>

	<v-skeleton-loader v-if="loader.loading.value && !loader.data.value" type="card@2" />
	<v-alert v-else-if="loader.error.value" type="error" variant="tonal">{{ $t(loader.error.value.messageKey) }}</v-alert>
	<template v-else>
		<h2 class="voxa-heading mb-3" style="font-size: 1.1rem">{{ $t("board.meetings.upcoming") }}</h2>
		<v-card v-if="!upcoming.length" variant="outlined" class="pa-6 text-center text-medium-emphasis mb-6">{{
			$t("board.meetings.none")
		}}</v-card>
		<v-card v-for="meeting in upcoming" :key="meeting.id" class="pa-5 mb-4">
			<div class="d-flex flex-wrap align-center ga-2 mb-1">
				<h3 class="voxa-heading text-primary" style="font-size: 1.3rem">{{ meeting.name }}</h3>
				<v-chip size="small" :color="meeting.status === MeetingStatus.Ready ? 'success' : undefined">{{
					$t(`board.status.${meeting.status}`)
				}}</v-chip>
			</div>
			<p class="text-body-medium text-medium-emphasis mb-3">
				{{ formatDateTime(meeting.startsAt) }}<span v-if="meeting.location"> · {{ meeting.location }}</span>
			</p>
			<div class="d-flex flex-wrap ga-6 mb-4 text-body-medium">
				<span>{{ $t("board.meetings.items", { n: meeting.itemCount }) }}</span>
				<span v-if="meeting.advanceVoting && meeting.advanceOpensAt">{{
					$t("board.meetings.advanceFrom", { date: formatDate(meeting.advanceOpensAt) })
				}}</span>
				<span>{{
					meeting.codesIssued
						? $t("board.meetings.codesSent", { n: meeting.codesIssued })
						: $t("board.meetings.codesNotSent")
				}}</span>
			</div>
			<div class="d-flex flex-wrap ga-2">
				<v-btn variant="outlined" :to="{ name: 'meeting', params: { id: meeting.id } }">{{
					$t("board.meetings.edit")
				}}</v-btn>
				<v-btn
					color="primary"
					:to="{ name: 'live', params: { id: meeting.id } }"
					:disabled="meeting.status !== MeetingStatus.Ready"
					>{{ $t("board.meetings.live") }}</v-btn
				>
			</div>
		</v-card>

		<h2 class="voxa-heading mt-8 mb-3" style="font-size: 1.1rem">{{ $t("board.meetings.archive") }}</h2>
		<v-card variant="outlined">
			<v-table>
				<thead>
					<tr>
						<th>{{ $t("board.meetings.meeting") }}</th>
						<th>{{ $t("board.meetings.date") }}</th>
						<th>{{ $t("board.meetings.itemsHeader") }}</th>
						<th>{{ $t("board.meetings.turnout") }}</th>
						<th />
					</tr>
				</thead>
				<tbody>
					<tr v-if="!archived.length">
						<td colspan="5" class="text-medium-emphasis">{{ $t("board.meetings.archiveEmpty") }}</td>
					</tr>
					<tr v-for="meeting in archived" :key="meeting.id">
						<td>{{ meeting.name }}</td>
						<td>{{ formatDate(meeting.startsAt) }}</td>
						<td>{{ meeting.itemCount }}</td>
						<td>{{ $t("board.meetings.turnoutValue", { used: meeting.codesUsed, issued: meeting.codesIssued }) }}</td>
						<td class="text-right">
							<v-btn size="small" variant="text" :to="{ name: 'live', params: { id: meeting.id } }">{{
								$t("board.meetings.results")
							}}</v-btn>
							<v-btn size="small" variant="text" :loading="copying === meeting.id" @click="copy(meeting.id)">{{
								$t("board.meetings.copy")
							}}</v-btn>
						</td>
					</tr>
				</tbody>
			</v-table>
		</v-card>
		<p class="text-body-small text-medium-emphasis mt-2">{{ $t("board.meetings.archiveNote") }}</p>
	</template>

	<meeting-dialog v-model="dialog" :meeting="null" @saved="onCreated" />
	<v-snackbar v-model="snackbar" timeout="4000">{{ snackbarText }}</v-snackbar>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useI18n } from "vue-i18n"
import { useRouter } from "vue-router"
import MeetingDialog from "@/components/MeetingDialog.vue"
import { MeetingStatus, type MeetingModel } from "@/logic/api"
import { formatDate, formatDateTime } from "@/services/format"
import { useApi } from "@/services/useApi"
import { useAsyncAction } from "@/services/useAsyncAction"
import { useLoader } from "@/services/useLoader"
import { useBoardStore } from "@/store/BoardStore"

const api = useApi()
const board = useBoardStore()
const router = useRouter()
const { t } = useI18n()
const loader = useLoader(async () => (await api.admin.adminMeetingsList()).data)
const upcoming = computed(() => (loader.data.value ?? []).filter((m) => m.status !== MeetingStatus.Archived))
const archived = computed(() => (loader.data.value ?? []).filter((m) => m.status === MeetingStatus.Archived))
const dialog = ref(false)
const copying = ref<number | null>(null)
const demo = useAsyncAction<MeetingModel>()
const snackbar = ref(false)
const snackbarText = ref("")

onMounted(() => void loader.load())

async function onCreated(meeting: MeetingModel): Promise<void> {
	dialog.value = false
	await router.push({ name: "meeting", params: { id: meeting.id } })
}

async function copy(id: number): Promise<void> {
	copying.value = id
	try {
		const copy = (await api.admin.adminMeetingsCopyCreate(id)).data
		await router.push({ name: "meeting", params: { id: copy.id } })
	} catch {
		snackbarText.value = t("apiError.unknown")
		snackbar.value = true
	} finally {
		copying.value = null
	}
}

async function createDemo(): Promise<void> {
	const meeting = await demo.run(async () => (await api.admin.adminDataDemoCreate()).data)
	if (meeting) {
		snackbarText.value = t("board.meetings.demoCreated")
		snackbar.value = true
		await loader.load()
	}
}
</script>
