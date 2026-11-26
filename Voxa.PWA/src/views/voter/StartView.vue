<template>
	<section class="bg-primary px-6 pt-8 pb-14">
		<p class="voxa-eyebrow text-secondary mb-2">{{ club.brand.clubName }}</p>
		<h1 class="voxa-heading" style="font-size: 2.4rem">{{ $t("start.title") }}</h1>
		<p class="mt-2 text-body-large" style="opacity: 0.85">{{ $t("start.subtitle") }}</p>
	</section>

	<v-card class="mx-4 pa-6" style="margin-top: -32px" elevation="3">
		<form @submit.prevent="submit">
			<label for="voxa-code" class="voxa-heading text-primary d-block mb-3" style="font-size: 1.1rem">{{
				$t("start.codeLabel")
			}}</label>
			<v-text-field
				id="voxa-code"
				v-model="code"
				class="voxa-code"
				placeholder="XXXX-XXXX"
				autocomplete="one-time-code"
				autocapitalize="characters"
				spellcheck="false"
				inputmode="text"
				maxlength="9"
				:error-messages="errorText ? [errorText] : []"
				@update:model-value="onInput" />
			<p class="text-body-medium text-medium-emphasis mb-4">{{ $t("start.codeHelp") }}</p>
			<v-btn type="submit" color="secondary" size="x-large" block :loading="voter.loading" :disabled="!complete">
				{{ $t("start.continue") }}
			</v-btn>
		</form>
	</v-card>

	<div class="px-6 pt-6 d-flex flex-column ga-3 text-body-medium">
		<div class="d-flex ga-3">
			<v-icon icon="mdi-shield-lock-outline" color="primary" />
			<span>
				{{ $t("start.secret") }}
				<router-link :to="{ name: 'privacy' }" class="text-primary font-weight-bold">{{
					$t("start.howItWorks")
				}}</router-link>
			</span>
		</div>
		<div class="d-flex ga-3">
			<v-icon icon="mdi-email-outline" color="primary" />
			<span>
				{{ club.brand.contactEmail ? $t("start.noCodeMail", { email: club.brand.contactEmail }) : $t("start.noCode") }}
			</span>
		</div>
	</div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue"
import { useI18n } from "vue-i18n"
import { useRouter } from "vue-router"
import { codesFromHash, formatCode, isCompleteCode } from "@/services/votingCode"
import { useClubStore } from "@/store/ClubStore"
import { useVoterStore } from "@/store/VoterStore"

const club = useClubStore()
const voter = useVoterStore()
const router = useRouter()
const { t } = useI18n()
const code = ref("")
const complete = computed(() => isCompleteCode(code.value))

const errorText = computed(() => {
	const kind = voter.error?.kind
	if (kind === "not-found") return t("start.unknownCode")
	if (kind === "rate-limited") return t("start.blocked")
	if (kind === "validation") return t("start.unknownCode")
	return voter.error ? t(voter.error.messageKey) : ""
})

function onInput(value: string): void {
	code.value = formatCode(value)
	voter.error = null
}

async function submit(): Promise<void> {
	if (!complete.value) return
	if (await voter.signIn([code.value])) await router.push({ name: "items" })
}

onMounted(async () => {
	// A link from the mail carries the codes after the #, which never reaches a server. A parent of several members
	// gets one link with a code for each of them.
	const fromLink = codesFromHash(window.location.hash)
	if (fromLink.length) {
		history.replaceState(null, "", window.location.pathname)
		code.value = fromLink[0] ?? ""
		if (await voter.signIn(fromLink)) await router.push({ name: "items" })
	} else if (voter.signedIn) {
		await router.replace({ name: "items" })
	}
})
</script>
