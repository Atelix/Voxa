<template>
	<v-skeleton-loader v-if="loader.loading.value && !meeting" type="heading, card, card" />
	<v-alert v-else-if="loader.error.value && !meeting" type="error" variant="tonal">{{
		$t(loader.error.value.messageKey)
	}}</v-alert>
	<template v-else-if="meeting">
		<v-btn variant="text" prepend-icon="mdi-arrow-left" class="px-0 mb-2" :to="{ name: 'meetings' }">{{
			$t("board.nav.meetings")
		}}</v-btn>
		<div class="d-flex flex-wrap align-center ga-2 mb-6">
			<h1 class="voxa-heading text-primary" style="font-size: 1.8rem">{{ meeting.name }}</h1>
			<v-chip size="small">{{ $t(`board.status.${meeting.status}`) }}</v-chip>
			<v-spacer />
			<v-btn
				v-if="meeting.status === MeetingStatus.Ready"
				color="primary"
				prepend-icon="mdi-broadcast"
				:to="{ name: 'live', params: { id: meeting.id } }"
				>{{ $t("board.meetings.live") }}</v-btn
			>
		</div>

		<v-card class="pa-5 mb-6">
			<div class="d-flex align-center mb-3">
				<h2 class="voxa-heading" style="font-size: 1.1rem">1. {{ $t("board.meeting.section") }}</h2>
				<v-spacer />
				<v-btn v-if="!archived" variant="text" prepend-icon="mdi-pencil" @click="meetingDialog = true">{{
					$t("common.edit")
				}}</v-btn>
			</div>
			<p>
				{{ formatDateTime(meeting.startsAt) }}<span v-if="meeting.location"> · {{ meeting.location }}</span>
			</p>
			<p class="text-body-medium text-medium-emphasis">{{ $t(`board.meeting.kind${meeting.kind}`) }}</p>
			<h2 class="voxa-heading mt-5 mb-2" style="font-size: 1.1rem">2. {{ $t("board.meeting.advanceSection") }}</h2>
			<p v-if="meeting.advanceVoting && meeting.advanceOpensAt">
				{{
					$t("board.meeting.advanceWindow", {
						from: formatDateTime(meeting.advanceOpensAt),
						to: formatDateTime(meeting.startsAt),
					})
				}}
			</p>
			<p v-else>{{ $t("board.meeting.noAdvance") }}</p>
			<p class="text-body-small text-medium-emphasis">
				{{ $t("board.meeting.earliest", { date: formatDate(meeting.earliestAdvanceOpensAt) }) }}
			</p>
		</v-card>

		<v-card class="pa-5 mb-6">
			<div class="d-flex flex-wrap align-center ga-2 mb-3">
				<h2 class="voxa-heading" style="font-size: 1.1rem">3. {{ $t("board.item.section") }}</h2>
				<v-spacer />
				<template v-if="!archived">
					<v-btn variant="outlined" prepend-icon="mdi-plus" @click="openItem(null)">{{ $t("board.item.add") }}</v-btn>
				</template>
			</div>
			<p v-if="!meeting.items.length" class="text-medium-emphasis">{{ $t("board.item.none") }}</p>
			<v-list v-else lines="two" class="py-0">
				<v-list-item v-for="(item, index) in meeting.items" :key="item.id" class="px-0">
					<template #prepend>
						<span class="voxa-heading text-primary mr-4" style="width: 1.5rem">{{ item.position }}</span>
					</template>
					<v-list-item-title class="font-weight-bold">{{ item.title }}</v-list-item-title>
					<v-list-item-subtitle>
						{{ itemDetail(item) }}
						<span v-if="!item.advanceVoting"> · {{ $t("board.item.hallOnly") }}</span>
					</v-list-item-subtitle>
					<template #append>
						<template v-if="!archived">
							<v-btn
								icon="mdi-arrow-up"
								variant="text"
								size="small"
								:disabled="index === 0"
								:aria-label="$t('board.item.up')"
								@click="move(index, -1)" />
							<v-btn
								icon="mdi-arrow-down"
								variant="text"
								size="small"
								:disabled="index === meeting.items.length - 1"
								:aria-label="$t('board.item.down')"
								@click="move(index, 1)" />
							<v-btn
								icon="mdi-pencil"
								variant="text"
								size="small"
								:disabled="item.locked"
								:aria-label="$t('common.edit')"
								@click="openItem(item)" />
							<v-btn
								icon="mdi-delete-outline"
								variant="text"
								size="small"
								:disabled="item.locked"
								:aria-label="$t('common.delete')"
								@click="removeItem(item)" />
						</template>
					</template>
				</v-list-item>
			</v-list>
			<p class="text-body-small text-medium-emphasis mt-2">{{ $t("board.item.lockNote") }}</p>
		</v-card>

		<voters-panel v-if="!archived" :meeting="meeting" @changed="loader.load()" />

		<v-card v-if="!archived" variant="outlined" class="pa-5 mt-6">
			<h2 class="voxa-heading mb-2" style="font-size: 1.1rem">{{ $t("board.meeting.endSection") }}</h2>
			<p class="text-body-medium mb-4">
				{{ meeting.status === MeetingStatus.Ready ? $t("board.meeting.archiveText") : $t("board.meeting.deleteText") }}
			</p>
			<v-btn
				v-if="meeting.status === MeetingStatus.Ready"
				color="error"
				variant="outlined"
				@click="confirmArchive = true"
				>{{ $t("board.meeting.archive") }}</v-btn
			>
			<v-btn v-else color="error" variant="outlined" @click="confirmDelete = true">{{
				$t("board.meeting.delete")
			}}</v-btn>
		</v-card>

		<MeetingDialog v-model="meetingDialog" :meeting="meeting" @saved="onMeetingSaved" />
		<ItemDialog v-model="itemDialog" :meeting-id="meeting.id" :item="editing" @saved="onItemSaved" />

		<confirm-dialog
			v-model="confirmArchive"
			:title="$t('board.meeting.archive')"
			:text="$t('board.meeting.archiveConfirm')"
			:confirm="$t('board.meeting.archive')"
			@confirm="archive" />
		<confirm-dialog
			v-model="confirmDelete"
			:title="$t('board.meeting.delete')"
			:text="$t('board.meeting.deleteConfirm')"
			:confirm="$t('board.meeting.delete')"
			@confirm="remove" />
		<v-snackbar v-model="snackbar" timeout="4000">{{ snackbarText }}</v-snackbar>
	</template>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useI18n } from "vue-i18n"
import { useRouter } from "vue-router"
import ConfirmDialog from "@/components/ConfirmDialog.vue"
import ItemDialog from "@/components/ItemDialog.vue"
import MeetingDialog from "@/components/MeetingDialog.vue"
import VotersPanel from "@/components/VotersPanel.vue"
import { AgendaItemKind, MajorityRule, MeetingStatus, OptionRole, type AgendaItemModel } from "@/logic/api"
import { normalizeApiError } from "@/logic/ApiError"
import { formatDate, formatDateTime } from "@/services/format"
import { useApi } from "@/services/useApi"
import { useLoader } from "@/services/useLoader"

const props = defineProps<{ id: string }>()
const api = useApi()
const router = useRouter()
const { t } = useI18n()
const loader = useLoader(async () => (await api.admin.adminMeetingsDetail(Number(props.id))).data)
const meeting = computed(() => loader.data.value)
const archived = computed(() => meeting.value?.status === MeetingStatus.Archived)
const meetingDialog = ref(false)
const itemDialog = ref(false)
const editing = ref<AgendaItemModel | null>(null)
const confirmArchive = ref(false)
const confirmDelete = ref(false)
const snackbar = ref(false)
const snackbarText = ref("")

onMounted(() => void loader.load())

function notify(text: string): void {
	snackbarText.value = text
	snackbar.value = true
}

function itemDetail(item: AgendaItemModel): string {
	if (item.kind === AgendaItemKind.Proposal) {
		return `${t("board.item.proposal")} · ${item.majority === MajorityRule.TwoThirds ? t("board.item.majorityTwoThirds") : t("board.item.majorityAbsolute")}`
	}
	const candidates = item.options.filter((o) => o.role === OptionRole.Candidate).length
	return `${t("board.item.election")} · ${t("board.item.seatsCandidates", { seats: item.seats, candidates })}`
}

function openItem(item: AgendaItemModel | null): void {
	editing.value = item
	itemDialog.value = true
}

async function onMeetingSaved(): Promise<void> {
	meetingDialog.value = false
	notify(t("common.saved"))
	await loader.load()
}

async function onItemSaved(): Promise<void> {
	itemDialog.value = false
	notify(t("common.saved"))
	await loader.load()
}

async function run(action: () => Promise<unknown>, done?: string): Promise<boolean> {
	try {
		await action()
		if (done) notify(done)
		return true
	} catch (error) {
		const normalized = normalizeApiError(error)
		notify(normalized.problem?.detail ?? t(normalized.messageKey))
		return false
	}
}

async function move(index: number, delta: number): Promise<void> {
	if (!meeting.value) return
	const ids = meeting.value.items.map((i) => i.id)
	const [moved] = ids.splice(index, 1)
	if (moved === undefined) return
	ids.splice(index + delta, 0, moved)
	const id = meeting.value.id
	if (await run(() => api.admin.adminMeetingsItemsOrderUpdate(id, { itemIds: ids }))) await loader.load()
}

async function removeItem(item: AgendaItemModel): Promise<void> {
	if (await run(() => api.admin.adminItemsDelete(item.id), t("common.deleted"))) await loader.load()
}

async function archive(): Promise<void> {
	if (!meeting.value) return
	const id = meeting.value.id
	if (await run(() => api.admin.adminMeetingsArchiveCreate(id), t("board.meeting.archived"))) await loader.load()
}

async function remove(): Promise<void> {
	if (!meeting.value) return
	const id = meeting.value.id
	if (await run(() => api.admin.adminMeetingsDelete(id))) await router.replace({ name: "meetings" })
}
</script>
