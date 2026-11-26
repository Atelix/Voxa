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
						<span class="text-body-small" :class="statusClass(item.status)">{{ statusText(item.status) }}</span>
					</span>
					<v-icon
						:icon="statusIcon(item.status)"
						:color="item.status === BallotItemStatus.Open ? 'primary' : undefined" />
				</component>
			</div>

			<p class="text-body-small text-medium-emphasis mt-6 text-center">{{ $t("items.refreshes") }}</p>
		</template>
	</div>
</template>

<script setup lang="ts">
import { computed, onMounted } from "vue"
import { useI18n } from "vue-i18n"
import { useRouter } from "vue-router"
import { BallotItemStatus, VotingPhase } from "@/logic/api"
import { formatDate, formatDateTime } from "@/services/format"
import { usePolling } from "@/services/usePolling"
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
	if (!voter.code) void router.replace({ name: "start" })
})

// New items open during the meeting; check every few seconds.
usePolling(async () => {
	await voter.refresh()
	if (!voter.code) await router.replace({ name: "start" })
}, 5000)

function statusText(status: BallotItemStatus): string {
	return t(`items.status.${status}`)
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
