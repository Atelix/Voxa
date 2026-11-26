import { describe, expect, it } from "vitest"
import { AgendaItemKind, BallotItemStatus, VotingPhase, type BallotSessionModel } from "@/logic/api"
import { mergeSessions, openCodes, sameMeeting, type CodeSession } from "@/services/ballotCodes"

/** The id of the agenda item at a position, the same in every session. */
const itemId = (position: number): string => `00000000-0000-0000-0000-${String(position).padStart(12, "0")}`

function session(...statuses: BallotItemStatus[]): BallotSessionModel {
	return {
		meetingName: "ALV 2027",
		startsAt: "2027-02-02T17:45:00Z",
		location: null,
		advanceOpensAt: null,
		phase: VotingPhase.Meeting,
		items: statuses.map((status, i) => ({
			id: itemId(i + 1),
			position: i + 1,
			title: `Punt ${i + 1}`,
			description: null,
			kind: AgendaItemKind.Proposal,
			seats: 1,
			status,
			options: [],
		})),
	}
}

const { Open, Voted, Closed, Pending } = BallotItemStatus

describe("several codes on one device", () => {
	const codes: CodeSession[] = [
		{ code: "AAAA-AAAA", session: session(Voted, Open, Voted, Closed, Pending) },
		{ code: "BBBB-BBBB", session: session(Open, Open, Voted, Closed, Pending) },
	]

	it("keeps an item open while any code can still vote, and voted once all have", () => {
		expect(mergeSessions(codes)?.items.map((i) => i.status)).toEqual([Open, Open, Voted, Closed, Pending])
	})

	it("lists the codes that can still vote on an item", () => {
		expect(openCodes(codes, itemId(1))).toEqual(["BBBB-BBBB"])
		expect(openCodes(codes, itemId(2))).toEqual(["AAAA-AAAA", "BBBB-BBBB"])
		expect(openCodes(codes, itemId(3))).toEqual([])
	})

	it("has nothing to show without codes", () => {
		expect(mergeSessions([])).toBeNull()
	})

	it("only combines codes of the same meeting", () => {
		expect(sameMeeting(session(), session())).toBe(true)
		expect(sameMeeting(session(), { ...session(), meetingName: "BALV 2027" })).toBe(false)
	})
})
