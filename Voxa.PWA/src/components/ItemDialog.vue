<template>
	<v-dialog :model-value="modelValue" max-width="640" @update:model-value="emit('update:modelValue', $event)">
		<v-card :title="item ? $t('board.item.editTitle') : $t('board.item.newTitle')">
			<v-card-text>
				<v-btn-toggle v-if="!item" v-model="form.kind" mandatory color="primary" variant="outlined" class="mb-4">
					<v-btn :value="AgendaItemKind.Proposal">{{ $t("board.item.proposal") }}</v-btn>
					<v-btn :value="AgendaItemKind.Election">{{ $t("board.item.election") }}</v-btn>
				</v-btn-toggle>
				<v-text-field v-model="form.title" :label="$t('board.item.title')" />
				<v-textarea v-model="form.description" :label="$t('board.item.description')" rows="2" auto-grow />
				<v-select
					v-if="form.kind === AgendaItemKind.Proposal"
					v-model="form.majority"
					:items="majorities"
					:label="$t('board.item.majority')" />
				<template v-else>
					<v-text-field v-model.number="form.seats" type="number" min="1" :label="$t('board.item.seats')" />
					<p class="text-body-medium mb-2">{{ $t("board.item.candidates") }}</p>
					<div v-for="(candidate, index) in form.candidates" :key="index" class="d-flex ga-2">
						<v-text-field v-model="candidate.label" :label="$t('board.item.candidateName')" />
						<v-text-field v-model="candidate.note" :label="$t('board.item.candidateNote')" />
						<v-btn
							icon="mdi-close"
							variant="text"
							:aria-label="$t('common.delete')"
							@click="form.candidates.splice(index, 1)" />
					</div>
					<v-btn variant="text" prepend-icon="mdi-plus" @click="form.candidates.push({ label: '', note: '' })">{{
						$t("board.item.addCandidate")
					}}</v-btn>
					<p class="text-body-small text-medium-emphasis mt-2">{{ $t("board.item.blankNote") }}</p>
				</template>
				<v-switch v-model="form.advanceVoting" color="primary" :label="$t('board.item.advance')" hide-details />
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
				<v-btn color="primary" variant="flat" :loading="action.pending.value" :disabled="!form.title" @click="save">{{
					$t("common.save")
				}}</v-btn>
			</v-card-actions>
		</v-card>
	</v-dialog>
</template>

<script setup lang="ts">
import { computed, reactive, watch } from "vue"
import { useI18n } from "vue-i18n"
import { AgendaItemKind, MajorityRule, OptionRole, type AgendaItemModel } from "@/logic/api"
import { useApi } from "@/services/useApi"
import { useAsyncAction } from "@/services/useAsyncAction"

const props = defineProps<{ modelValue: boolean; meetingId: string; item: AgendaItemModel | null }>()
const emit = defineEmits<{ "update:modelValue": [boolean]; saved: [] }>()
const api = useApi()
const { t } = useI18n()
const action = useAsyncAction<AgendaItemModel>()
const form = reactive({
	kind: AgendaItemKind.Proposal,
	title: "",
	description: "",
	majority: MajorityRule.Absolute,
	seats: 1,
	advanceVoting: true,
	candidates: [{ label: "", note: "" }] as { label: string; note: string }[],
})
const majorities = computed(() => [
	{ title: t("board.item.majorityAbsolute"), value: MajorityRule.Absolute },
	{ title: t("board.item.majorityTwoThirds"), value: MajorityRule.TwoThirds },
])

watch(
	() => props.modelValue,
	(open) => {
		if (!open) return
		action.error.value = null
		const i = props.item
		const candidates =
			i?.options.filter((o) => o.role === OptionRole.Candidate).map((o) => ({ label: o.label, note: o.note ?? "" })) ??
			[]
		Object.assign(form, {
			kind: i?.kind ?? AgendaItemKind.Proposal,
			title: i?.title ?? "",
			description: i?.description ?? "",
			majority: i?.majority ?? MajorityRule.Absolute,
			seats: i?.seats || 1,
			advanceVoting: i?.advanceVoting ?? true,
			candidates: candidates.length ? candidates : [{ label: "", note: "" }],
		})
	},
	{ immediate: true }
)

async function save(): Promise<void> {
	const request = {
		title: form.title,
		description: form.description || null,
		kind: form.kind,
		majority: form.majority,
		seats: form.kind === AgendaItemKind.Election ? form.seats : 0,
		advanceVoting: form.advanceVoting,
		candidates:
			form.kind === AgendaItemKind.Election
				? form.candidates.filter((c) => c.label.trim()).map((c) => ({ label: c.label, note: c.note || null }))
				: [],
	}
	const saved = await action.run(async () =>
		props.item
			? (await api.admin.adminItemsUpdate(props.item.id, request)).data
			: (await api.admin.adminMeetingsItemsCreate(props.meetingId, request)).data
	)
	if (saved) emit("saved")
}
</script>
