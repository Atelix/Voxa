<template>
	<v-main>
		<v-container max-width="420" class="py-12">
			<div class="d-flex justify-center mb-6"><club-logo height="64px" /></div>
			<v-card class="pa-6">
				<h1 class="voxa-heading text-primary mb-1" style="font-size: 1.4rem">{{ $t("board.login.title") }}</h1>
				<p class="text-body-medium text-medium-emphasis mb-4">{{ $t("board.login.text") }}</p>
				<form @submit.prevent="submit">
					<v-text-field
						v-model="password"
						type="password"
						autocomplete="current-password"
						:label="$t('board.login.password')"
						:error-messages="errorText ? [errorText] : []" />
					<v-btn type="submit" color="primary" size="large" block :loading="action.pending.value" :disabled="!password">
						{{ $t("board.login.submit") }}
					</v-btn>
				</form>
			</v-card>
			<div class="d-flex justify-center align-center mt-4">
				<v-btn variant="text" :to="{ name: 'start' }">{{ $t("board.login.toVoting") }}</v-btn>
				<language-menu />
			</div>
		</v-container>
	</v-main>
</template>

<script setup lang="ts">
import { computed, ref } from "vue"
import { useI18n } from "vue-i18n"
import { useRoute, useRouter } from "vue-router"
import ClubLogo from "@/components/ClubLogo.vue"
import LanguageMenu from "@/components/LanguageMenu.vue"
import { useAsyncAction } from "@/services/useAsyncAction"
import { useBoardStore } from "@/store/BoardStore"

const board = useBoardStore()
const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const password = ref("")
const action = useAsyncAction<void>()

const errorText = computed(() => {
	const kind = action.error.value?.kind
	if (kind === "validation") return t("board.login.wrong")
	if (kind === "rate-limited") return t("board.login.blocked")
	return action.error.value ? t(action.error.value.messageKey) : ""
})

async function submit(): Promise<void> {
	await action.run(() => board.signIn(password.value))
	if (board.signedIn) {
		const back =
			typeof route.query.return === "string" && route.query.return.startsWith("/admin/") ? route.query.return : "/admin"
		await router.replace(back)
	}
}
</script>
