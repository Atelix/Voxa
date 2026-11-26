import { describe, expect, it } from "vitest"
import { useLoader } from "./useLoader"

function deferred<T>(): { promise: Promise<T>; resolve: (value: T) => void } {
	let resolve!: (value: T) => void
	const promise = new Promise<T>((r) => (resolve = r))
	return { promise, resolve }
}

describe("useLoader", () => {
	it("starts loading so views show a skeleton before the first response", () => {
		const loader = useLoader(() => Promise.resolve(1))

		expect(loader.loading.value).toBe(true)
		expect(loader.data.value).toBeNull()
	})

	it("keeps the newest response when an older one arrives later", async () => {
		const first = deferred<string>()
		const second = deferred<string>()
		const responses = [first.promise, second.promise]
		const loader = useLoader(() => responses.shift()!)

		const firstLoad = loader.load()
		const secondLoad = loader.load()
		second.resolve("new")
		await secondLoad
		first.resolve("old")
		await firstLoad

		expect(loader.data.value).toBe("new")
		expect(loader.loading.value).toBe(false)
	})

	it("turns a failure into an ApiError", async () => {
		const loader = useLoader(() => Promise.reject(new TypeError("Failed to fetch")))

		await loader.load()

		expect(loader.error.value?.kind).toBe("offline")
		expect(loader.loading.value).toBe(false)
	})
})
