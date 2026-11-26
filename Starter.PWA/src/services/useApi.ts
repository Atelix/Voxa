import { Api } from "@/logic/api"
import { apiFetch } from "@/services/apiFetch"
import { API_URL } from "@/services/apiUrl"

let client: Api<unknown> | undefined

/** The generated API client (src/logic/api.ts, see AGENTS.md), with the app's fetch wrapper. */
export function useApi(): Api<unknown> {
	client ??= new Api({ baseUrl: API_URL, customFetch: apiFetch })
	return client
}
