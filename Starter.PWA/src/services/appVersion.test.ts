import { describe, expect, it } from "vitest"
import { versionsDiffer } from "./appVersion"

describe("versionsDiffer", () => {
	it("warns only when two real releases differ", () => {
		expect(versionsDiffer("1.2.0", "1.3.0")).toBe(true)
		expect(versionsDiffer("1.2.0", "1.2.0")).toBe(false)
		expect(versionsDiffer("1.2.0", null)).toBe(false)
		expect(versionsDiffer("development", "1.3.0")).toBe(false)
		expect(versionsDiffer("1.2.0", "development")).toBe(false)
	})
})
