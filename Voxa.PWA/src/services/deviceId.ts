const STORAGE_KEY = "voxa.device"

let memory: string | undefined

/**
 * A random id for this browser, sent with every request so the API can count wrong codes per phone: in a hall
 * everyone shares one network address. It identifies nothing else and is never stored by the API.
 */
export function deviceId(): string {
	if (memory) return memory
	try {
		memory = localStorage.getItem(STORAGE_KEY) ?? undefined
		if (!memory) {
			memory = crypto.randomUUID()
			localStorage.setItem(STORAGE_KEY, memory)
		}
	} catch {
		memory ??= crypto.randomUUID()
	}
	return memory
}
