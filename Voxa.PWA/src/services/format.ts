const zone = "Europe/Amsterdam"

/** "dinsdag 2 februari 2027, 18:45" in the club's time zone. */
export function formatDateTime(iso: string, locale = "nl-NL"): string {
	return new Intl.DateTimeFormat(locale, { dateStyle: "full", timeStyle: "short", timeZone: zone }).format(
		new Date(iso)
	)
}

/** "3 januari 2027". */
export function formatDate(iso: string, locale = "nl-NL"): string {
	return new Intl.DateTimeFormat(locale, { day: "numeric", month: "long", year: "numeric", timeZone: zone }).format(
		new Date(iso)
	)
}

/** The value for an <input type="datetime-local">, in the browser's own time zone. */
export function toLocalInput(iso: string | null | undefined): string {
	if (!iso) return ""
	const date = new Date(iso)
	const pad = (n: number): string => String(n).padStart(2, "0")
	return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

/** An <input type="datetime-local"> value back to an ISO time, or null when empty. */
export function fromLocalInput(value: string): string | null {
	return value ? new Date(value).toISOString() : null
}

export function percentage(part: number, total: number): string {
	return total > 0 ? `${Math.round((part / total) * 100)}%` : "0%"
}
