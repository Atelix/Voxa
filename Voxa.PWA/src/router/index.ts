import { createRouter, createWebHistory, type RouteComponent } from "vue-router"

/** Views load on first visit, so the first page only downloads what it shows. */
type LazyView = () => Promise<{ default: RouteComponent }>

const ItemsView: LazyView = () => import("@/views/ItemsView.vue")
const NotFoundView: LazyView = () => import("@/views/NotFoundView.vue")

const router = createRouter({
	history: createWebHistory(import.meta.env.BASE_URL),
	routes: [
		{
			path: "/",
			name: "items",
			component: ItemsView,
		},
		{
			path: "/:pathMatch(.*)*",
			name: "not-found",
			component: NotFoundView,
		},
	],
})

export default router
