import { describe, expect, it } from "vitest"
import en from "@/languages/en"
import nl from "@/languages/nl"

function keys(messages: object, prefix = ""): string[] {
	return Object.entries(messages).flatMap(([key, value]) =>
		typeof value === "object" && value !== null ? keys(value as object, `${prefix}${key}.`) : [`${prefix}${key}`]
	)
}

describe("translations", () => {
	it("has every English text in Dutch too", () => {
		expect(keys(nl).sort()).toEqual(keys(en).sort())
	})
})
