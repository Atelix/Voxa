/** The API's base address. Empty in production builds: the API is reached on the app's own origin under /api. */
export const API_URL = (import.meta.env.VITE_API_URL ?? "").trim().replace(/\/+$/, "")
