/** How the app sees a failed request. The API answers failures with problem details (DomainExceptionHandler). */
export type ApiErrorKind =
	| "offline"
	| "timeout"
	| "cancelled"
	| "authentication"
	| "authorization"
	| "validation"
	| "not-found"
	| "conflict"
	| "rate-limited"
	| "server"
	| "invalid-response"
	| "unknown"

export interface ProblemBody {
	title?: string
	detail?: string
	status?: number
	errors?: Record<string, string[]>
}

export class ApiError extends Error {
	constructor(
		public readonly kind: ApiErrorKind,
		public readonly status?: number,
		public readonly retryable = false,
		public readonly messageKey = `apiError.${kind}`,
		options?: ErrorOptions,
		public readonly problem?: ProblemBody
	) {
		super(messageKey, options)
		this.name = "ApiError"
	}

	/** First server-provided validation message for a field, if any. */
	fieldError(field: string): string | undefined {
		const match = Object.entries(this.problem?.errors ?? {}).find(([key]) => key.toLowerCase() === field.toLowerCase())
		return match?.[1]?.[0]
	}
}

export function fromStatus(status: number, problem?: ProblemBody): ApiError {
	if (status === 401) return new ApiError("authentication", status, false, undefined, undefined, problem)
	if (status === 403) return new ApiError("authorization", status, false, undefined, undefined, problem)
	if (status === 404) return new ApiError("not-found", status, false, undefined, undefined, problem)
	if (status === 408) return new ApiError("timeout", status, true, undefined, undefined, problem)
	if (status === 409 || status === 412) return new ApiError("conflict", status, false, undefined, undefined, problem)
	if (status === 400 || status === 422) return new ApiError("validation", status, false, undefined, undefined, problem)
	if (status === 429) return new ApiError("rate-limited", status, true, undefined, undefined, problem)
	if (status >= 500) return new ApiError("server", status, true, undefined, undefined, problem)
	return new ApiError("unknown", status, false, undefined, undefined, problem)
}

function isProblem(value: unknown): value is ProblemBody {
	return typeof value === "object" && value !== null
}

/** Turns anything thrown by the generated client, fetch or our own code into an ApiError. */
export function normalizeApiError(error: unknown): ApiError {
	if (error instanceof ApiError) return error
	if (error instanceof Response) {
		const body = (error as Response & { error?: unknown }).error
		return fromStatus(error.status, isProblem(body) ? body : undefined)
	}
	if (error instanceof DOMException && error.name === "AbortError") {
		return new ApiError("cancelled", undefined, false, "apiError.cancelled", { cause: error })
	}
	if (error instanceof TypeError) {
		return new ApiError("offline", undefined, true, "apiError.offline", { cause: error })
	}
	return new ApiError("unknown", undefined, false, "apiError.unknown", { cause: error })
}
