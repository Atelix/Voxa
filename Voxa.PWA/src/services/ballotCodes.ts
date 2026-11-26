import { BallotItemStatus, type BallotItemModel, type BallotSessionModel } from "@/logic/api"

/** A code on this device and what it may vote on. */
export interface CodeSession {
	code: string
	session: BallotSessionModel
}

/** Whether two sessions are for the same meeting, so their codes can be used together. */
export function sameMeeting(a: BallotSessionModel, b: BallotSessionModel): boolean {
	return a.meetingName === b.meetingName && a.startsAt === b.startsAt
}

/**
 * The meeting as one voter with several codes sees it. An item is open while any code can still vote on it, and
 * voted once every code has voted; otherwise all codes share the item's state.
 */
export function mergeSessions(sessions: CodeSession[]): BallotSessionModel | null {
	const first = sessions[0]?.session
	if (!first) return null
	return {
		...first,
		items: first.items.map((item) => {
			const statuses = sessions.map((s) => statusOf(s.session, item.id) ?? item.status)
			const notVoted = statuses.find((status) => status !== BallotItemStatus.Voted)
			const open = statuses.includes(BallotItemStatus.Open)
			return { ...item, status: open ? BallotItemStatus.Open : (notVoted ?? BallotItemStatus.Voted) }
		}),
	}
}

/** The codes that can still vote on an item. */
export function openCodes(sessions: CodeSession[], itemId: string): string[] {
	return sessions.filter((s) => statusOf(s.session, itemId) === BallotItemStatus.Open).map((s) => s.code)
}

function statusOf(session: BallotSessionModel, itemId: string): BallotItemModel["status"] | undefined {
	return session.items.find((i) => i.id === itemId)?.status
}
