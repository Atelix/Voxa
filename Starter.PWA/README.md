# Starter.PWA

The front end: Vue 3, Vuetify 4, Pinia, vue-router and vue-i18n (English and Dutch), built with Vite and served by a
Cloudflare Worker (`src/worker.ts`) that adds the security headers and cache rules to every response.

```bash
pnpm install
pnpm dev            # http://localhost:3000, talks to the API on http://localhost:5143 (.env.development)
pnpm run type-check
pnpm run lint
pnpm run test
pnpm run build
```

- `src/logic/api.ts` is generated from the API's Swagger document: `pnpm run generate-api-schema` while the API runs.
  Never edit it by hand.
- Load view data with `useLoader` and run saves with `useAsyncAction` (`src/services`); see AGENTS.md for the
  loading-state rules.
- `VITE_*` variables are baked in at build time; the workflows set `VITE_APP_VERSION` so the app can warn while it
  runs against a different API release.
