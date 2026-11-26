<template>
	<v-app-bar flat border="b" color="surface" class="voxa-no-print">
		<div class="d-flex align-center ga-3 px-4 w-100">
			<router-link :to="{ name: 'meetings' }" class="d-flex align-center"><club-logo height="40px" /></router-link>
			<span class="voxa-heading text-primary d-none d-sm-inline" style="font-size: 1rem">{{ $t("board.title") }}</span>
			<v-spacer />
			<v-btn variant="text" :to="{ name: 'meetings' }" exact>{{ $t("board.nav.meetings") }}</v-btn>
			<v-btn variant="text" :to="{ name: 'settings' }">{{ $t("board.nav.settings") }}</v-btn>
			<v-btn v-if="board.trialMode" variant="text" :to="{ name: 'outbox' }">{{ $t("board.nav.outbox") }}</v-btn>
			<language-menu />
			<v-btn icon="mdi-logout" variant="text" :aria-label="$t('board.nav.signOut')" @click="signOut" />
		</div>
	</v-app-bar>
	<v-main>
		<v-alert v-if="board.trialMode" type="info" variant="tonal" density="compact" rounded="0" class="voxa-no-print">{{
			$t("board.trialBanner")
		}}</v-alert>
		<v-container max-width="1100" class="py-6">
			<router-view />
		</v-container>
	</v-main>
</template>

<script setup lang="ts">
import { useRouter } from "vue-router"
import ClubLogo from "@/components/ClubLogo.vue"
import LanguageMenu from "@/components/LanguageMenu.vue"
import { useBoardStore } from "@/store/BoardStore"

const board = useBoardStore()
const router = useRouter()

async function signOut(): Promise<void> {
	await board.signOut()
	await router.replace({ name: "board-login" })
}
</script>
