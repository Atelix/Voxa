---
name: material-design
description: Use when designing, building or reviewing the visual UI of Voxa.PWA. Owns Vuetify components, branding, layout, accessibility and Material Design 3 audits; detailed MD3 principles live in its reference files. Reactive state and application logic belong to ecc-vue-patterns.
---

# Material Design in the project

The front end is a Vue 3 app built on **Vuetify 4**, which implements
Material Design 3 (its type scale, elevation levels and grid). Good Material UI here means using Vuetify the way it is meant
to be used, not adding another design library on top.

- `Voxa.PWA`: the app. Its theme (light and dark) is in `src/plugins/vuetify.ts`,
  together with the component defaults (corner radius, field variants). Change the
  brand there, not per component.
- Accessibility fixes that apply everywhere live in `src/styles/main.css`.

Before building, read the screen you are changing and one or two neighbouring
screens, and match what they do. Consistency with the rest of the app beats a
"more correct" pattern used in one place.

## Hard rules

1. **Vuetify only.** Do not install `@material/web`, Material UI, Tailwind or
   another component or CSS framework. Do not hand-roll a component Vuetify
   already has (buttons, chips, dialogs, menus, lists, tabs, snackbars,
   data tables, steppers, date pickers).
2. **Theme colours, never hex.** Use `color="primary"`, `bg-surface`,
   `text-medium-emphasis`, `rgb(var(--v-theme-on-surface))`. A hard-coded
   colour in a component breaks theming and dark mode. New semantic
   colours go into the theme in `plugins/vuetify.ts`, in both light and dark.
3. **Utility classes, not inline styles.** Spacing (`pa-4`, `mb-2`, `ga-2`),
   typography (Material 3 names: `text-title-large`, `text-body-medium`,
   `text-body-small`; the Material 2 names such as `text-h6` or `text-caption`
   no longer exist), flex (`d-flex align-center`) and display
   (`d-none d-md-flex`). Spacing follows the Material 4/8 px grid; use the scale
   steps, not arbitrary pixel values. Component `<style>` blocks go in
   `@layer voxa { }`, which ranks above Vuetify's components and below its
   utility classes (each app's `index.html`), so a utility class always wins.
4. **Icons are MDI** (`mdi-*` from `@mdi/font`). Pick the outline variant when
   the surrounding screen uses outlines.
5. **Loading states follow `AGENTS.md`**: `v-skeleton-loader` shaped like the
   content, never a spinner as a placeholder, never an empty state before the
   first response.
6. **All text goes through i18n** (`$t(...)`, keys in `src/languages/en.ts`
   and `nl.ts`), including `aria-label`s and tooltips.

## Colour use

- Never combine `bg-primary` with your own text colour: let Vuetify pick the
  `on-primary` colour by using the `color` prop.
- Keep large areas on `surface` / `background`; use `primary` for emphasis
  (the main action, the selected item, an active tab), not for whole pages.
- Status colours (`error`, `warning`, `success`, `info`) mean status only, never
  decoration or branding.
- Check new screens in light and dark theme.

## Picking components

| Need | Use | Notes |
|------|-----|-------|
| The main action on a screen | `v-btn color="primary"` (flat/elevated) | One per view. |
| Secondary actions | `variant="tonal"` or `"outlined"` | |
| Low-emphasis or dialog actions | `variant="text"` | Dialog actions sit right-aligned in `v-card-actions`, cancel before confirm. |
| Icon-only action | `v-btn icon` with an `aria-label` | Add `v-tooltip` when the meaning isn't obvious. |
| Group of related content | `v-card` with `v-card-item`/`-title`/`-text`/`-actions` | Don't nest cards inside cards. |
| Collections of rows | `v-list` with `v-list-item` (`prepend-avatar`, `title`, `subtitle`) | Prefer props over hand-built slots. |
| Tabular data | `v-data-table` / `v-data-table-server` | Keep `:loading` and a `#loading` skeleton slot. |
| Filters and tags | `v-chip` / `v-chip-group` | Not for actions; use buttons for those. |
| Short feedback after an action | the existing alerts container / `v-snackbar` | Don't use `v-alert` for transient feedback. |
| Persistent inline message | `v-alert` with the right `type` | |
| Focused task or form | `v-dialog` with a `v-card` | `:fullscreen="$vuetify.display.mobile"` for forms, as the existing dialogs do. |
| Overflow actions | `v-menu` on an icon button (`mdi-dots-vertical`) | |
| Switching views on one page | `v-tabs` + `v-window` | |
| Long forms | `v-form` with `v-text-field`/`v-select`/`v-autocomplete`, no `variant` (the default comes from `plugins/vuetify.ts`) | Validation via `:rules`, errors in the field, not alerts. |

Reuse the project's own components under `src/components` when they fit, instead
of building a near-copy.

## Layout and responsiveness

- Design mobile first; Voxa.PWA is installed on phones. Use `v-container`,
  `v-row` (spaced with `gap`; `density="compact"` for forms), `v-col cols="12" md="6"`
  and Vuetify 4's breakpoints (md 840 px, lg 1145 px)
  (`useDisplay()` / `$vuetify.display.smAndDown`) rather than media queries.
- Touch targets are at least 48 px for primary controls and never below
  24 px.
- Keep a clear hierarchy: one page title (`text-headline-small`/`text-title-large`), section titles,
  then body text. Don't skip levels for visual size; change the class instead.
- Readable line length: constrain text-heavy content (`max-width` on the card
  or column) instead of letting it run across a desktop screen.
- Group with whitespace and `v-divider` before reaching for borders, shadows or
  extra cards. Use elevation sparingly and consistently with nearby screens.

## Accessibility (WCAG 2.2 AA)

- Text contrast at least 4.5:1, large text and icons 3:1. Vuetify's default
  medium-emphasis opacity is too low on white; raise it once in
  `src/styles/main.css` rather than with per-component fixes.
- Every icon-only control has an `aria-label`; decorative icons stay hidden
  from assistive technology.
- Don't rely on colour alone for state: pair it with an icon or text.
- Keep keyboard focus visible and in a logical order; dialogs must return
  focus to the control that opened them (Vuetify does this when you use the
  activator slot).

## Motion

Use Vuetify's built-in transitions (`dialog-bottom-transition`,
`v-expand-transition`, `v-fade-transition`). Keep motion short and purposeful,
and don't add animations to content that changes often.

## Reviewing a screen

When asked to review or polish UI, check in this order and fix what you find:

1. Hard-coded colours, fonts or pixel spacing that bypass the theme.
2. More than one primary action, or a primary action that isn't the main task.
3. Missing loading, empty and error states (per `AGENTS.md`).
4. Mobile layout at 360 px wide: overflow, cramped actions, dialogs that
   should be fullscreen.
5. Contrast, focus, labels and touch target sizes.
6. Light and dark theme.
7. Inconsistency with neighbouring screens (button variants, field variants,
   spacing, list styles).

When you can run the app, look at the change in a browser at phone and
desktop width before calling it done; type-check, lint and build as
`AGENTS.md` requires.

## References

- The [Material Design 3 reference](references/material-3/GUIDE.md) for Material Design 3
  principles in depth: color roles, type scale, shape, motion, navigation choice
  and a compliance audit. Its `@material/web` and Compose code does not apply here.
- Material Design 3 guidelines: https://m3.material.io
- Vuetify 4 components and utilities: https://vuetifyjs.com/en/components/all/
- Vuetify theme configuration: https://vuetifyjs.com/en/features/theme/
