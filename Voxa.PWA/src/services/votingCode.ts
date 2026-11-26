/** The characters a code can contain: no 0, O, 1 or I, so they cannot be confused. */
export const CODE_ALPHABET = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"

/** Uppercases, drops everything that cannot be in a code and adds the dash: "k7q2 m9xd" becomes "K7Q2-M9XD". */
export function formatCode(input: string): string {
	const clean = [...input.toUpperCase()]
		.filter((c) => CODE_ALPHABET.includes(c))
		.slice(0, 8)
		.join("")
	return clean.length > 4 ? `${clean.slice(0, 4)}-${clean.slice(4)}` : clean
}

export function isCompleteCode(input: string): boolean {
	return formatCode(input).length === 9
}

/** "K7Q2-M9XD" shown as "••••-M9XD", so the code is not readable over someone's shoulder. */
export function maskCode(code: string): string {
	const formatted = formatCode(code)
	return formatted.length === 9 ? `••••-${formatted.slice(5)}` : formatted
}

/** The code from a mail link (#code=K7Q2M9XD). The fragment never reaches a server, so the code is not logged. */
export function codeFromHash(hash: string): string | null {
	const match = /code=([A-Za-z0-9-]+)/.exec(hash)
	return match?.[1] ? formatCode(match[1]) : null
}
