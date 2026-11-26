<template>
	<div class="px-4 pt-4">
		<v-btn variant="text" prepend-icon="mdi-arrow-left" class="mb-2 px-0" :to="{ name: 'items' }">{{
			$t("vote.allItems")
		}}</v-btn>

		<v-skeleton-loader v-if="!session" type="heading, paragraph, list-item@3" />

		<v-card v-else-if="!item" variant="outlined" class="pa-6 text-center">{{ $t("vote.notFound") }}</v-card>

		<!-- Done: the vote was stored. -->
		<v-card v-else-if="step === 'done'" class="pa-6 text-center">
			<v-icon icon="mdi-check-circle" color="success" size="64" class="mb-3" />
			<h1 class="voxa-heading text-primary mb-2" style="font-size: 1.6rem">{{ $t("vote.receivedTitle") }}</h1>
			<p class="font-weight-bold mb-3">{{ item.title }}</p>
			<p class="text-body-medium text-medium-emphasis mb-2">{{ $t("vote.receivedSecret") }}</p>
			<p class="text-body-medium text-medium-emphasis mb-6">{{ $t("vote.receivedResult") }}</p>
			<v-btn color="primary" size="large" block :to="{ name: 'items' }">{{ $t("vote.backToItems") }}</v-btn>
		</v-card>

		<!-- This code already voted here. -->
		<v-card v-else-if="item.status === BallotItemStatus.Voted || conflict" class="pa-6">
			<h1 class="voxa-heading text-primary mb-2" style="font-size: 1.4rem">{{ $t("vote.alreadyTitle") }}</h1>
			<p class="mb-4">{{ $t("vote.alreadyText", { title: item.title }) }}</p>
			<v-alert type="warning" variant="tonal" class="mb-4" :title="$t('vote.notYouTitle')">{{
				$t("vote.notYouText")
			}}</v-alert>
			<v-btn color="primary" block :to="{ name: 'items' }">{{ $t("vote.backToItems") }}</v-btn>
		</v-card>

		<v-card v-else-if="item.status !== BallotItemStatus.Open" variant="outlined" class="pa-6">
			<h1 class="voxa-heading text-primary mb-2" style="font-size: 1.4rem">{{ item.title }}</h1>
			<p>{{ item.status === BallotItemStatus.Closed ? $t("vote.closed") : $t("vote.notOpen") }}</p>
		</v-card>

		<!-- Confirm the choice. -->
		<template v-else-if="step === 'confirm'">
			<h1 class="voxa-heading text-primary mb-4" style="font-size: 1.6rem">{{ $t("vote.confirmTitle") }}</h1>
			<v-card class="pa-5 mb-4">
				<p class="voxa-eyebrow text-medium-emphasis">{{ $t("vote.itemNumber", { n: item.position }) }}</p>
				<p class="font-weight-bold mb-4">{{ item.title }}</p>
				<p class="voxa-eyebrow text-medium-emphasis">{{ $t("vote.yourChoice") }}</p>
				<p v-for="label in chosenLabels" :key="label" class="voxa-heading text-primary" style="font-size: 1.5rem">
					{{ label }}
				</p>
			</v-card>
			<v-alert type="info" variant="tonal" icon="mdi-shield-lock-outline" class="mb-4">{{
				$t("vote.finalNotice")
			}}</v-alert>
			<v-alert v-if="action.error.value" type="error" variant="tonal" class="mb-4">{{ errorText }}</v-alert>
			<v-btn color="secondary" size="x-large" block class="mb-2" :loading="action.pending.value" @click="send">{{
				$t("vote.send")
			}}</v-btn>
			<v-btn variant="text" block @click="step = 'choose'">{{ $t("vote.back") }}</v-btn>
		</template>

		<!-- Choose. -->
		<template v-else>
			<p class="voxa-eyebrow text-medium-emphasis mb-1">
				{{ $t("vote.itemOf", { n: item.position, total: session.items.length }) }}
			</p>
			<h1 class="voxa-heading text-primary mb-2" style="font-size: 1.6rem">{{ item.title }}</h1>
			<p v-if="item.description" class="text-body-large mb-4">{{ item.description }}</p>

			<template v-if="item.kind === AgendaItemKind.Proposal">
				<h2 class="voxa-heading mb-3" style="font-size: 1.1rem">{{ $t("vote.whatDoYouVote") }}</h2>
				<div role="radiogroup" class="d-flex flex-column ga-3">
					<button
						v-for="option in item.options"
						:key="option.id"
						type="button"
						role="radio"
						class="voxa-choice"
						:aria-checked="picked.includes(option.id)"
						@click="picked = [option.id]">
						<v-icon :icon="picked.includes(option.id) ? 'mdi-radiobox-marked' : 'mdi-radiobox-blank'" color="primary" />
						<span class="d-flex flex-column">
							<span class="font-weight-bold">{{ option.label }}</span>
							<span class="text-body-small text-medium-emphasis">{{ $t(`vote.hint.${option.role}`) }}</span>
						</span>
					</button>
				</div>
			</template>

			<template v-else>
				<div class="d-flex align-center mb-3">
					<h2 class="voxa-heading" style="font-size: 1.1rem">{{ $t("vote.candidates") }}</h2>
					<v-spacer />
					<span class="text-body-small text-medium-emphasis">{{
						$t("vote.chosenOf", { n: candidatesPicked, seats: item.seats })
					}}</span>
				</div>
				<p class="text-body-medium mb-3">{{ $t("vote.seats", { seats: item.seats }) }}</p>
				<div class="d-flex flex-column ga-3">
					<button
						v-for="option in candidates"
						:key="option.id"
						type="button"
						class="voxa-choice"
						:aria-pressed="picked.includes(option.id)"
						:disabled="blankPicked || (candidatesPicked >= item.seats && !picked.includes(option.id))"
						@click="toggle(option.id)">
						<v-icon
							:icon="picked.includes(option.id) ? 'mdi-checkbox-marked' : 'mdi-checkbox-blank-outline'"
							color="primary" />
						<span class="d-flex flex-column">
							<span class="font-weight-bold">{{ option.label }}</span>
							<span v-if="option.note" class="text-body-small text-medium-emphasis">{{ option.note }}</span>
						</span>
					</button>
					<button v-if="blank" type="button" class="voxa-choice" :aria-pressed="blankPicked" @click="toggleBlank">
						<v-icon :icon="blankPicked ? 'mdi-checkbox-marked' : 'mdi-checkbox-blank-outline'" color="primary" />
						<span class="d-flex flex-column">
							<span class="font-weight-bold">{{ $t("vote.blank") }}</span>
							<span class="text-body-small text-medium-emphasis">{{ $t("vote.blankHint") }}</span>
						</span>
					</button>
				</div>
			</template>

			<v-btn
				color="secondary"
				size="x-large"
				block
				class="mt-6"
				:disabled="picked.length === 0"
				@click="step = 'confirm'">
				{{ $t("vote.toConfirm") }}
			</v-btn>
			<p class="text-body-small text-medium-emphasis text-center mt-2">{{ $t("vote.once") }}</p>
		</template>
	</div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useI18n } from "vue-i18n"
import { useRouter } from "vue-router"
import { AgendaItemKind, BallotItemStatus, OptionRole } from "@/logic/api"
import { useAsyncAction } from "@/services/useAsyncAction"
import { useVoterStore } from "@/store/VoterStore"

const props = defineProps<{ id: string }>()
const voter = useVoterStore()
const router = useRouter()
const { t } = useI18n()
const action = useAsyncAction<void>()
const step = ref<"choose" | "confirm" | "done">("choose")
const picked = ref<number[]>([])
const conflict = ref(false)

const session = computed(() => voter.session)
const item = computed(() => session.value?.items.find((i) => i.id === Number(props.id)))
const candidates = computed(() => item.value?.options.filter((o) => o.role === OptionRole.Candidate) ?? [])
const blank = computed(() => item.value?.options.find((o) => o.role === OptionRole.Blank))
const blankPicked = computed(() => !!blank.value && picked.value.includes(blank.value.id))
const candidatesPicked = computed(() => picked.value.filter((id) => id !== blank.value?.id).length)
const chosenLabels = computed(
	() =>
		item.value?.options
			.filter((o) => picked.value.includes(o.id))
			.map((o) => (o.role === OptionRole.Blank ? t("vote.blank") : o.label)) ?? []
)

const errorText = computed(() => {
	const kind = action.error.value?.kind
	if (kind === "authorization") return t("vote.closedMeanwhile")
	return action.error.value ? t(action.error.value.messageKey) : ""
})

onMounted(async () => {
	if (!voter.code) return void router.replace({ name: "start" })
	await voter.refresh()
})

function toggle(id: number): void {
	picked.value = picked.value.includes(id) ? picked.value.filter((p) => p !== id) : [...picked.value, id]
}

function toggleBlank(): void {
	if (!blank.value) return
	picked.value = blankPicked.value ? [] : [blank.value.id]
}

async function send(): Promise<void> {
	if (!item.value) return
	const itemId = item.value.id
	await action.run(() => voter.vote(itemId, picked.value))
	if (!action.error.value) step.value = "done"
	else if (action.error.value.kind === "conflict") conflict.value = true
}
</script>
