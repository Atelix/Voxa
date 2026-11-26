<template>
	<v-app-bar flat border="b" color="surface" height="72">
		<div class="d-flex align-center ga-3 px-4 w-100" style="max-width: 640px; margin: 0 auto">
			<router-link
				:to="{ name: voter.session ? 'items' : 'start' }"
				class="d-flex align-center"
				:aria-label="$t('voter.home')">
				<club-logo />
			</router-link>
			<div class="d-flex flex-column" style="min-width: 0">
				<span class="voxa-heading text-primary text-truncate" style="font-size: 1rem">
					{{ voter.session?.meetingName ?? $t("voter.title") }}
				</span>
				<span class="text-body-small text-medium-emphasis text-truncate">
					{{ voter.code ? $t("voter.codeShown", { code: maskCode(voter.code) }) : club.brand.clubName }}
				</span>
			</div>
			<v-spacer />
			<v-btn v-if="voter.code" variant="text" size="small" @click="signOut">{{ $t("voter.signOut") }}</v-btn>
		</div>
	</v-app-bar>
	<v-main>
		<div style="max-width: 640px; margin: 0 auto" class="pb-8">
			<router-view />
		</div>
	</v-main>
</template>

<script setup lang="ts">
import { useRouter } from "vue-router"
import ClubLogo from "@/components/ClubLogo.vue"
import { maskCode } from "@/services/votingCode"
import { useClubStore } from "@/store/ClubStore"
import { useVoterStore } from "@/store/VoterStore"

const club = useClubStore()
const voter = useVoterStore()
const router = useRouter()

function signOut(): void {
	voter.signOut()
	void router.push({ name: "start" })
}
</script>
