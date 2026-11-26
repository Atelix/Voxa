import { ref, type Ref } from "vue"
import { normalizeApiError, type ApiError } from "@/logic/ApiError"

export interface Loader<T> {
	/** The last successful result, or null before the first one arrives. */
	data: Ref<T | null>
	/** True from the start until the first response and while a reload runs. Drives the skeleton loader. */
	loading: Ref<boolean>
	error: Ref<ApiError | null>
	load: () => Promise<void>
	/** Ignore an in-flight response while a replacement request is being scheduled. */
	invalidate: () => void
}

/**
 * Loading state for data a view shows. `loading` starts true, so a view renders its skeleton instead of an
 * empty or "not found" state until the first response (see AGENTS.md). A response that arrives after a newer
 * request is dropped, and failures are normalised to an ApiError.
 */
export function useLoader<T>(fetcher: () => Promise<T>): Loader<T> {
	const data = ref(null) as Ref<T | null>
	const loading = ref(true)
	const error = ref<ApiError | null>(null)
	let sequence = 0

	function invalidate(): void {
		sequence++
		loading.value = true
		error.value = null
	}

	async function load(): Promise<void> {
		const current = ++sequence
		loading.value = true
		error.value = null
		try {
			const result = await fetcher()
			if (current === sequence) data.value = result
		} catch (caught) {
			if (current === sequence) error.value = normalizeApiError(caught)
		} finally {
			if (current === sequence) loading.value = false
		}
	}

	return { data, loading, error, load, invalidate }
}
