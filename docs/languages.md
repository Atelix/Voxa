# Languages

Voxa speaks Dutch (`nl`) and English (`en`). Every screen, the hall screen, the code mails, the API's error messages
and the trial data come in both.

## Which language people see

- **In the app**, each device chooses with the language button (the translate icon) in the top bar, or on the board's
  sign-in page. The choice is kept on that device. Without a choice the app follows the device's language, and if
  Voxa does not have that language, the club's language. Dutch is the last fallback.
- **API messages** follow the app: the PWA sends its language as `Accept-Language`, and the API writes errors in it.
  A request without a language Voxa knows gets Dutch.
- **Mails** go out in the meeting's language, set in the meeting dialog. A meeting without one uses the club's
  language from **Instellingen → Vereniging → Taal**. The test mail uses the same language.
- **Trial data** (**Proefvergadering maken**) is written in the board's current language.

The fixed options of a proposal (for, against, abstain) and the blank choice of an election are shown in the reader's
language. Agenda titles, descriptions and candidate names are what the board typed, so they stay as typed.

Web addresses are English in every language (`/admin`, `/items`, `/hall/<id>`). The link in the code mail only uses
`/#code=...`, so it does not depend on them.

## Adding a language

Use the two-letter ISO 639-1 code, for example `de`.

1. **PWA**: copy `Voxa.PWA/src/languages/en.ts` to `de.ts` and translate it. Register it in
   `Voxa.PWA/src/plugins/i18n.ts` with its own name (`Deutsch`), its date locale (`de-DE`) and Vuetify's texts
   (`import { de } from "vuetify/locale"`). `i18n.test.ts` fails while a key is missing.
2. **API**: copy `Voxa/Resources/Texts.resx` to `Texts.de.resx` and translate every `<value>`, then add `"de"` to
   `Languages.Supported` in `Voxa.Abstractions/Languages.cs`. `LanguageTests` fails while a text is missing.
3. Run the full validation suite from `AGENTS.md`.

No migration is needed: the club's and meeting's languages are stored as codes.
