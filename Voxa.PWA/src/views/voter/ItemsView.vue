<template>
	<div class="px-4 pt-6">
		<v-skeleton-loader v-if="!session" type="heading, list-item-two-line@5" />

		<template v-else>
			<div class="d-flex align-center ga-2 mb-1">
				<h1 class="voxa-heading text-primary" style="font-size: 1.6rem">{{ $t("items.title") }}</h1>
				<v-spacer />
				<v-chip
					v-if="session.phase === VotingPhase.Meeting"
					color="secondary"
					variant="flat"
					size="small"
					prepend-icon="mdi-circle-medium">
					{{ $t("items.live") }}
				</v-chip>
			</div>
			<p class="text-body-medium text-medium-emphasis mb-4">
				{{ $t("items.progress", { voted: voted, total: session.items.length }) }} {{ phaseText }}
			</p>

			<v-alert v-if="session.phase === VotingPhase.NotYetOpen" type="info" variant="tonal" class="mb-4">
				{{
					session.advanceOpensAt
						? $t("items.advanceOpens", { date: formatDate(session.advanceOpensAt) })
						: $t("items.meetingStarts", { date: formatDateTime(session.startsAt) })
				}}
			</v-alert>
			<v-alert v-else-if="session.phase === VotingPhase.Ended" type="success" variant="tonal" class="mb-4">{{
				$t("items.ended")
			}}</v-alert>

			<div class="d-flex flex-column ga-3">
				<component
					:is="item.status === BallotItemStatus.Open ? 'router-link' : 'div'"
					v-for="item in session.items"
					:key="item.id"
					:to="item.status === BallotItemStatus.Open ? { name: 'vote', params: { id: item.id } } : undefined"
					class="voxa-choice text-decoration-none"
					:style="item.status === BallotItemStatus.Open ? { borderColor: 'rgb(var(--v-theme-primary))' } : {}">
					<span class="voxa-heading text-primary" style="width: 1.5rem">{{ item.position }}</span>
					<span class="d-flex flex-column flex-grow-1">
						<span class="font-weight-bold">{{ item.title }}</span>
						<span class="text-body-small" :class="statusClass(item.status)">{{ statusText(item) }}</span>
					</span>
					<v-icon
						:icon="statusIcon(item.status)"
						:color="item.status === BallotItemStatus.Open ? 'primary' : undefined" />
				</component>
			</div>

			<v-card variant="outlined" class="pa-4 mt-6">
				<h2 class="voxa-heading text-primary mb-1" style="font-size: 1.1rem">
					{{ $t("codes.title", voter.codes.length) }}
				</h2>
				<p class="text-body-medium text-medium-emphasis mb-3">
					{{ voter.codes.length > 1 ? $t("codes.several") : $t("codes.one") }}
				</p>
				<div class="d-flex flex-wrap ga-2 mb-3">
					<v-chip v-for="code in voter.codes" :key="code" variant="tonal" prepend-icon="mdi-key-outline">{{
						maskCode(code)
					}}</v-chip>
				</div>
				<form v-if="adding" @submit.prevent="add">
					<label for="voxa-extra-code" class="d-block text-body-medium font-weight-bold mb-2">{{
						$t("codes.addLabel")
					}}</label>
					<v-text-field
						id="voxa-extra-code"
						v-model="extra"
						class="voxa-code"
						placeholder="XXXX-XXXX"
						autocomplete="one-time-code"
						autocapitalize="characters"
						spellcheck="false"
						maxlength="9"
						:error-messages="addError ? [addError] : []"
						@update:model-value="onExtraInput" />
					<div class="d-flex ga-2">
						<v-btn
							type="submit"
							color="primary"
							:loading="addAction.pending.value"
							:disabled="!isCompleteCode(extra)"
							>{{ $t("codes.add") }}</v-btn
						>
						<v-btn variant="text" @click="adding = false">{{ $t("codes.cancel") }}</v-btn>
					</div>
				</form>
				<v-btn v-else variant="tonal" color="primary" prepend-icon="mdi-plus" @click="adding = true">{{
					$t("codes.addAnother")
				}}</v-btn>
			</v-card>

			<p class="text-body-small text-medium-emphasis mt-6 text-center">{{ $t("items.refreshes") }}</p>
		</template>
	</div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useI18n } from "vue-i18n"
import { useRouter } from "vue-router"
import { BallotItemStatus, VotingPhase, type BallotItemModel } from "@/logic/api"
import { formatDate, formatDateTime } from "@/services/format"
import { useAsyncAction } from "@/services/useAsyncAction"
import { usePolling } from "@/services/usePolling"
import { formatCode, isCompleteCode, maskCode } from "@/services/votingCode"
import { useVoterStore } from "@/store/VoterStore"

const voter = useVoterStore()
const router = useRouter()
const { t } = useI18n()
const session = computed(() => voter.session)
const voted = computed(() => session.value?.items.filter((i) => i.status === BallotItemStatus.Voted).length ?? 0)

const phaseText = computed(() => {
	if (session.value?.phase === VotingPhase.Advance) return t("items.phaseAdvance")
	if (session.value?.phase === VotingPhase.Meeting) return t("items.phaseMeeting")
	return ""
})

onMounted(() => {
	if (!voter.signedIn) void router.replace({ name: "start" })
})

// New items open during the meeting; check every few seconds.
usePolling(async () => {
	await voter.refresh()
	if (!voter.signedIn) await router.replace({ name: "start" })
}, 5000)

function statusText(item: BallotItemModel): string {
	const open = voter.openFor(item.id).length
	if (item.status === BallotItemStatus.Open && open < voter.codes.length)
		return t("items.openCodes", { open, total: voter.codes.length })
	return t(`items.status.${item.status}`)
}

const adding = ref(false)
const extra = ref("")
const addAction = useAsyncAction<void>()
const addError = computed(() => {
	const error = addAction.error.value
	if (!error) return ""
	if (error.kind === "not-found" || (error.kind === "validation" && error.messageKey === "apiError.validation"))
		return t("start.unknownCode")
	if (error.kind === "rate-limited") return t("start.blocked")
	return t(error.messageKey)
})

function onExtraInput(value: string): void {
	extra.value = formatCode(value)
	addAction.error.value = null
}

async function add(): Promise<void> {
	await addAction.run(() => voter.addCode(extra.value))
	if (!addAction.error.value) {
		extra.value = ""
		adding.value = false
	}
}

function statusIcon(status: BallotItemStatus): string {
	if (status === BallotItemStatus.Open) return "mdi-chevron-right"
	if (status === BallotItemStatus.Voted) return "mdi-check-circle"
	if (status === BallotItemStatus.Closed) return "mdi-lock-outline"
	return "mdi-clock-outline"
}

function statusClass(status: BallotItemStatus): string {
	return status === BallotItemStatus.Open ? "text-primary font-weight-bold" : "text-medium-emphasis"
}
</script>
