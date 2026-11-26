import { onBeforeUnmount, onMounted } from "vue"

/** Runs `action` now and every `intervalMs` while the component is mounted and the page is visible. */
export function usePolling(action: () => Promise<void>, intervalMs: number): void {
	let timer: ReturnType<typeof setInterval> | undefined
	let running = false

	async function tick(): Promise<void> {
		if (running || document.visibilityState === "hidden") return
		running = true
		try {
			await action()
		} finally {
			running = false
		}
	}

	onMounted(() => {
		void tick()
		timer = setInterval(() => void tick(), intervalMs)
	})
	onBeforeUnmount(() => clearInterval(timer))
}
