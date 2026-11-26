<template>
	<h1 class="voxa-heading text-primary mb-1" style="font-size: 1.8rem">{{ $t("board.settings.title") }}</h1>
	<p class="text-body-medium text-medium-emphasis mb-6">{{ $t("board.settings.lead") }}</p>

	<v-skeleton-loader v-if="loader.loading.value && !settings" type="card@3" />
	<v-alert v-else-if="loader.error.value && !settings" type="error" variant="tonal">{{
		$t(loader.error.value.messageKey)
	}}</v-alert>
	<template v-else-if="settings">
		<v-row>
			<v-col cols="12" md="7">
				<v-card class="pa-5 mb-4">
					<h2 class="voxa-heading mb-3" style="font-size: 1.1rem">{{ $t("board.settings.club") }}</h2>
					<v-text-field v-model="form.clubName" :label="$t('board.settings.clubName')" />
					<v-text-field
						v-model="form.contactEmail"
						:label="$t('board.settings.contactEmail')"
						:hint="$t('board.settings.contactHint')"
						persistent-hint
						class="mb-2" />
				</v-card>

				<v-card class="pa-5 mb-4">
					<h2 class="voxa-heading mb-3" style="font-size: 1.1rem">{{ $t("board.settings.brand") }}</h2>
					<div class="d-flex ga-4">
						<v-text-field v-model="form.primaryColor" :label="$t('board.settings.primary')">
							<template #append-inner
								><input v-model="form.primaryColor" type="color" :aria-label="$t('board.settings.primary')"
							/></template>
						</v-text-field>
						<v-text-field v-model="form.secondaryColor" :label="$t('board.settings.secondary')">
							<template #append-inner
								><input v-model="form.secondaryColor" type="color" :aria-label="$t('board.settings.secondary')"
							/></template>
						</v-text-field>
					</div>
					<v-alert v-if="lowContrast" type="warning" variant="tonal" density="compact" class="mb-4">{{
						$t("board.settings.contrast")
					}}</v-alert>
					<div class="d-flex ga-4">
						<v-select
							v-model="form.headingFont"
							:items="settings.availableFonts"
							:label="$t('board.settings.headingFont')" />
						<v-select v-model="form.bodyFont" :items="settings.availableFonts" :label="$t('board.settings.bodyFont')" />
					</div>
					<div class="d-flex align-center ga-4">
						<div
							class="pa-2 rounded"
							style="border: 1px solid rgba(var(--v-border-color), 0.2); min-width: 120px; min-height: 64px">
							<img v-if="form.logoDataUrl" :src="form.logoDataUrl" alt="" style="max-height: 60px; max-width: 200px" />
							<span v-else class="text-medium-emphasis text-body-small">{{ $t("board.settings.noLogo") }}</span>
						</div>
						<v-btn variant="outlined" @click="logoInput?.click()">{{ $t("board.settings.uploadLogo") }}</v-btn>
						<v-btn v-if="form.logoDataUrl" variant="text" @click="form.logoDataUrl = null">{{
							$t("common.delete")
						}}</v-btn>
						<input
							ref="logoInput"
							type="file"
							accept="image/png,image/jpeg,image/svg+xml,image/webp"
							hidden
							@change="readLogo" />
					</div>
					<p class="text-body-small text-medium-emphasis mt-2">{{ $t("board.settings.logoHint") }}</p>
				</v-card>

				<v-card class="pa-5 mb-4">
					<h2 class="voxa-heading mb-3" style="font-size: 1.1rem">{{ $t("board.settings.mail") }}</h2>
					<v-text-field v-model="form.mailFromName" :label="$t('board.settings.fromName')" />
					<v-text-field
						v-model="form.mailFromAddress"
						:label="$t('board.settings.fromAddress')"
						:hint="$t('board.settings.fromHint')"
						persistent-hint
						class="mb-2" />
					<v-text-field v-model="form.mailReplyTo" :label="$t('board.settings.replyTo')" />
				</v-card>

				<v-card class="pa-5 mb-4">
					<h2 class="voxa-heading mb-3" style="font-size: 1.1rem">{{ $t("board.settings.rules") }}</h2>
					<div class="d-flex ga-4">
						<v-text-field
							v-model.number="form.advanceVotingMaxDays"
							type="number"
							min="0"
							:label="$t('board.settings.advanceDays')" />
						<v-text-field v-model="form.advanceVotingArticle" :label="$t('board.settings.article')" />
					</div>
					<div class="d-flex ga-4">
						<v-select v-model="form.minorVoting" :items="minorOptions" :label="$t('board.settings.minors')" />
						<v-text-field
							v-model.number="form.adultAge"
							type="number"
							min="12"
							max="21"
							:label="$t('board.settings.adultAge')" />
					</div>
					<p class="text-body-small text-medium-emphasis">{{ $t("board.settings.secretNote") }}</p>
				</v-card>

				<v-card class="pa-5 mb-4">
					<h2 class="voxa-heading mb-3" style="font-size: 1.1rem">{{ $t("board.settings.members") }}</h2>
					<v-select v-model="form.memberSource" :items="sourceOptions" :label="$t('board.settings.source')" />
					<template v-if="form.memberSource === MemberSourceKind.Keeva">
						<v-text-field
							v-model="form.keevaUrl"
							:label="$t('board.settings.keevaUrl')"
							placeholder="https://club.keeva.app" />
						<v-alert :type="settings.keevaKeyConfigured ? 'success' : 'warning'" variant="tonal" density="compact">
							{{ settings.keevaKeyConfigured ? $t("board.settings.keevaKey") : $t("board.settings.keevaNoKey") }}
						</v-alert>
					</template>
				</v-card>

				<div class="d-flex flex-wrap ga-2">
					<v-btn color="primary" size="large" :loading="save.pending.value" @click="submit">{{
						$t("common.save")
					}}</v-btn>
					<v-btn variant="outlined" prepend-icon="mdi-download" @click="exportSettings">{{
						$t("board.settings.export")
					}}</v-btn>
					<v-btn variant="outlined" prepend-icon="mdi-upload" @click="importInput?.click()">{{
						$t("board.settings.import")
					}}</v-btn>
					<input ref="importInput" type="file" accept="application/json,.json" hidden @change="importSettings" />
				</div>
				<v-alert v-if="save.error.value" type="error" variant="tonal" class="mt-4">
					{{ $t(save.error.value.messageKey) }}
					<div v-if="save.error.value.problem?.detail" class="text-body-small">
						{{ save.error.value.problem.detail }}
					</div>
				</v-alert>
			</v-col>

			<v-col cols="12" md="5">
				<p class="voxa-eyebrow text-medium-emphasis mb-2">{{ $t("board.settings.preview") }}</p>
				<v-card class="overflow-hidden mb-6" :style="{ fontFamily: `'${form.bodyFont}'` }">
					<div class="pa-5" :style="{ background: form.primaryColor, color: onColor(form.primaryColor) }">
						<div class="voxa-eyebrow" :style="{ color: form.secondaryColor, fontFamily: `'${form.headingFont}'` }">
							{{ form.clubName }}
						</div>
						<div class="voxa-heading" :style="{ fontFamily: `'${form.headingFont}'`, fontSize: '1.8rem' }">
							{{ $t("start.title") }}
						</div>
					</div>
					<div class="pa-5">
						<div class="mb-3">{{ $t("start.codeHelp") }}</div>
						<div
							class="rounded-lg text-center py-3 voxa-heading"
							:style="{
								background: form.secondaryColor,
								color: onColor(form.secondaryColor),
								fontFamily: `'${form.headingFont}'`,
							}">
							{{ $t("start.continue") }}
						</div>
					</div>
				</v-card>

				<v-card variant="outlined" class="pa-5">
					<h2 class="voxa-heading mb-2" style="font-size: 1.1rem">{{ $t("board.settings.data") }}</h2>
					<p class="text-body-medium mb-4">{{ $t("board.settings.dataText") }}</p>
					<v-btn
						variant="outlined"
						block
						prepend-icon="mdi-download"
						class="mb-2"
						:loading="exporting"
						@click="exportResults"
						>{{ $t("board.settings.exportResults") }}</v-btn
					>
					<v-btn
						color="error"
						variant="outlined"
						block
						prepend-icon="mdi-delete-forever-outline"
						@click="wipeDialog = true"
						>{{ $t("board.settings.wipe") }}</v-btn
					>
				</v-card>
			</v-col>
		</v-row>

		<confirm-dialog
			v-model="wipeDialog"
			:title="$t('board.settings.wipe')"
			:text="$t('board.settings.wipeText')"
			:confirm="$t('board.settings.wipe')"
			:disabled="wipeWord !== 'WISSEN'"
			@confirm="wipe">
			<v-card-text class="pt-0"
				><v-text-field v-model="wipeWord" :label="$t('board.settings.wipeLabel')"
			/></v-card-text>
		</confirm-dialog>
		<v-snackbar v-model="snackbar" timeout="4000">{{ snackbarText }}</v-snackbar>
	</template>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue"
import { useI18n } from "vue-i18n"
import ConfirmDialog from "@/components/ConfirmDialog.vue"
import { MemberSourceKind, MinorVoting, type ClubSettingsModel, type ClubSettingsRequest } from "@/logic/api"
import { normalizeApiError } from "@/logic/ApiError"
import { contrast, onColor } from "@/services/theme"
import { useApi } from "@/services/useApi"
import { useAsyncAction } from "@/services/useAsyncAction"
import { useLoader } from "@/services/useLoader"
import { useClubStore } from "@/store/ClubStore"

const api = useApi()
const club = useClubStore()
const { t } = useI18n()
const loader = useLoader(async () => (await api.admin.adminClubList()).data)
const settings = computed(() => loader.data.value)
const save = useAsyncAction<ClubSettingsModel>()
const logoInput = ref<HTMLInputElement>()
const importInput = ref<HTMLInputElement>()
const wipeDialog = ref(false)
const wipeWord = ref("")
const exporting = ref(false)
const snackbar = ref(false)
const snackbarText = ref("")

const form = reactive<ClubSettingsRequest>({
	clubName: "",
	contactEmail: null,
	primaryColor: "#1F2A44",
	secondaryColor: "#4FB3D9",
	headingFont: "Montserrat",
	bodyFont: "Open Sans",
	logoDataUrl: null,
	mailFromName: "",
	mailFromAddress: null,
	mailReplyTo: null,
	advanceVotingMaxDays: 30,
	advanceVotingArticle: null,
	minorVoting: MinorVoting.Guardian,
	adultAge: 18,
	memberSource: MemberSourceKind.Csv,
	keevaUrl: null,
})

const minorOptions = computed(() => [
	{ title: t("board.settings.minorGuardian"), value: MinorVoting.Guardian },
	{ title: t("board.settings.minorSelf"), value: MinorVoting.Self },
	{ title: t("board.settings.minorNone"), value: MinorVoting.None },
])
const sourceOptions = computed(() => [
	{ title: t("board.settings.sourceCsv"), value: MemberSourceKind.Csv },
	{ title: t("board.settings.sourceKeeva"), value: MemberSourceKind.Keeva },
])
const lowContrast = computed(
	() => /^#[0-9A-Fa-f]{6}$/.test(form.primaryColor) && contrast(form.primaryColor, "#FFFFFF") < 4.5
)

onMounted(() => void loader.load())
watch(settings, (value) => {
	if (value) fill(value)
})

function fill(value: Partial<ClubSettingsRequest>): void {
	for (const key of Object.keys(form) as (keyof ClubSettingsRequest)[]) {
		if (key in value) (form as Record<string, unknown>)[key] = value[key] ?? null
	}
}

function notify(text: string): void {
	snackbarText.value = text
	snackbar.value = true
}

function clean(): ClubSettingsRequest {
	const empty = (v: string | null | undefined): string | null => (v && v.trim() ? v.trim() : null)
	return {
		...form,
		contactEmail: empty(form.contactEmail),
		mailFromAddress: empty(form.mailFromAddress),
		mailReplyTo: empty(form.mailReplyTo),
		advanceVotingArticle: empty(form.advanceVotingArticle),
		keevaUrl: empty(form.keevaUrl),
		mailFromName: form.mailFromName || form.clubName,
	}
}

async function submit(): Promise<void> {
	const saved = await save.run(async () => (await api.admin.adminClubUpdate(clean())).data.data)
	if (saved) {
		loader.data.value = saved
		club.set({ ...saved })
		notify(t("common.saved"))
	}
}

function readLogo(event: Event): void {
	const input = event.target as HTMLInputElement
	const file = input.files?.[0]
	input.value = ""
	if (!file) return
	if (file.size > 700_000) return notify(t("board.settings.logoTooBig"))
	const reader = new FileReader()
	reader.onload = (): void => {
		if (typeof reader.result === "string") form.logoDataUrl = reader.result
	}
	reader.readAsDataURL(file)
}

function download(name: string, content: string): void {
	const url = URL.createObjectURL(new Blob([content], { type: "application/json" }))
	const link = document.createElement("a")
	link.href = url
	link.download = name
	link.click()
	URL.revokeObjectURL(url)
}

function exportSettings(): void {
	download("voxa-club.json", JSON.stringify(clean(), null, 2))
}

async function importSettings(event: Event): Promise<void> {
	const input = event.target as HTMLInputElement
	const file = input.files?.[0]
	input.value = ""
	if (!file) return
	try {
		fill(JSON.parse(await file.text()) as Partial<ClubSettingsRequest>)
		notify(t("board.settings.imported"))
	} catch {
		notify(t("board.settings.importFailed"))
	}
}

async function exportResults(): Promise<void> {
	exporting.value = true
	try {
		const data = (await api.admin.adminDataExportList()).data
		download(`voxa-uitslagen-${new Date().toISOString().slice(0, 10)}.json`, JSON.stringify(data, null, 2))
	} catch (error) {
		notify(t(normalizeApiError(error).messageKey))
	} finally {
		exporting.value = false
	}
}

async function wipe(): Promise<void> {
	try {
		await api.admin.adminDataWipeCreate({ confirm: wipeWord.value })
		wipeWord.value = ""
		notify(t("board.settings.wiped"))
	} catch (error) {
		notify(t(normalizeApiError(error).messageKey))
	}
}
</script>
