/** The static-assets binding Cloudflare gives the Worker (see wrangler.jsonc). */
interface AssetsBinding {
	fetch(request: Request): Promise<Response>
}

export interface Env {
	ASSETS: AssetsBinding
}

/** Entry points must never be cached: a new release has to be picked up on the next load. */
const UNCACHED_PATHS = new Set(["/", "/index.html", "/service-worker.js"])

/**
 * Origin of the API when a build sets VITE_API_URL to another host. Production and beta builds leave it empty and
 * reach the API on the app's own origin (the reverse proxy sends /api to it), which 'self' already covers.
 */
function apiOrigin(): string {
	const configured = import.meta.env.VITE_API_URL?.trim()
	if (!configured) return ""
	try {
		return new URL(configured).origin
	} catch {
		return ""
	}
}

/**
 * Content-Security-Policy and related headers for the app. Script only runs from the app's own files, so
 * injected markup cannot load or run code, and the API is the only other host the app may talk to.
 */
// pathname lets a rule differ per page, for example a page other sites may frame.
export function securityHeaders(_pathname: string, api = apiOrigin()): Record<string, string> {
	const self = api ? `'self' ${api}` : "'self'"
	const headers: Record<string, string> = {
		"Content-Security-Policy": [
			"default-src 'self'",
			"script-src 'self'",
			// Vuetify writes its theme into a style element at runtime. The fonts are bundled with the app.
			"style-src 'self' 'unsafe-inline'",
			"font-src 'self' data:",
			// Images and files may come from the API; uploads are previewed from blob: URLs.
			`img-src ${self} data: blob:`,
			`media-src ${self} blob:`,
			`connect-src ${self}`,
			`frame-src ${self}`,
			`manifest-src ${self}`,
			"worker-src 'self'",
			"frame-ancestors 'self'",
			"base-uri 'self'",
			"form-action 'self'",
			"object-src 'none'",
		].join("; "),
		"X-Content-Type-Options": "nosniff",
		"Referrer-Policy": "strict-origin-when-cross-origin",
		// No camera, microphone, location, payment or USB APIs. Loosen this when a feature needs one of them.
		"Permissions-Policy": "camera=(), microphone=(), geolocation=(), payment=(), usb=()",
		"Strict-Transport-Security": "max-age=31536000; includeSubDomains",
		"X-Frame-Options": "SAMEORIGIN",
	}
	return headers
}

/**
 * The service worker's own response decides the policy it runs under. It imports no scripts, so it may only load
 * scripts from the app itself.
 */
export function serviceWorkerHeaders(): Record<string, string> {
	return {
		"Content-Security-Policy": ["default-src 'self'", "script-src 'self'", "object-src 'none'"].join("; "),
		"X-Content-Type-Options": "nosniff",
	}
}

/**
 * Browser caching. Vite puts the build output under /assets/ with a
 * content hash in each file name, so those files never change and the browser need not ask for them again. Pages
 * (including the single-page fallback) and the service worker are revalidated on every load so a new release is
 * picked up at once; the other public files (icons, the manifest) may be kept for an hour.
 */
export function cacheControl(pathname: string, contentType: string | null): string {
	if (pathname.startsWith("/assets/")) return "public, max-age=31536000, immutable"
	if (UNCACHED_PATHS.has(pathname) || contentType?.startsWith("text/html")) return "no-cache"
	return "public, max-age=3600"
}

export default {
	async fetch(request: Request, env: Env): Promise<Response> {
		const url = new URL(request.url)

		// The reverse proxy sends /api to the API before it reaches the app, so an API request that gets here has no
		// endpoint. It answers 404 like the API does, instead of a page or a 200 the app would mistake for data.
		if (url.pathname.startsWith("/api/")) {
			return new Response(
				JSON.stringify({ title: "Resource not found", status: 404, detail: "This address is not an API endpoint." }),
				{
					status: 404,
					headers: {
						"Content-Type": "application/problem+json",
						"Cache-Control": "no-store",
						"X-Content-Type-Options": "nosniff",
					},
				}
			)
		}

		const asset = await env.ASSETS.fetch(request)
		const response = new Response(asset.body, asset)
		const headers = url.pathname === "/service-worker.js" ? serviceWorkerHeaders() : securityHeaders(url.pathname)
		for (const [name, value] of Object.entries(headers)) response.headers.set(name, value)
		response.headers.set("Cache-Control", cacheControl(url.pathname, response.headers.get("Content-Type")))
		return response
	},
}
