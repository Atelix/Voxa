import { describe, expect, it } from "vitest"
import { OptionRole } from "@/logic/api"
import { optionLabel } from "./optionLabel"

const t = (key: string): string => `[${key}]`

describe("optionLabel", () => {
	it("translates the fixed options by their role", () => {
		expect(optionLabel({ role: OptionRole.For, label: "Voor" }, t)).toBe("[vote.option.For]")
		expect(optionLabel({ role: OptionRole.Against, label: "Tegen" }, t)).toBe("[vote.option.Against]")
		expect(optionLabel({ role: OptionRole.Abstain, label: "Onthouding" }, t)).toBe("[vote.option.Abstain]")
		expect(optionLabel({ role: OptionRole.Blank, label: "Blanco" }, t)).toBe("[vote.blank]")
	})

	it("keeps a candidate's name", () => {
		expect(optionLabel({ role: OptionRole.Candidate, label: "Kandidaat A" }, t)).toBe("Kandidaat A")
	})
})
