import { describe, expect, it } from "vitest"
import { codesFromHash, formatCode, isCompleteCode, maskCode } from "@/services/votingCode"

describe("voting codes", () => {
	it("formats what people type", () => {
		expect(formatCode("k7q2 m9xd")).toBe("K7Q2-M9XD")
		expect(formatCode("K7Q")).toBe("K7Q")
		expect(formatCode("K7Q2-M9XD-EXTRA")).toBe("K7Q2-M9XD")
	})

	it("drops characters that are never in a code", () => {
		expect(formatCode("O0I1K7Q2M9XD")).toBe("K7Q2-M9XD")
	})

	it("knows a complete code", () => {
		expect(isCompleteCode("K7Q2M9XD")).toBe(true)
		expect(isCompleteCode("K7Q2M9X")).toBe(false)
	})

	it("masks the first half", () => {
		expect(maskCode("K7Q2-M9XD")).toBe("••••-M9XD")
	})

	it("reads the code from a mail link", () => {
		expect(codesFromHash("#code=K7Q2M9XD")).toEqual(["K7Q2-M9XD"])
		expect(codesFromHash("#other")).toEqual([])
	})

	it("reads every code from a link for several members", () => {
		expect(codesFromHash("#code=K7Q2M9XD,ABCD2345,K7Q2M9XD,XYZ")).toEqual(["K7Q2-M9XD", "ABCD-2345"])
	})
})
