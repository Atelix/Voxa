import { ref } from "vue"

/** The release this build belongs to, set by CI with VITE_APP_VERSION. */
export const APP_VERSION = import.meta.env.VITE_APP_VERSION?.trim() || "development"

/** The version the API last reported in its X-App-Version header. */
export const apiVersion = ref<string | null>(null)

export function recordApiVersion(version: string | null): void {
	if (version) apiVersion.value = version
}

/**
 * True while app and API run different releases, for example during a deploy: the workflows update the API first and
 * the Worker after it. Development builds never warn.
 */
export function versionsDiffer(app: string, api: string | null): boolean {
	if (!api || app === "development" || api === "development") return false
	return app !== api
}
