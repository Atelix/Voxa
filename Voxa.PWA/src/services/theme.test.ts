import { describe, expect, it } from "vitest"
import { contrast, onColor, tint } from "@/services/theme"

describe("brand colours", () => {
	it("puts white text on navy and black text on sky blue", () => {
		expect(onColor("#231E5B")).toBe("#FFFFFF")
		expect(onColor("#62CDF6")).toBe("#000000")
	})

	it("measures contrast like WCAG", () => {
		expect(contrast("#FFFFFF", "#000000")).toBeCloseTo(21, 0)
	})

	it("tints towards white", () => {
		expect(tint("#000000", 0.5)).toBe("#808080")
	})
})
