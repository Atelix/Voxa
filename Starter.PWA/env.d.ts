/// <reference types="vite/client" />
interface ImportMetaEnv {
	/** Where the API lives when it is not on the app's own origin, such as http://localhost:5143 in development. */
	readonly VITE_API_URL?: string
	/** The release this build belongs to; the API reports its own, and the app warns while they differ. */
	readonly VITE_APP_VERSION?: string
}

interface ImportMeta {
	readonly env: ImportMetaEnv
}
