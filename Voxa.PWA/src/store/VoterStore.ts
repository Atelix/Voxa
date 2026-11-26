import { defineStore } from "pinia"
import { ref } from "vue"
import type { BallotSessionModel } from "@/logic/api"
import { normalizeApiError, type ApiError } from "@/logic/ApiError"
import { useApi } from "@/services/useApi"

const STORAGE_KEY = "voxa.code"

function readCode(): string | null {
	try {
		return sessionStorage.getItem(STORAGE_KEY)
	} catch {
		return null
	}
}

/**
 * The voter's code and what it may vote on. The code is kept for this browser tab only (sessionStorage), so closing
 * the tab forgets it, and it is sent in request bodies, never in an address.
 */
export const useVoterStore = defineStore("voter", () => {
	const code = ref<string | null>(readCode())
	const session = ref<BallotSessionModel | null>(null)
	const error = ref<ApiError | null>(null)
	const loading = ref(false)

	function remember(value: string | null): void {
		code.value = value
		try {
			if (value) sessionStorage.setItem(STORAGE_KEY, value)
			else sessionStorage.removeItem(STORAGE_KEY)
		} catch {
			// Not kept across reloads; the code still works in this page.
		}
	}

	/** Checks a code and loads its session. Returns false when the code is unknown or blocked. */
	async function signIn(value: string): Promise<boolean> {
		loading.value = true
		error.value = null
		try {
			session.value = (await useApi().ballot.ballotSessionCreate({ code: value })).data
			remember(value)
			return true
		} catch (caught) {
			error.value = normalizeApiError(caught)
			return false
		} finally {
			loading.value = false
		}
	}

	async function refresh(): Promise<void> {
		if (!code.value) return
		try {
			session.value = (await useApi().ballot.ballotSessionCreate({ code: code.value })).data
		} catch (caught) {
			const normalized = normalizeApiError(caught)
			// An unknown code (archived meeting) ends the session; a network hiccup keeps the current view.
			if (normalized.kind === "not-found") signOut()
			error.value = normalized
		}
	}

	async function vote(itemId: number, optionIds: number[]): Promise<void> {
		if (!code.value) throw new Error("No code")
		await useApi().ballot.ballotVotesCreate({ code: code.value, itemId, optionIds })
		await refresh()
	}

	function signOut(): void {
		remember(null)
		session.value = null
	}

	return { code, session, error, loading, signIn, refresh, vote, signOut }
})
