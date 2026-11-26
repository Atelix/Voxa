// public/service-worker.js
// Only takes care of updates: a new release activates at once and the app reloads (src/registerServiceWorker.ts).
// It caches nothing and imports no scripts, so its policy (serviceWorkerHeaders in src/worker.ts) only allows the
// app's own origin. Add offline caching or push messages here when the app needs them.

self.addEventListener("message", (event) => {
	if (event.data?.type === "SKIP_WAITING") {
		self.skipWaiting()
	}
})

self.addEventListener("activate", (event) => {
	event.waitUntil(self.clients.claim())
})
