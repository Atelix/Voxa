import { describe, expect, it } from "vitest"
import serviceWorkerSource from "../public/service-worker.js?raw"
import worker, { cacheControl, securityHeaders, type Env } from "./worker"

function envServing(body: string, contentType: string): Env {
	return { ASSETS: { fetch: () => Promise.resolve(new Response(body, { headers: { "Content-Type": contentType } })) } }
}

function policy(response: Response): string[] {
	return (response.headers.get("Content-Security-Policy") ?? "").split("; ")
}

describe("Cloudflare Worker", () => {
	it("answers an API address that reached the app with a 404 problem", async () => {
		const response = await worker.fetch(
			new Request("https://app.example.com/api/does-not-exist"),
			envServing("<html></html>", "text/html")
		)

		expect(response.status).toBe(404)
		expect(response.headers.get("Content-Type")).toBe("application/problem+json")
		expect(await response.json()).toMatchObject({ status: 404, title: "Resource not found" })
	})

	it("serves app pages with a policy that only runs the app's own scripts", async () => {
		const response = await worker.fetch(
			new Request("https://app.example.com/items"),
			envServing("<html></html>", "text/html")
		)

		expect(await response.text()).toBe("<html></html>")
		expect(policy(response)).toContain("script-src 'self'")
		expect(policy(response)).toContain("connect-src 'self'")
		expect(policy(response)).toContain("object-src 'none'")
		expect(policy(response)).toContain("frame-ancestors 'self'")
		expect(response.headers.get("X-Frame-Options")).toBe("SAMEORIGIN")
		expect(response.headers.get("X-Content-Type-Options")).toBe("nosniff")
		expect(response.headers.get("Strict-Transport-Security")).toBe("max-age=31536000; includeSubDomains")
	})

	it("lets browsers keep hashed build files and revalidate pages and the service worker", async () => {
		const script = await worker.fetch(
			new Request("https://app.example.com/assets/index-Bx1c2d.js"),
			envServing("", "text/javascript")
		)
		expect(script.headers.get("Cache-Control")).toBe("public, max-age=31536000, immutable")

		const page = await worker.fetch(new Request("https://app.example.com/items/12"), envServing("", "text/html"))
		expect(page.headers.get("Cache-Control")).toBe("no-cache")

		expect(cacheControl("/", null)).toBe("no-cache")
		expect(cacheControl("/service-worker.js", "text/javascript")).toBe("no-cache")
		expect(cacheControl("/favicon.svg", "image/svg+xml")).toBe("public, max-age=3600")
	})

	it("allows the API host when the API is not on the app's origin", () => {
		const csp = securityHeaders("/", "https://api.example.com")["Content-Security-Policy"].split("; ")

		expect(csp).toContain("connect-src 'self' https://api.example.com")
		expect(csp).toContain("img-src 'self' https://api.example.com data: blob:")
	})

	it("only lets the service worker load scripts from the app itself", async () => {
		const response = await worker.fetch(
			new Request("https://app.example.com/service-worker.js"),
			envServing("", "text/javascript")
		)

		expect(policy(response)).toContain("script-src 'self'")
	})

	it("ships a service worker that imports no scripts from other hosts", () => {
		expect(serviceWorkerSource).not.toMatch(/importScripts\s*\(\s*["'`]https?:/)
	})
})
