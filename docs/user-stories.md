# User Stories

## S1 — Idempotent booking (20 points)

**As a** buyer on a flaky connection
**I want** my retried booking request to be recognised as the same purchase
**So that** I am never charged twice for one seat.

Support keeps refunding double charges: a buyer's client times out, retries,
and the retry books and charges again. Accept an `Idempotency-Key` header on
`POST /api/bookings` so a retried request is the same request.

**Acceptance criteria**

- `POST /api/bookings` accepts an optional `Idempotency-Key` header (any
  non-empty string chosen by the client).
- Replaying a request with the same key returns the **original** booking — same
  booking id, a success status — rather than creating a second booking or a
  second charge. Exactly one booking, one payment, and one charge exist no
  matter how many times the request is replayed.
- A replay still returns the original success even when a fresh request for
  that seat would be rejected — the original outcome is the answer, always.
- Requests with **different** keys are independent requests.
- Requests with **no** header behave exactly as they do today.

**Out of scope:** key expiry, persisting keys across database resets, detecting
a replayed key used with a *different* seat or buyer.

**Tests:** `dotnet test --filter Category=S1`

---

## S2 — Atomic confirmation (15 points)

**As the** venue's finance team
**I want** a confirmed booking to be all-or-nothing
**So that** the books always balance.

After a deploy restarted the service mid-request, finance found bookings with
no payment record and gaps in the audit table. Confirming a booking writes
three rows — booking, payment record, audit entry — and today they do not stand
or fall together.

**Acceptance criteria**

- When the charge is declined, the response is `402` and the database gains
  **zero** new booking, payment, or audit rows — and the seat remains available
  to the next buyer.
- When the charge succeeds, all three rows land, one of each.
- No half-states: there is never a booking without its payment and audit entry.

**Out of scope:** retrying declined charges, refunds, compensating a charge
that succeeded when a later step failed.

**Tests:** `dotnet test --filter Category=S2`

---

## S3 — Seat hold with expiry (15 points)

**As a** buyer typing card details
**I want** a short hold on my chosen seat
**So that** someone faster does not take it out from under me.

**Acceptance criteria**

- `POST /api/events/{eventId}/seats/{seatId}/holds` creates a hold. The request
  body carries a `holderKey` — the buyer's email, the same value they will book
  with. The response is `201` with the seat id, the holder key, and
  `expiresAtUtc`.
- The hold lasts a fixed window read from configuration
  (`Holds:DurationMinutes`; the shipped default is 5). Not a hardcoded 5.
- While a hold is active, only the holder can book the seat — anyone else gets
  a `409`. Booking as the holder means booking with a `buyerEmail` equal to the
  hold's `holderKey`.
- Once the hold expires, the seat behaves as free — including being holdable
  and bookable by others. Expiry is evaluated when the seat is looked at; there
  is no background job.
- Holding a seat that is already held, or already booked, is a `409`.

**Out of scope:** extending or releasing holds, holding multiple seats at once,
showing holds on the seat map.

**Tests:** `dotnet test --filter Category=S3`
