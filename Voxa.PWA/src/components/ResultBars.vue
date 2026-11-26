<template>
	<div class="d-flex flex-column ga-3">
		<div v-for="(option, index) in rows" :key="option.optionId">
			<div class="d-flex align-center mb-1" :style="{ fontSize: large ? '1.6rem' : undefined }">
				<span class="font-weight-bold">{{ option.role === OptionRole.Blank ? $t("vote.blank") : option.label }}</span>
				<v-icon
					v-if="option.elected"
					icon="mdi-check-decagram"
					color="success"
					class="ml-2"
					:aria-label="$t('board.result.elected')" />
				<v-spacer />
				<span>{{ option.votes.total }} · {{ percentage(option.votes.total, base) }}</span>
			</div>
			<div
				class="rounded"
				:style="{ height: large ? '28px' : '14px', background: 'rgba(var(--v-border-color), 0.08)' }">
				<div
					class="rounded h-100"
					:style="{ width: width(option.votes.total), background: color(option.role, index) }" />
			</div>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { OptionRole, type ResultModel } from "@/logic/api"
import { percentage } from "@/services/format"

const props = defineProps<{ result: ResultModel; large?: boolean }>()
const rows = computed(() => props.result.options)
// Bars are a share of the ballots; abstentions and blank votes are shown but do not count for the outcome.
const base = computed(() => Math.max(props.result.ballots.total, 1))

function width(n: number): string {
	return `${Math.min(100, (n / base.value) * 100).toFixed(1)}%`
}

function color(role: OptionRole, index: number): string {
	if (role === OptionRole.For) return "rgb(var(--v-theme-primary))"
	if (role === OptionRole.Against) return "rgb(var(--v-theme-secondary))"
	if (role === OptionRole.Abstain || role === OptionRole.Blank) return "#9A9CB5"
	return index % 2 === 0 ? "rgb(var(--v-theme-primary))" : "rgb(var(--v-theme-secondary))"
}
</script>
