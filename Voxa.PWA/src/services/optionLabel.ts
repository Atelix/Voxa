import { OptionRole } from "@/logic/api"

type Translate = (key: string) => string

/**
 * The text of an option in the app's language. The API stores the fixed options (for, against, abstain, blank) with
 * Dutch labels, so those are shown by their role; candidates keep the name the board typed.
 */
export function optionLabel(option: { role: OptionRole; label: string }, t: Translate): string {
	switch (option.role) {
		case OptionRole.For:
		case OptionRole.Against:
		case OptionRole.Abstain:
			return t(`vote.option.${option.role}`)
		case OptionRole.Blank:
			return t("vote.blank")
		default:
			return option.label
	}
}
