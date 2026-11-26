# Voxa: digital voting for a members' meeting

Voxa lets a club's members vote on the items of a general meeting: in advance from home, live in the hall with their
phone, or on paper. Votes are secret by design. One installation serves one club; the club's name, branding, mail
sender and statute rules are settings in the app, so the same code runs for every club.

## How a year goes

1. **Switch it on** a few weeks before the meeting (deploy, or start the server again).
2. **Settings** (`/bestuur/instellingen`): club name, language ([languages.md](languages.md)), colours, fonts, logo,
   mail sender, advance-voting term, the article of the statutes, how minors vote, and where the member list comes
   from. Import a preset such as
   [`clubs/sittard-condors.json`](../clubs/sittard-condors.json) with **Instellingen importeren**, then save.
3. **Meeting** (`/bestuur`): create it, or copy last year's (the date moves a year ahead and the year in the name goes
   up). Add the agenda items: proposals (for/against/abstain, absolute or two-thirds majority) and elections
   (candidates and seats; a blank option is added). Per item choose whether advance voting is allowed.
4. **Voters**: fetch the list from the member administration (Keeva) or upload a CSV. Suspended members drop out;
   minors are left out, vote themselves, or get their code through a parent, as the settings say.
5. **Codes**: send a test mail to yourself, then send the codes. Members without an email address get a printed code
   (**Papieren codes**). Voters who share an address, such as a parent of several youth members, get one mail with a
   code per member and one link that opens the voting page with all of those codes.
6. **Meeting night** (`/bestuur/vergadering/<id>/live`): open the hall screen (`/zaal/<id>`) on the projector; it shows
   the QR code to the voting page. Open an item, let people vote, close it. Only then the result appears; add paper
   votes on the result page and show it on the hall screen.
7. **Archive** the meeting: voting ends, codes and the voter list are wiped, results stay.
8. **Export** the results (**Instellingen → Uitslagen exporteren**), then **Alles wissen** (type `WISSEN`) and
   switch the server off until next year.

## Secrecy and privacy

- A code is 8 random characters (`XXXX-XXXX`, no look-alike characters). The database stores only its
  HMAC-SHA256 hash with the server's `Codes:Pepper`; the readable code exists only in memory while its mail is sent.
- Nothing links a code to a voter: voters are mailed in random order, and the voter record only notes that a code
  was sent.
- Every code is the vote of one member, also when one person holds several (a parent voting for their children). The
  voting page can hold several codes: a vote goes out once per selected code, all selected by default, and the voter
  can untick a code to vote differently for that member. The codes stay in the browser tab; the server gets each vote
  as a separate request and does not learn which codes were used together.
- A vote raises counters (ballots per item and channel, choices per option and channel) and marks the code as used
  for that item, in one transaction. There is no row per vote, no timestamp and no code next to a choice, so neither
  the board nor a database administrator can tell who voted what or in which order.
- While an item is open, the board and the hall screen see only the number of ballots. Results are visible after the
  item is closed.
- The code travels in the request body and in the URL fragment of the mail link (`#code=...`), which browsers never
  send to a server, so it does not end up in access logs.
- Wrong codes are rate limited: 5 per device and 60 per network address per 10 minutes (a whole hall can share one
  address). The board login allows 5 wrong passwords per address per 10 minutes.
- Archiving deletes the codes and the voter list. **Alles wissen** deletes everything except the settings.
- Mailgun keeps its own logs of sent mails (recipient, subject, delivery) for its retention period; open and click
  tracking is switched off. Mailgun can also keep the message itself, including the code, for a few days, so it
  sees which code went to which address. Limit access to the Mailgun account accordingly; without the database it
  still cannot see any votes.

## Server settings

Set these as environment variables (in `.env.api` next to `docker-compose-production.yml`, see
[`.env.api.example`](../.env.api.example)). Outside Development the API refuses to start without the required ones.

| Setting | Required | What it is |
| --- | --- | --- |
| `ConnectionString` | yes | PostgreSQL; set by the compose file. |
| `Admin__Password` | yes | The board's shared password. |
| `Codes__Pepper` | yes | Secret for hashing codes. Generate with `openssl rand -hex 32`; keep it for the life of a meeting. |
| `App__PublicUrl` | yes for mail | The app's public address, used for the link in the code mail. |
| `Mail__Mode` | no | `Outbox` (default, trial: nothing leaves the server) or `Mailgun`. |
| `Mail__Mailgun__Domain`, `Mail__Mailgun__ApiKey` | with Mailgun | Sending domain and API key. `Mail__Mailgun__BaseUrl` defaults to the EU region. |
| `Mail__RedirectTo` | no | Sends every mail to this address instead, for a dress rehearsal. |
| `Keeva__ApiKey` | with Keeva | Key with the scopes Members, MembersContacts and MembersPersonalData. |

The sender address in the club settings must belong to the Mailgun domain.

The API reads the client address from Cloudflare's `CF-Connecting-IP` header for rate limiting, so the server must
accept HTTP traffic only from Cloudflare (the standard setup in [deployment.md](deployment.md)); otherwise a client
could send its own header.

## Trial mode

With `Mail__Mode=Outbox` (the default, and always in Development) no mail is sent. The board gets a **Testmailbox**
with every mail the app would have sent, and **Proefvergadering maken** creates a meeting three days ahead with four
items, nine test members at `example.org` (two minors via the same parent, one without email, one suspended) and sends their
codes to the test mailbox. Open a mail there and use **Stemlink openen** to vote as that member.

Locally:

```bash
dotnet ef database update --project Voxa --startup-project Voxa.WebApi
dotnet run --project Voxa.WebApi          # board password in Development: bestuur
pnpm --dir Voxa.PWA dev                    # http://localhost:3000, board at /bestuur
```

## Member list

**Keeva**: set the Keeva address in the settings and `Keeva__ApiKey` on the server. Current members are fetched; a
parent's email comes from the member's linked parent account (the primary contact first). Keeva has no suspension
field, so remove suspended members by hand after the import.

**CSV**: a header row, separated by `;`, `,` or tab, quotes allowed. Columns (Dutch or English names):

| Column | Names accepted |
| --- | --- |
| Name | `naam`, or `voornaam` + `tussenvoegsel` + `achternaam` |
| Email | `email`, `e-mail`, `e-mailadres` |
| Birth date | `geboortedatum` (`2010-05-31`, `31-05-2010`, `31/05/2010`) |
| Status | `status` or `geschorst` (`geschorst`, `ja`, `1` mean suspended) |
| Parent | `naam ouder`, `e-mail ouder` |

The list can be replaced until the first code is sent; after that, add members one by one.

## Results

- **Proposal**: valid votes are for + against (abstentions do not count). Adopted with more than half, or at least two
  thirds where the item says so.
- **Election**: valid ballots are all ballots minus blank ones. A candidate needs more than half of the valid ballots;
  the seats go to the candidates with a majority and the most votes. A tie on the last seat elects none of the tied
  (**Staking van stemmen**); too few majorities means a new round (create a new item for it).
