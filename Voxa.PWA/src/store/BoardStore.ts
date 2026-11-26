import { defineStore } from "pinia"
import { ref } from "vue"
import { useApi } from "@/services/useApi"

/** Whether the board is signed in (a cookie the app cannot read), and whether this is the trial setup. */
export const useBoardStore = defineStore("board", () => {
	const signedIn = ref(false)
	const trialMode = ref(false)
	const checked = ref(false)

	async function check(): Promise<boolean> {
		try {
			const session = (await useApi().admin.adminSessionList()).data
			signedIn.value = session.signedIn
			trialMode.value = session.trialMode
		} catch {
			signedIn.value = false
		} finally {
			checked.value = true
		}
		return signedIn.value
	}

	async function signIn(password: string): Promise<void> {
		const session = (await useApi().admin.adminSessionCreate({ password })).data
		signedIn.value = session.signedIn
		trialMode.value = session.trialMode
	}

	async function signOut(): Promise<void> {
		await useApi().admin.adminSessionDelete()
		signedIn.value = false
	}

	return { signedIn, trialMode, checked, check, signIn, signOut }
})
