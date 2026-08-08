# Brownfield Scenario: Box Office API

> **Stretch scenario.** This is the hardest scenario in the catalog, and it is
> deliberately harder than what a typical coding interview will ask of you.
> Finish one of the other scenarios first, and treat this one as growth, not as
> the bar.

You have joined the team behind a small box-office API for reserved-seat
events. Buyers browse events, look at the seat map, and book seats. The happy
path works and the code reads cleanly — but the backlog you are picking up is
the kind that only shows itself in production: bookings that collide, charges
that happen twice, writes that land halfway, and a sale that opened at the
wrong hour for half the world.

## Stack

.NET 10 · ASP.NET Core (controllers) · EF Core 10 · SQLite · xUnit

## Getting started

**Codespaces** — open this repository in a Codespace. Everything is installed.

**Locally** — install the [.NET 10 SDK](https://dotnet.microsoft.com/download),
then:

```bash
dotnet restore
dotnet run --project src/BoxOfficeApi
```

The database is created, migrated, and seeded on first run. Browse
<http://localhost:5000/swagger> for the API surface.

## Your work

- `docs/user-stories.md` — three stories to implement
- `docs/bug-reports.md` — two defects to fix
- `docs/domain.md` — how the domain works
- `docs/rubric.md` — how your work is reviewed beyond the tests

## Running the tests

```bash
dotnet test                       # everything
./scripts/check.sh                # per work item, matching the grader
```

On Windows: `./scripts/check.ps1`

The suite ships red for every work item and green for existing behaviour.
Turning the red ones green without turning any green one red is the assignment.

## Grading

<!-- grading-table:start -->
| Work item | Points |
| --- | --- |
| Existing behaviour keeps working | 20 |
| S1 idempotent booking | 20 |
| S2 atomic confirmation | 15 |
| S3 seat hold with expiry | 15 |
| B1 double-booking | 15 |
| B2 on-sale time is wrong across timezones | 15 |
<!-- grading-table:end -->

Every push runs the same per-item checks in GitHub Actions and writes a
scorecard to the run summary — open the **Grade** workflow run to see which work
items scored and which did not. A partial score is expected while you are
working; the run is not "broken" because items are still red.

Regression carries real weight — breaking working code costs more than leaving
a story unfinished.
