import { Workbox } from "workbox-window"

// Development serves the app from Vite; a service worker would keep serving an older build there.
if ("serviceWorker" in navigator && import.meta.env.PROD) {
	window.addEventListener("load", () => void registerServiceWorker())
}

async function registerServiceWorker(): Promise<void> {
	const serviceWorkerUrl = new URL("/service-worker.js", window.location.origin)
	const wb = new Workbox(serviceWorkerUrl.toString())

	let refreshing = false

	// When a new SW is waiting, tell it to activate immediately
	wb.addEventListener("waiting", () => {
		void wb.messageSW({ type: "SKIP_WAITING" })
	})

	// Reload once when the new SW takes control
	wb.addEventListener("controlling", () => {
		if (refreshing) return
		refreshing = true
		window.location.reload()
	})

	try {
		const registration = await wb.register()

		// Force update check on load
		if (registration) {
			await registration.update()
		}
	} catch (err) {
		console.error("Service Worker registration failed:", err)
	}
}
