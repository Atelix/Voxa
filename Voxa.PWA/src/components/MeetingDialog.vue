<template>
	<v-dialog :model-value="modelValue" max-width="560" @update:model-value="emit('update:modelValue', $event)">
		<v-card :title="meeting ? $t('board.meeting.editTitle') : $t('board.meeting.newTitle')">
			<v-card-text>
				<v-text-field
					v-model="form.name"
					:label="$t('board.meeting.name')"
					:placeholder="$t('board.meeting.namePlaceholder')" />
				<v-select v-model="form.kind" :items="kinds" :label="$t('board.meeting.kind')" />
				<v-text-field v-model="form.startsAt" type="datetime-local" :label="$t('board.meeting.startsAt')" />
				<v-text-field v-model="form.location" :label="$t('board.meeting.location')" />
				<v-select
					v-model="form.language"
					:items="languages"
					:label="$t('board.meeting.language')"
					:hint="$t('board.meeting.languageHint')"
					persistent-hint
					class="mb-2" />
				<v-switch
					v-model="form.advanceVoting"
					color="primary"
					:label="$t('board.meeting.advance')"
					hide-details
					class="mb-2" />
				<v-text-field
					v-if="form.advanceVoting"
					v-model="form.advanceOpensAt"
					type="datetime-local"
					:label="$t('board.meeting.advanceOpens')"
					:hint="$t('board.meeting.advanceHint')"
					persistent-hint />
				<v-alert v-if="action.error.value" type="error" variant="tonal" class="mt-4">
					{{ $t(action.error.value.messageKey) }}
					<div v-if="action.error.value.problem?.detail" class="text-body-small">
						{{ action.error.value.problem.detail }}
					</div>
				</v-alert>
			</v-card-text>
			<v-card-actions>
				<v-spacer />
				<v-btn variant="text" @click="emit('update:modelValue', false)">{{ $t("common.cancel") }}</v-btn>
				<v-btn
					color="primary"
					variant="flat"
					:loading="action.pending.value"
					:disabled="!form.name || !form.startsAt"
					@click="save"
					>{{ $t("common.save") }}</v-btn
				>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from "vue"
import { useI18n } from "vue-i18n"
import { MeetingKind, type MeetingModel } from "@/logic/api"
import { isAppLocale, languageName, supportedLocales } from "@/plugins/i18n"
import { fromLocalInput, toLocalInput } from "@/services/format"
import { useApi } from "@/services/useApi"
import { useAsyncAction } from "@/services/useAsyncAction"
import { useClubStore } from "@/store/ClubStore"

const props = defineProps<{ modelValue: boolean; meeting: MeetingModel | null }>()
const emit = defineEmits<{ "update:modelValue": [boolean]; saved: [MeetingModel] }>()
const api = useApi()
const club = useClubStore()
const { t } = useI18n()
const action = useAsyncAction<MeetingModel>()
const form = reactive({
	name: "",
	kind: MeetingKind.Annual,
	startsAt: "",
	location: "",
	// "" is the club's language; Vuetify's select does not take null as an item value.
	language: "",
	advanceVoting: true,
	advanceOpensAt: "",
})
const kinds = computed(() => [
	{ title: t("board.meeting.kindAnnual"), value: MeetingKind.Annual },
	{ title: t("board.meeting.kindExtra"), value: MeetingKind.Extra },
])
const languages = computed(() => [
	{
		title: t("board.meeting.languageClub", {
			language: isAppLocale(club.brand.language) ? languageName(club.brand.language) : club.brand.language,
		}),
		value: "",
	},
	...supportedLocales.map((locale) => ({ title: languageName(locale), value: locale })),
])

watch(
	() => props.modelValue,
	(open) => {
		if (!open) return
		action.error.value = null
		const m = props.meeting
		Object.assign(form, {
			name: m?.name ?? "",
			kind: m?.kind ?? MeetingKind.Annual,
			startsAt: toLocalInput(m?.startsAt),
			location: m?.location ?? "",
			language: m?.language ?? "",
			advanceVoting: m?.advanceVoting ?? true,
			advanceOpensAt: toLocalInput(m?.advanceOpensAt),
		})
	},
	{ immediate: true }
)

async function save(): Promise<void> {
	const request = {
		name: form.name,
		kind: form.kind,
		startsAt: fromLocalInput(form.startsAt) ?? "",
		location: form.location || null,
		language: form.language || null,
		advanceVoting: form.advanceVoting,
		// Empty: the earliest moment the club's rules allow.
		advanceOpensAt: form.advanceVoting ? fromLocalInput(form.advanceOpensAt) : null,
	}
	const saved = await action.run(async () =>
		props.meeting
			? (await api.admin.adminMeetingsUpdate(props.meeting.id, request)).data
			: (await api.admin.adminMeetingsCreate(request)).data
	)
	if (saved) emit("saved", saved)
}
</script>
