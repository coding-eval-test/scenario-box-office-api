# Bug Reports

## B1 — Two customers, one seat, both charged (15 points)

**Reported by:** Venue box-office manager
**Severity:** Critical — it has reached chargebacks twice this month.

**What happened**

Twice this month, two customers arrived at the venue holding confirmations for
the same seat. Both had been charged. Support refunded one of each pair and
moved the affected customers by hand.

**What support found**

- In both incidents, the two bookings were created **within the same second**,
  during the first minutes after a popular event opened.
- Both confirmations are real: both bookings exist, each with its own payment
  record.
- Nobody can reproduce it by hand after the fact — booking an already-booked
  seat politely returns a conflict, every time we try.

**Notes from the reporter:** "It only seems to happen when two people go for
the same seat at almost the same moment. The rest of the time the system
correctly says the seat is taken. We cannot sell a seat twice — reconciliation
flags it, and the second customer is furious."

**Expected:** when two buyers go for the same seat, exactly one booking and one
charge exist afterwards, and the other buyer gets the same conflict response a
late booking gets today.

**Tests:** `dotnet test --filter Category=B1`

---

## B2 — Sale opens at the wrong moment for some regions (15 points)

**Reported by:** Marketing, after the Winter Gala pre-announcement
**Severity:** High — the announced on-sale time was wrong for most of the world.

**What happened**

The Winter Gala on-sale was announced globally for a single moment. Buyers in
some regions could book **hours before** that moment; buyers in other regions
were still being rejected **hours after** it.

**What support found**

- The early and late offsets are consistent per region and match each buyer's
  distance from UTC — one community got in nine hours early, another was locked
  out for six hours past the announcement.
- For buyers in the UK (in winter) the gate behaved exactly as announced.
- Once a sale has been open for a day or so, nobody can reproduce anything —
  the gate only misbehaves near the boundary.

**Expected:** the on-sale instant is one moment worldwide. A booking attempted
before it is rejected and one attempted after it is accepted, no matter where
the buyer — or the server — is.

**Tests:** `dotnet test --filter Category=B2`
