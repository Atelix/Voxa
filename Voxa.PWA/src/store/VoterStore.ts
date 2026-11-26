import { defineStore } from "pinia"
import { computed, ref } from "vue"
import { ApiError, normalizeApiError } from "@/logic/ApiError"
import { mergeSessions, openCodes, sameMeeting, type CodeSession } from "@/services/ballotCodes"
import { useApi } from "@/services/useApi"
import { formatCode } from "@/services/votingCode"

const STORAGE_KEY = "voxa.codes"

function readCodes(): string[] {
	try {
		const stored = sessionStorage.getItem(STORAGE_KEY)
		const parsed: unknown = stored ? JSON.parse(stored) : []
		return Array.isArray(parsed) ? parsed.filter((c): c is string => typeof c === "string") : []
	} catch {
		return []
	}
}

async function fetchSession(code: string): Promise<CodeSession> {
	return { code, session: (await useApi().ballot.ballotSessionCreate({ code })).data }
}

/**
 * The voter's codes and what they may vote on. A parent of several youth members holds a code per child; each code
 * is its own vote. The codes are kept for this browser tab only (sessionStorage), so closing the tab forgets them,
 * and they are sent in request bodies, never in an address. The server never learns which codes are used together.
 */
export const useVoterStore = defineStore("voter", () => {
	const codes = ref<string[]>(readCodes())
	const sessions = ref<CodeSession[]>([])
	const error = ref<ApiError | null>(null)
	const loading = ref(false)

	/** The meeting as seen with all codes: an item is open while any code can still vote on it. */
	const session = computed(() => mergeSessions(sessions.value))
	const signedIn = computed(() => codes.value.length > 0)

	function remember(values: string[]): void {
		codes.value = values
		try {
			if (values.length) sessionStorage.setItem(STORAGE_KEY, JSON.stringify(values))
			else sessionStorage.removeItem(STORAGE_KEY)
		} catch {
			// Not kept across reloads; the codes still work in this page.
		}
	}

	function keep(found: CodeSession[]): void {
		sessions.value = found
		remember(found.map((s) => s.code))
	}

	/** Checks the codes from a link or the start page and makes them this device's codes. Returns false when none works. */
	async function signIn(values: string[]): Promise<boolean> {
		loading.value = true
		error.value = null
		try {
			const found: CodeSession[] = []
			for (const value of values) {
				try {
					const next = await fetchSession(value)
					if (!found[0] || sameMeeting(found[0].session, next.session)) found.push(next)
				} catch (caught) {
					error.value = normalizeApiError(caught)
					// Blocked: further tries would only count as more wrong codes.
					if (error.value.kind === "rate-limited") break
				}
			}
			if (!found.length) return false
			error.value = null
			keep(found)
			return true
		} finally {
			loading.value = false
		}
	}

	/**
	 * Adds another code, for example a child's code that came in a separate mail or on a paper slip. Throws an
	 * ApiError when the code is unknown, already on this device, or for another meeting.
	 */
	async function addCode(value: string): Promise<void> {
		const code = formatCode(value)
		if (codes.value.includes(code)) throw new ApiError("conflict", 409, false, "codes.alreadyAdded")
		const next = await fetchSession(code)
		const first = sessions.value[0]
		if (first && !sameMeeting(first.session, next.session))
			throw new ApiError("validation", 400, false, "codes.otherMeeting")
		keep([...sessions.value, next])
	}

	async function refresh(): Promise<void> {
		if (!codes.value.length) return
		const found: CodeSession[] = []
		let ended = 0
		for (const code of codes.value) {
			try {
				found.push(await fetchSession(code))
			} catch (caught) {
				const normalized = normalizeApiError(caught)
				// An unknown code (archived meeting) is dropped; a network hiccup keeps the current view.
				if (normalized.kind !== "not-found") {
					error.value = normalized
					return
				}
				ended++
			}
		}
		if (ended) keep(found)
		else sessions.value = found
	}

	/** The codes that can still vote on an item. */
	function openFor(itemId: string): string[] {
		return openCodes(sessions.value, itemId)
	}

	/**
	 * Casts the same choice for each of the given codes, one ballot per code. A code that fails does not stop the
	 * others; the first failure is thrown after the session is refreshed, so the page shows which codes still can vote.
	 */
	async function vote(itemId: string, optionIds: string[], forCodes: string[]): Promise<void> {
		if (!forCodes.length) throw new Error("No code")
		let failure: ApiError | null = null
		for (const code of forCodes) {
			try {
				await useApi().ballot.ballotVotesCreate({ code, itemId, optionIds })
			} catch (caught) {
				failure ??= normalizeApiError(caught)
			}
		}
		await refresh()
		if (failure) throw failure
	}

	function signOut(): void {
		remember([])
		sessions.value = []
	}

	return { codes, sessions, session, signedIn, error, loading, signIn, addCode, refresh, openFor, vote, signOut }
})
