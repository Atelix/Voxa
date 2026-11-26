import { createRouter, createWebHistory, type RouteComponent } from "vue-router"
import { useBoardStore } from "@/store/BoardStore"

/** Views load on first visit, so the first page only downloads what it shows. */
type LazyView = () => Promise<{ default: RouteComponent }>

const view = (load: LazyView): LazyView => load

const router = createRouter({
	history: createWebHistory(import.meta.env.BASE_URL),
	scrollBehavior: () => ({ top: 0 }),
	routes: [
		{
			path: "/",
			component: view(() => import("@/views/voter/VoterLayout.vue")),
			children: [
				{ path: "", name: "start", component: view(() => import("@/views/voter/StartView.vue")) },
				{ path: "stempunten", name: "items", component: view(() => import("@/views/voter/ItemsView.vue")) },
				{
					path: "stempunt/:id(\\d+)",
					name: "vote",
					component: view(() => import("@/views/voter/VoteView.vue")),
					props: true,
				},
				{ path: "privacy", name: "privacy", component: view(() => import("@/views/voter/PrivacyView.vue")) },
			],
		},
		{
			path: "/zaal/:meetingId(\\d+)",
			name: "hall",
			component: view(() => import("@/views/HallView.vue")),
			props: true,
		},
		{
			path: "/bestuur/inloggen",
			name: "board-login",
			component: view(() => import("@/views/board/LoginView.vue")),
		},
		{
			path: "/bestuur",
			component: view(() => import("@/views/board/BoardLayout.vue")),
			meta: { board: true },
			children: [
				{ path: "", name: "meetings", component: view(() => import("@/views/board/MeetingsView.vue")) },
				{
					path: "vergadering/:id(\\d+)",
					name: "meeting",
					component: view(() => import("@/views/board/MeetingView.vue")),
					props: true,
				},
				{
					path: "vergadering/:id(\\d+)/live",
					name: "live",
					component: view(() => import("@/views/board/LiveView.vue")),
					props: true,
				},
				{
					path: "stempunt/:id(\\d+)/uitslag",
					name: "result",
					component: view(() => import("@/views/board/ResultView.vue")),
					props: true,
				},
				{ path: "instellingen", name: "settings", component: view(() => import("@/views/board/SettingsView.vue")) },
				{ path: "proefmailbox", name: "outbox", component: view(() => import("@/views/board/OutboxView.vue")) },
			],
		},
		{
			path: "/:pathMatch(.*)*",
			name: "not-found",
			component: view(() => import("@/views/NotFoundView.vue")),
		},
	],
})

// Board pages need a signed-in board; the API checks too, this only saves a round of errors.
router.beforeEach(async (to) => {
	if (!to.matched.some((record) => record.meta.board)) return true
	const board = useBoardStore()
	if (board.signedIn || (await board.check())) return true
	return { name: "board-login", query: { terug: to.fullPath } }
})

export default router
