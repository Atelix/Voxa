import { describe, expect, it } from "vitest"
import en from "@/languages/en"
import nl from "@/languages/nl"
import { browserLocale, resolveLocale } from "./i18n"

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

describe("browserLocale", () => {
	it("takes the first browser language the app has", () => {
		expect(browserLocale(["de-DE", "en-US", "nl"])).toBe("en")
		expect(browserLocale(["nl-BE"])).toBe("nl")
	})

	it("finds nothing for languages the app lacks", () => {
		expect(browserLocale(["de-DE", "fr"])).toBeUndefined()
		expect(browserLocale(undefined)).toBeUndefined()
	})
})

describe("resolveLocale", () => {
	it("puts the person's own choice first", () => {
		expect(resolveLocale("en", ["nl-NL"], "nl")).toBe("en")
	})

	it("follows the device when nobody chose", () => {
		expect(resolveLocale(null, ["en-GB"], "nl")).toBe("en")
	})

	it("falls back to the club's language, then Dutch", () => {
		expect(resolveLocale(null, ["de-DE"], "en")).toBe("en")
		expect(resolveLocale(null, ["de-DE"], "xx")).toBe("nl")
		expect(resolveLocale(null, [], undefined)).toBe("nl")
	})
})
