import { normalizeApiError, type ApiError } from "@/logic/ApiError"
import { ref, type Ref } from "vue"

export interface AsyncAction<T> {
	pending: Ref<boolean>
	error: Ref<ApiError | null>
	/** Runs the action unless one is already running, which protects against duplicate submissions. */
	run: (action: () => Promise<T>) => Promise<T | undefined>
}

export function useAsyncAction<T>(): AsyncAction<T> {
	const pending = ref(false)
	const error = ref<ApiError | null>(null)

	async function run(action: () => Promise<T>): Promise<T | undefined> {
		if (pending.value) return undefined
		pending.value = true
		error.value = null
		try {
			return await action()
		} catch (caught) {
			error.value = normalizeApiError(caught)
			return undefined
		} finally {
			pending.value = false
		}
	}

	return { pending, error, run }
}
