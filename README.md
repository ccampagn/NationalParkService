# Trailhead — National Park Campsite Reservations

Trailhead lets people browse all 63 U.S. national parks, see which parks have campsites free for their dates, and reserve
an individual site. It has two parts:

| Folder | What it is |
| --- | --- |
| `NationalParkServiceAPI` | ASP.NET Core Web API (.NET 10, EF Core, SQL Server) |
| `NationalParkServiceAPI/NationalParkServiceAPI.Tests` | xUnit tests, run against EF Core's in-memory database |
| `NationalParkServiceAPI/Sql` | Scripts that create and seed the database |
| `NationalParkServiceReact` | React 19 + Vite front end |

## Features

- **Search parks** by name, region and activity. Only parks with at least one free, in-season site for the searched
  dates are shown, and each card shows the real number of sites left.
- **Book a site.** Choose a campground and dates, then pick a free site. Stays are 1–14 nights, a site's guest limit is
  enforced, and two people can't book the same site for the same night.
- **Campground seasons.** Seasonal campgrounds can't be booked when they're closed. Seasons can run across New Year
  (e.g. a winter desert campground open Oct 15 – Apr 15).
- **Accounts.** Sign up and log in to book and to see or cancel your trips.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20.19+ or 22.12+ (required by Vite 8)
- SQL Server — SQL Server Express (`.\SQLEXPRESS`) is what the default connection string uses
- `sqlcmd` for running the database scripts

## Database setup

Create an empty database named `NationalParkService`, then run the scripts in `NationalParkServiceAPI/Sql` in this
order. Each one is safe to run again.

1. `CreateParkTable.sql`
2. `CreateCampgroundTable.sql`
3. `AddCampgroundSeason.sql`
4. `CreateSiteTable.sql` — creates 10 sites for every existing campground, so run it after the campgrounds are loaded
5. `CreateUserTable.sql`
6. `CreateReservationTable.sql`
7. `AddReservationUserId.sql`
8. `AddReservationSiteId.sql`

From `NationalParkServiceAPI/Sql` in PowerShell:

```powershell
'CreateParkTable','CreateCampgroundTable','AddCampgroundSeason','CreateSiteTable','CreateUserTable','CreateReservationTable','AddReservationUserId','AddReservationSiteId' |
  ForEach-Object { sqlcmd -S .\SQLEXPRESS -d NationalParkService -E -C -f 65001 -b -i "$_.sql"; if ($LASTEXITCODE) { throw "$_.sql failed" } }
```

`-f 65001` reads the files as UTF-8 so accented names (e.g. Piñon Flats) come through correctly, and `-b` stops on the
first error.

> The `PassType` and `Pass` tables used by `api/passtypes` and `api/passes` don't have scripts here yet.

The campground season dates in `AddCampgroundSeason.sql` are typical dates, not an official schedule. Check them against
nps.gov before relying on them.

## Running locally

**API** — from `NationalParkServiceAPI`:

```powershell
dotnet run --launch-profile https
```

It listens on `https://localhost:7286` (and `http://localhost:5277`). The connection string is
`ConnectionStrings:NationalParkService` in `appsettings.json`. In Development, the OpenAPI document is at `/openapi/v1.json`.

**React app** — from `NationalParkServiceReact`:

```powershell
npm install
npm run dev
```

Open `http://localhost:63371`. Vite proxies `/api` to the API on `https://localhost:7286`, so start the API first.

## Tests

```powershell
dotnet test NationalParkServiceAPI/NationalParkServiceAPI.Tests
```

For the front end, `npm run lint` and `npm run build` in `NationalParkServiceReact`.

## API

| Method | Route | Auth | Description |
| --- | --- | --- | --- |
| GET | `/api/parks` | | All parks with their campgrounds and seasons |
| GET | `/api/parks/available?checkIn=&checkOut=` | | Parks with free, in-season sites for the stay, and how many |
| GET | `/api/campgrounds/{id}/sites?checkIn=&checkOut=` | | A campground's sites and whether each is free |
| POST | `/api/users` | | Sign up |
| POST | `/api/users/login` | | Log in; returns a bearer token |
| GET | `/api/users/me` | ✓ | The signed-in user |
| GET | `/api/reservations` | ✓ | The signed-in user's reservations |
| GET | `/api/reservations/{code}` | ✓ | One reservation by confirmation code |
| POST | `/api/reservations` | ✓ | Book a site: `{ siteId, checkIn, checkOut, guests }` |
| DELETE | `/api/reservations/{code}` | ✓ | Cancel a reservation |
| GET | `/api/passtypes` | | Park pass types |
| GET/POST/PUT/DELETE | `/api/passes` | | Park passes |

Dates are `yyyy-MM-dd`. Check-out is the morning you leave, so it isn't counted as a night. Bearer tokens last 7 days,
and the React app sends them in the `Authorization` header. Validation errors come back as `400` with a plain-text
message; a site that's already booked returns `409`.

## Deployment (Azure)

- **API:** Azure App Service. Set the `NationalParkService` connection string to the Azure SQL database, and set
  `Cors:AllowedOrigins` to the front end's URL (in `appsettings.json` or as an app setting such as
  `Cors__AllowedOrigins__0`). Run the SQL scripts against the Azure database before deploying code that needs new
  columns. If the API fails because the database is out of date, the browser reports it as a CORS error because the
  error response has no CORS header, so check the API's Log stream first.
- **Front end:** Azure Static Web Apps. `npm run build` reads `VITE_API_URL` (the deployed API address, no trailing
  slash) from `.env.production`.
