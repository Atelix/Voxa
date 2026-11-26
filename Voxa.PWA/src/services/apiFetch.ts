import { ApiError, normalizeApiError } from "@/logic/ApiError"
import { recordApiVersion } from "@/services/appVersion"
import { deviceId } from "@/services/deviceId"

const TIMEOUT_MS = 20_000
const MAX_READ_RETRIES = 2
const RETRYABLE_STATUSES = [502, 503, 504]

/**
 * fetch for the generated client: enforces a timeout, retries idempotent reads on transient failures, records the
 * API version header, and throws every failure as an ApiError. The board's sign-in is a cookie, so requests include
 * credentials; the device id lets the API limit wrong codes per phone instead of per hall.
 */
export const apiFetch: typeof fetch = async (input, init = {}) => {
	const method = (init.method ?? "GET").toUpperCase()
	const canRetry = method === "GET" || method === "HEAD"
	const headers = new Headers(init.headers)
	headers.set("X-Voxa-Device", deviceId())
	init = { ...init, headers, credentials: "include" }

	for (let attempt = 0; ; attempt++) {
		let response: Response
		try {
			response = await fetchWithTimeout(input, init)
		} catch (error) {
			const normalized = normalizeApiError(error)
			if (canRetry && attempt < MAX_READ_RETRIES && normalized.retryable && !init.signal?.aborted) continue
			throw normalized
		}

		recordApiVersion(response.headers.get("X-App-Version"))
		if (canRetry && attempt < MAX_READ_RETRIES && RETRYABLE_STATUSES.includes(response.status)) continue
		return response
	}
}

async function fetchWithTimeout(input: RequestInfo | URL, init: RequestInit): Promise<Response> {
	const controller = new AbortController()
	const timer = setTimeout(() => controller.abort(new DOMException("Timed out", "TimeoutError")), TIMEOUT_MS)
	const abort = (): void => controller.abort(init.signal?.reason)
	init.signal?.addEventListener("abort", abort, { once: true })
	try {
		return await fetch(input, { ...init, signal: controller.signal })
	} catch (error) {
		if (controller.signal.reason instanceof DOMException && controller.signal.reason.name === "TimeoutError") {
			throw new ApiError("timeout", undefined, true, "apiError.timeout", { cause: error })
		}
		throw error
	} finally {
		clearTimeout(timer)
		init.signal?.removeEventListener("abort", abort)
	}
}
