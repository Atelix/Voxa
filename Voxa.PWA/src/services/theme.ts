/** Helpers to derive readable colours from a club's brand colours. */

function channels(hex: string): [number, number, number] {
	const value = hex.replace("#", "")
	return [0, 2, 4].map((i) => parseInt(value.slice(i, i + 2), 16)) as [number, number, number]
}

/** WCAG relative luminance, 0 (black) to 1 (white). */
export function luminance(hex: string): number {
	const [r, g, b] = channels(hex).map((c) => {
		const s = c / 255
		return s <= 0.03928 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4
	}) as [number, number, number]
	return 0.2126 * r + 0.7152 * g + 0.0722 * b
}

export function contrast(a: string, b: string): number {
	const [light, dark] = [luminance(a), luminance(b)].sort((x, y) => y - x) as [number, number]
	return (light + 0.05) / (dark + 0.05)
}

/** Black or white, whichever reads better on the colour. */
export function onColor(hex: string): string {
	return contrast(hex, "#FFFFFF") >= contrast(hex, "#000000") ? "#FFFFFF" : "#000000"
}

/** A very light tint of a colour, for selected cards and backgrounds. */
export function tint(hex: string, amount = 0.88): string {
	const mixed = channels(hex).map((c) => Math.round(c + (255 - c) * amount))
	return `#${mixed.map((c) => c.toString(16).padStart(2, "0")).join("")}`.toUpperCase()
}
