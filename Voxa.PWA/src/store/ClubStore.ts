import { defineStore } from "pinia"
import { ref } from "vue"
import type { ClubBrandModel } from "@/logic/api"
import { useApi } from "@/services/useApi"

/** Shown until the club's own brand arrives (and if it cannot be loaded). Matches ClubSettingsStore.Defaults. */
export const DEFAULT_BRAND: ClubBrandModel = {
	clubName: "Voxa",
	contactEmail: null,
	language: "nl",
	primaryColor: "#1F2A44",
	secondaryColor: "#4FB3D9",
	headingFont: "Montserrat",
	bodyFont: "Open Sans",
	logoDataUrl: null,
}

/** The club's name and brand. The app's colours and fonts follow it (see App.vue). */
export const useClubStore = defineStore("club", () => {
	const brand = ref<ClubBrandModel>(DEFAULT_BRAND)
	const loaded = ref(false)

	async function load(): Promise<void> {
		try {
			brand.value = (await useApi().club.clubList()).data
		} catch {
			// The app still works in its neutral colours.
		} finally {
			loaded.value = true
		}
	}

	function set(value: ClubBrandModel): void {
		brand.value = value
	}

	return { brand, loaded, load, set }
})
