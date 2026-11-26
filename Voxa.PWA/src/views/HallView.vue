<template>
	<v-main class="bg-primary">
		<div class="d-flex flex-column" style="min-height: 100vh; padding: 4vh 5vw">
			<div class="d-flex align-center ga-4 mb-6">
				<div class="bg-surface rounded-lg pa-2"><club-logo height="56px" /></div>
				<div>
					<div class="voxa-heading" style="font-size: 1.4rem">{{ club.brand.clubName }}</div>
					<div style="opacity: 0.8">{{ screen?.meetingName }}</div>
				</div>
			</div>

			<v-skeleton-loader v-if="!screen && !failed" type="heading, paragraph" bg-color="transparent" />
			<p v-else-if="failed" class="voxa-heading" style="font-size: 2rem">{{ $t("hall.unavailable") }}</p>

			<!-- A result the board put on screen. -->
			<template v-else-if="screen?.result && !screen.openItem">
				<p class="voxa-eyebrow text-secondary mb-2">{{ $t("hall.result", { n: screen.result.position }) }}</p>
				<h1 class="voxa-heading mb-4" style="font-size: clamp(2rem, 5vw, 4rem)">{{ screen.result.title }}</h1>
				<p class="voxa-heading text-secondary mb-6" style="font-size: clamp(1.5rem, 3vw, 2.5rem)">
					{{ $t(`board.result.outcome.${screen.result.outcome}`) }}
				</p>
				<v-card class="pa-8" style="max-width: 1000px">
					<result-bars :result="screen.result" large />
					<p class="mt-6 text-medium-emphasis">{{ $t("hall.counted", { n: screen.result.ballots.total }) }}</p>
				</v-card>
			</template>

			<!-- The open item with the QR code. -->
			<div v-else class="d-flex flex-wrap ga-12 align-center flex-grow-1">
				<div style="flex: 1 1 420px">
					<template v-if="screen?.openItem">
						<p class="voxa-eyebrow text-secondary mb-2">{{ $t("hall.open", { n: screen.openItem.position }) }}</p>
						<h1 class="voxa-heading mb-8" style="font-size: clamp(2rem, 5vw, 4.5rem)">{{ screen.openItem.title }}</h1>
					</template>
					<h1 v-else class="voxa-heading mb-8" style="font-size: clamp(2rem, 5vw, 4rem)">{{ $t("hall.waiting") }}</h1>
					<ol class="d-flex flex-column ga-4" style="list-style: none; font-size: clamp(1.1rem, 2vw, 1.6rem)">
						<li v-for="n in 3" :key="n" class="d-flex align-center ga-4">
							<span
								class="voxa-heading bg-secondary rounded-circle d-flex align-center justify-center flex-shrink-0"
								style="width: 48px; height: 48px"
								>{{ n }}</span
							>
							{{ $t(`hall.step${n}`) }}
						</li>
					</ol>
					<p class="mt-8" style="opacity: 0.8; font-size: 1.2rem">{{ $t("hall.noPhone") }}</p>
				</div>
				<div class="text-center">
					<div class="bg-surface pa-4 rounded-xl d-inline-block"><qr-code :value="origin" :size="320" /></div>
					<p class="voxa-heading mt-4" style="font-size: 1.6rem">{{ host }}</p>
					<p v-if="screen?.openItem" class="voxa-heading text-secondary mt-2" style="font-size: 2rem">
						{{ $t("hall.received", { n: screen.openItem.ballots.total }) }}
					</p>
				</div>
			</div>
		</div>
	</v-main>
</template>

<script setup lang="ts">
import { ref } from "vue"
import ClubLogo from "@/components/ClubLogo.vue"
import QrCode from "@/components/QrCode.vue"
import ResultBars from "@/components/ResultBars.vue"
import type { ScreenModel } from "@/logic/api"
import { useApi } from "@/services/useApi"
import { usePolling } from "@/services/usePolling"
import { useClubStore } from "@/store/ClubStore"

const props = defineProps<{ meetingId: string }>()
const api = useApi()
const club = useClubStore()
const screen = ref<ScreenModel | null>(null)
const failed = ref(false)
const origin = window.location.origin
const host = window.location.host

usePolling(async () => {
	try {
		screen.value = (await api.screen.screenDetail(Number(props.meetingId))).data
		failed.value = false
	} catch {
		failed.value = !screen.value
	}
}, 3000)
</script>
