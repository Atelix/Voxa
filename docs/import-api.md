# Member import API

A member administration, or an automation such as n8n, sends the club's member list to Voxa. Voxa applies the club's
rules itself (suspension, minors, duplicates), so send every current member and let Voxa decide who votes.

The OpenAPI document is served by every installation at **`https://<host>/api/import/openapi.json`**. In Development,
Swagger UI (`/swagger`) shows it as **Voxa member import**.

## Set it up

1. Generate a key: `openssl rand -hex 32`.
2. Set it on the server as `Import__ApiKey` in `.env.api` (see [`.env.api.example`](../.env.api.example)) and restart
   the API. Keys shorter than 32 characters are refused at start-up. One installation serves one club, so this is the
   club's key; change it by setting a new one.
3. Store the same key in the sending system, for example as an n8n **Header Auth** credential with name
   `Authorization` and value `Bearer <key>`.

The board's settings page shows whether the import API is on. Without a key every request gets `401`.

## Authentication

Every request sends `Authorization: Bearer <key>`. The key only reaches `/api/import`; it cannot open the board's
pages, and the board's sign-in does not reach the import API. After 10 wrong keys from one address in 10 minutes that
address gets `429` with `Retry-After`.

## Endpoints

### `GET /api/import/meetings`

The meetings that can receive a member list: every meeting that is not archived, soonest first.

```json
[
  {
    "id": 3,
    "name": "ALV 2027",
    "startsAt": "2027-02-02T17:45:00Z",
    "status": "Draft",
    "voterCount": 0,
    "importMode": "Replaced"
  }
]
```

`importMode` says what a push does now: `Replaced` until the first code is sent, `Added` after that.

### `POST /api/import/meetings/{meetingId}/members`

Sends the members. `name` is required; everything else is optional.

| Field | Type | Meaning |
| --- | --- | --- |
| `name` | string, 1–200 | Full name as the voter list and code mail show it, for example `Jan de Vries`. |
| `email` | string or null | The member's own address. Members of age without one need a paper code. |
| `birthDate` | `yyyy-MM-dd` or null | Tells minors apart, using the adult age in the club settings and the meeting date. Without it the member counts as of age. |
| `suspended` | boolean, default `false` | A suspended member has no vote and is skipped. |
| `guardians` | array or null, max 10 | Parents or guardians (`name`, `email`), primary contact first. |

At most 10,000 members per request.

```bash
curl -X POST "https://stem.example.org/api/import/meetings/3/members" \
  -H "Authorization: Bearer $VOXA_IMPORT_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "members": [
      { "name": "Anna de Vries", "email": "anna@example.org", "birthDate": "1990-01-01" },
      { "name": "Kees Jansen", "birthDate": "2014-03-03",
        "guardians": [{ "name": "Petra Jansen", "email": "petra@example.org" }] },
      { "name": "Gerda Smit", "email": "gerda@example.org", "suspended": true }
    ]
  }'
```

```json
{
  "mode": "Replaced",
  "imported": 2,
  "alreadyListed": 0,
  "viaGuardian": 1,
  "withoutEmail": 0,
  "skipped": ["Gerda Smit: geschorst"]
}
```

### What Voxa does with the list

- **Before the first code is sent** the list replaces the meeting's voter list (`mode: Replaced`), also members the
  board added by hand. A list in which nobody may vote is refused with `400`, so an empty export cannot wipe the list.
- **After the first code is sent** the list never replaces anything (`mode: Added`): members who are not on the list
  yet (by name, ignoring case) are added, without a code. Matching on the name alone means a member whose address
  changed after the codes went out does not get a second code; a second member with exactly the same name must be
  added by hand. Nobody is removed, so a voter who already has a code keeps it. `alreadyListed` counts the members that were already there. The board presses **Codes versturen** to
  mail the new voters.
- **Suspended** members are skipped (`geschorst`).
- **Minors** (younger than the adult age on the meeting day) follow the club setting: no vote (skipped,
  `minderjarig`), their own code, or a code for the parent or guardian. Then the code goes to the first guardian with
  an address and the mail greets them by `name`, or "ouder of verzorger" without one.
- **Duplicates** in one list (same name and address) are skipped (`dubbel`).
- Voters who share an address, such as a parent of several youth members, get one mail with all their codes.

### Errors

Errors are problem responses (`application/problem+json`) with a `detail`:

| Status | When |
| --- | --- |
| `400` | The body is invalid, or nobody in the list may vote. |
| `401` | The key is missing or wrong, or the server has no key. |
| `404` | The meeting does not exist. |
| `409` | The meeting is archived. |
| `429` | Too many wrong keys from this address. |

## n8n recipe

1. **Schedule Trigger**, for example daily during the weeks before the meeting, or a **Manual Trigger**.
2. Fetch the members from the member administration (its own node or an **HTTP Request**).
3. **HTTP Request** `GET https://<host>/api/import/meetings` with the Header Auth credential, and pick the meeting, for
   example the first one (`{{ $json[0].id }}`) or the one whose `name` you expect.
4. **Code** node that maps the members to `{ "members": [...] }` with the fields above. For example, from a member
   administration that splits names and marks parent accounts:

   ```js
   const members = $input.all().map(({ json: m }) => ({
     name: [m.firstname, m.infix, m.lastname].filter(Boolean).join(" "),
     email: m.email ?? null,
     birthDate: m.birthDate ?? null,
     suspended: Boolean(m.suspended),
     guardians: (m.accounts ?? [])
       .filter((a) => a.isParent)
       .sort((a, b) => Number(b.isPrimaryContact) - Number(a.isPrimaryContact))
       .map((a) => ({ name: a.name ?? null, email: a.email ?? null })),
   }))
   return [{ json: { members } }]
   ```

5. **HTTP Request** `POST https://<host>/api/import/meetings/{{ id }}/members`, body JSON `{{ $json }}`, with the
   same credential. Its response shows how many voters were read and who was skipped.
