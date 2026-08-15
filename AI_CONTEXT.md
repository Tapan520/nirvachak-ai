# AI_CONTEXT.md — Nirvachak AI

> **Purpose:** This file is the single source of truth for any AI tool working on this project.
> Read this before touching any module. It answers *what exists*, *where it lives*, and *how it works*.

---

## 1. Project Identity

| Property | Value |
|---|---|
| **Name** | Nirvachak AI |
| **Type** | India MLA & Ward Election Campaign Management System |
| **Web Stack** | ASP.NET Core 8 · Razor Pages (UI) + Web API (mobile REST) |
| **Mobile** | React Native + Expo SDK (TypeScript) in `mobile/` |
| **Database** | SQLite via EF Core 8 (`AppDbContext`) |
| **Auth (Web)** | ASP.NET Core Identity + Cookie (12 h sliding) |
| **Auth (API)** | JWT Bearer (24 h, zero clock-skew) |
| **Real-time** | SignalR hub at `/hubs/electionday` (`ElectionDayHub`) |
| **Hosting** | Railway.app — DB persisted at `/data/election.db` (volume) |
| **API Docs** | Swagger at `/swagger` (dev only) |

---

## 2. Roles & Permissions

```
SuperAdmin      -> platform-wide; no constituency; all access
Admin           -> one constituency; can create/manage users except SuperAdmin
CampaignManager -> one constituency; manages FieldWorker & BoothAgent
Candidate       -> one constituency; read-heavy; analytics & announcements
FieldWorker     -> one constituency + assigned booths; voter visits & calls
BoothAgent      -> one constituency + assigned booths; limited access
```

**Key rule:** Every user has a `ConstituencyId` (except `SuperAdmin`).
All data queries are automatically scoped by `ConstituencyId`.

---

## 3. Project Structure

```
Nirvachak_AI/
??? Controllers/              # REST API controllers (JWT auth)
??? Domain/
?   ??? Entities/             # All EF Core entity classes
?   ??? Enums/                # UserRole, VoterSentiment, etc.
??? Hubs/                     # ElectionDayHub (SignalR)
??? Infrastructure/
?   ??? Data/
?   ?   ??? AppDbContext.cs   # EF Core DbContext — single source of all DbSets
?   ??? Services/             # Business logic services (scoped & hosted)
??? Migrations/               # EF Core migrations
??? Models/Api/
?   ??? ApiModels.cs          # All API request/response DTOs (records)
??? Pages/                    # Razor Pages (web UI)
?   ??? Account/              # Login, Logout, 2FA, ForgotPassword
?   ??? Admin/                # User management, Rewards, Exotel, Backup
?   ??? Analytics/            # Sentiment, demographics, SurveyDemographics
?   ??? Announcements/        # Internal comms with role targeting
?   ??? BoothHeatMap/         # Booth-wise sentiment heat map
?   ??? BoothShifts/          # Volunteer shift planner
?   ??? Booths/               # Booth management
?   ??? Broadcast/            # WhatsApp/SMS message broadcasting
?   ??? Budget/               # Campaign budget planner
?   ??? Campaign/             # Campaign events (rally, padyatra, etc.)
?   ??? Competitor/           # Competitor intelligence tracker
?   ??? Dashboard/            # Main dashboard with live stats
?   ??? ElectionDay/          # Live turnout + booth checklist
?   ??? Expenses/             # EC-compliant expense tracking
?   ??? FieldReports/         # Daily field reports by workers
?   ??? Grievances/           # Voter grievance management
?   ??? Influencers/          # Community/religious leader network
?   ??? Leaderboard/          # Worker performance leaderboard
?   ??? PannaPramukh/         # Panna Pramukh contact tracker
?   ??? PhoneBanking/         # Phone call logging & stats
?   ??? Predictions/          # Predictive analytics (ML-lite)
?   ??? RapidResponse/        # Crisis/competitor rapid response
?   ??? Reports/              # EC PDF export, data exports
?   ??? Results/              # Election result entry (round-wise)
?   ??? Shared/               # _Layout.cshtml, _ValidationScripts
?   ??? Survey/               # Public voter self-survey (anonymous, rate-limited)
?   ??? Surveys/              # Internal survey management
?   ??? SwingVoters/          # Swing voter identification & re-engagement
?   ??? Transport/            # Voter transport request management
?   ??? VoterSlips/           # Print-ready QR voter slips
?   ??? Volunteers/           # Volunteer management
?   ??? Voters/               # Voter list, detail, import, create
??? wwwroot/                  # Static assets (CSS, JS, icons, manifest)
??? Program.cs                # App bootstrap, DI, middleware pipeline
??? SampleData/               # Sample voter CSV for import testing
??? mobile/                   # React Native Expo app
    ??? src/
        ??? api/              # Axios API clients (auth, voters, calls, etc.)
        ??? context/          # AuthContext, OfflineSyncContext
        ??? navigation/       # React Navigation stack
        ??? screens/          # App screens (Login, Dashboard, Voters, etc.)
```

---

## 4. Core Entities (Domain/Entities/)

| Entity | Key Fields | Notes |
|---|---|---|
| `AppUser` | `FullName`, `Role`, `ConstituencyId`, `AssignedBoothNumbers`, `IsActive` | Extends `IdentityUser` |
| `Voter` | `VoterId`, `Name`, `BoothNumber`, `Sentiment`, `ElectionDayStatus`, `IsDeleted` | Soft-delete via global query filter |
| `Constituency` | `Name`, `Code`, `ElectionType`, `CandidateName`, `ElectionDate` | Top-level scoping entity |
| `Booth` | `BoothNumber`, `BoothName`, `TotalVoters`, `ConstituencyId` | |
| `Ward` | `WardNumber`, `WardName`, `ConstituencyId` | |
| `DoorToDoorVisit` | `WorkerUserId`, `VoterId`, `VisitedAt`, `SentimentAfterVisit` | Core field activity log |
| `PhoneCallLog` | `CalledByUserId`, `VoterId`, `CalledAt`, `Outcome`, `SentimentAfterCall` | Phone banking log |
| `Grievance` | `Title`, `Status`, `Priority`, `Ward`, `BoothNumber` | Open -> InProgress -> Resolved |
| `Expense` | `Amount`, `Category`, `ExpenseDate`, `IsECCompliant` | EC budget tracked against Rs.40L cap |
| `CampaignEvent` | `Title`, `EventType`, `ScheduledAt`, `IsCompleted` | Rally, DoorToDoor, PhoneCall, etc. |
| `Volunteer` | `Name`, `Task`, `AssignedBoothNumbers`, `IsActive` | |
| `Announcement` | `Title`, `Body`, `Category`, `TargetRoles`, `RequiresAcknowledgement` | Role-targeted internal comms |
| `Survey` + `SurveyResponse` | `Title`, `Category`, `Responses` | Admin-created surveys |
| `Influencer` | `Name`, `Category`, `Community`, `Alignment`, `EstimatedFollowers` | Community leader network |
| `CompetitorActivity` | `CompetitorName`, `ActivityType`, `ThreatLevel` | Competitor intel tracker |
| `PannaPramukh` | `Name`, `BoothNumber`, `PannaNumber`, `VotersContacted` | Traditional voter outreach |
| `TransportVehicle` + `VoterTransportRequest` | Driver, vehicle, booth assignment | Election day voter transport |
| `FieldReport` | `WorkerUserId`, `ContactsMade`, `FavourContacts`, `Status` | Daily worker reports |
| `RapidResponseItem` | `Title`, `ThreatLevel`, `Status`, `AffectedWards` | Crisis response tracker |
| `BudgetPlan` | `Category`, `PlannedAmount` | Budget vs actual spending |
| `ElectionResult` | `BoothNumber`, `RoundNumber`, `CandidateVotes` | Round-wise result entry |
| `BoothShiftAssignment` | `VolunteerId`, `BoothNumber`, `ShiftStart`, `ShiftEnd` | Volunteer shift planner |
| `MessageTemplate` + `MessageBroadcast` | `Title`, `Body`, `Language` | WhatsApp/SMS broadcasting |
| `VoterTag` | `VoterId`, `Tag` | Freeform voter tagging |
| `VoterProfile` + `VoterConsent` + `SurveyCompletion` | Self-survey + coupon reward | Public voter self-survey module |
| `AuditLog` | `UserId`, `Action`, `EntityType`, `Details` | All sensitive actions tracked |
| `ExotelConfig` | `AccountSid`, `ApiKey`, `CallerId` | Per-constituency Exotel config |

---

## 5. Key Enums (Domain/Enums/)

| Enum | Values |
|---|---|
| `UserRole` | `SuperAdmin, Admin, CampaignManager, Candidate, FieldWorker, BoothAgent` |
| `VoterSentiment` | `Favour, Against, Neutral, Floating, Unknown` |
| `ElectionDayStatus` | `NotVoted, Voted, Absent` |
| `GrievancePriority` | `Low, Medium, High, Critical` |
| `GrievanceStatus` | `Open, InProgress, Resolved` |
| `CallOutcome` | `Talked, NoAnswer, CallBack, Wrong, Refused` |
| `InfluencerAlignment` | `Favour, Against, Neutral, Floating, Unknown` |
| `CompetitorActivityType` | `Rally, DoorToDoor, RoadShow, SmallMeeting, SocialMedia, MediaCoverage, Announcement, Other` |
| `CompetitorThreatLevel` | `Low, Medium, High, Critical` |
| `AnnouncementCategory` | `CampaignAnnouncement, CriticalAlert, ECComplianceNotice, DailyBriefing, Motivation, LiveDataNudge` |
| `VolunteerTask` | `BoothManagement, VoterOutreach, Transport, DataEntry, Communication, Other` |
| `ElectionType` | `MLA, Ward, Parliament` |
| `SurveyCategory` | `CandidateAwareness, LocalIssues, GeneralOpinion` |

---

## 6. Scoring & Leaderboard Logic

File: `Pages/Leaderboard/Index.cshtml` + `Index.cshtml.cs`

```
Score = (Visits x 2) + (Calls x 1) + (Favour Conversions x 3)
```

- Only `FieldWorker`, `BoothAgent`, `CampaignManager` are ranked.
- Period: `today` | `week` (Sun-Sat) | `month` | `alltime`.
- **Dense ranking** — tied scores share the same rank number.
- Top 3 displayed as podium cards (Gold, Silver, Bronze).

---

## 7. Authentication Flow

### Web (Cookie)
- Login -> `Pages/Account/Login.cshtml` -> `SignInManager.PasswordSignInAsync`
- Optional 2FA -> `Pages/Account/TwoFactorLogin.cshtml`
- 12-hour sliding cookie; all pages under `/` are authorized by default.
- Anonymous pages: `/Account/*`, `/Survey/*`

### API (JWT)
- `POST /api/auth/login` -> returns `{ token, expiresAt, role, ... }`
- All API controllers inherit `ApiBaseController` which provides:
  - `GetConstituencyId()` — from JWT claim
  - `GetUserRole()` — from JWT claim
- Mobile app stores token in `AsyncStorage` via `AuthContext`.

---

## 8. API Controllers (Controllers/)

| Controller | Base Route | Purpose |
|---|---|---|
| `AuthController` | `/api/auth` | Login, get current user |
| `VotersController` | `/api/voters` | Paged list, detail, sentiment, visit |
| `BoothsController` | `/api/booths` | Booth list with turnout |
| `ElectionDayController` | `/api/electionday` | Live turnout, mark voted/absent |
| `GrievancesController` | `/api/grievances` | CRUD grievances |
| `VolunteersController` | `/api/volunteers` | CRUD volunteers |
| `CampaignEventsController` | `/api/campaign` | CRUD events |
| `SurveysController` | `/api/surveys` | List, submit response |
| `ExpensesController` | `/api/expenses` | CRUD expenses |
| `AnalyticsController` | `/api/analytics` | Sentiment, demographics |
| `PredictiveAnalyticsController` | `/api/predictions` | Booth-level predictions |
| `PhoneBankingController` | `/api/phonebanking` | Call stats, log call, search |
| `InfluencersController` | `/api/influencers` | CRUD influencers |
| `CompetitorController` | `/api/competitor` | Log competitor activities |
| `WinProbabilityController` | `/api/winprobability` | Win probability score |
| `PannaPramukhController` | `/api/pannapramukh` | Panna Pramukh management |
| `BoothShiftsController` | `/api/boothshifts` | Shift assignments |
| `BudgetController` | `/api/budget` | Budget plan CRUD |
| `BroadcastController` | `/api/broadcast` | Message templates & broadcasts |
| `FieldReportsController` | `/api/fieldreports` | Field report CRUD |
| `RapidResponseController` | `/api/rapidresponse` | Crisis item CRUD |
| `TransportController` | `/api/transport` | Vehicles & transport requests |
| `VoterSlipsController` | `/api/voterslips` | Paged voter slips |
| `ElectionResultsController` | `/api/results` | Round-wise results |
| `VoterSurveyController` | `/api/votersurvey` | Self-survey profile & consent |

---

## 9. Background Services (Infrastructure/Services/)

| Service | Type | Purpose |
|---|---|---|
| `DatabaseBackupService` | `IHostedService` (Singleton) | Scheduled SQLite DB backup |
| `ExpenseBudgetAlertService` | `IHostedService` | Alert when expense cap is near |
| `SwingVoterAlertService` | `IHostedService` | Alert on significant sentiment swings |
| `DailyBriefingService` | `IHostedService` | Auto-post morning briefing announcement |
| `NoonAlertService` | `IHostedService` | Midday nudge for low-coverage booths |

---

## 10. Scoped Services (Infrastructure/Services/)

| Service | Purpose |
|---|---|
| `VoterImportService` | Parses CSV, bulk-inserts voters |
| `ElectionDayService` | Turnout calculations |
| `VoterSlipService` | Generates voter slip data |
| `JwtTokenService` | Creates signed JWT for mobile login |
| `AuditService` | Writes to `AuditLogs` table; call `LogAsync` or `Track` |
| `PredictiveAnalyticsService` | Booth-level turnout & support prediction |
| `WinProbabilityService` | Computes overall win probability score |
| `ExotelService` (+ interface) | Exotel click-to-call & SMS |
| `PushNotificationService` | Expo push notifications |
| `SmtpEmailService` (+ interface) | Email via SMTP / Mailjet / Resend |
| `SeedService` | Static; seeds demo data on first run |

---

## 11. Important Patterns & Conventions

### Razor Pages
- Each module has `IndexModel`, `CreateModel`, `EditModel`, `DeleteModel` in `Pages/<Module>/`.
- `[BindProperty(SupportsGet = true)]` for filter parameters.
- Role checks via `[Authorize(Roles = "...")]` on the `PageModel` class.
- `ConstituencyId` is always read from the current `AppUser`, not from query params (except SuperAdmin).

### API Controllers
- All controllers use JWT Bearer auth: `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`
- Inherit `ApiBaseController` for `GetConstituencyId()` and `GetUserRole()` helpers.
- DTOs are `record` types defined in `Models/Api/ApiModels.cs`.

### EF Core
- `AppDbContext` has all `DbSet<T>` properties — always add new entities here.
- `Voter` has a **global soft-delete query filter** (`IsDeleted == false`). Use `.IgnoreQueryFilters()` in admin restore scenarios.
- Performance indexes are defined in `OnModelCreating()`.
- New migrations: `dotnet ef migrations add <PascalCaseName>` — naming convention: `<timestamp>_<PascalCaseName>.cs`.

### Constituency Scoping Pattern
```csharp
// Used in every PageModel and Controller:
var cId = IsAdmin
    ? (SelectedConstituencyId ?? user.ConstituencyId)
    : user.ConstituencyId;
```

### Audit Logging Pattern
```csharp
// Async (awaited) — preferred for important actions:
await _audit.LogAsync(userId, userName, "Action", "EntityType", entityId, "Details", constituencyId);

// Synchronous tracking (fire-and-forget via EF):
_audit.Track(userId, userName, "Action", "EntityType", entityId, "Details", constituencyId);
```

---

## 12. Database Initialization Strategy (Program.cs)

- **Fresh install** (no DB file): `EnsureCreatedAsync()` -> then stamps all migration IDs as applied.
- **Existing DB**: `MigrateAsync()` — applies only pending migrations, never deletes data.
- **WAL mode** enabled after init: `PRAGMA journal_mode=WAL`
- DB path priority: `DATABASE_PATH` env var -> `/data/election.db` (prod) -> `appsettings.json` (dev only)

---

## 13. Mobile App (React Native / Expo) — `mobile/`

| Folder | Purpose |
|---|---|
| `mobile/src/api/` | Axios API clients — one file per domain (`voters.ts`, `auth.ts`, `phoneBanking.ts`, etc.) |
| `mobile/src/context/AuthContext` | JWT token storage, login/logout state |
| `mobile/src/context/OfflineSyncContext` | Queues visits offline; auto-syncs on reconnect |
| `mobile/src/navigation/` | React Navigation stack definition |
| `mobile/src/screens/` | One screen per feature (Login, Dashboard, VoterList, VoterDetail, etc.) |

> **Important:** Update `mobile/src/api/client.ts` -> `API_BASE_URL` to your LAN IP for physical device testing.

---

## 14. Demo Credentials (Seeded on First Run)

| Role | Email | Password |
|---|---|---|
| SuperAdmin | superadmin@nirvachak.ai | SuperAdmin@123 |
| Admin | admin@election.com | Admin@123 |
| CampaignManager | manager@election.com | Manager@123 |
| FieldWorker | worker@election.com | Worker@123 |

---

## 15. Environment Variables (Railway Production)

| Variable | Purpose |
|---|---|
| `DATABASE_PATH` | Absolute path to SQLite file (e.g. `/data/election.db`) |
| `Jwt__Key` | JWT signing secret (min 32 chars) |
| `Jwt__Issuer` | JWT issuer string |
| `Jwt__Audience` | JWT audience string |
| `ConnectionStrings__DefaultConnection` | Fallback DB path (dev only) |

---

## 16. Common Gotchas — Must Read Before Coding

| Gotcha | Detail |
|---|---|
| Voter soft-delete | Always filtered globally; use `.IgnoreQueryFilters()` to see deleted voters |
| Constituency scoping | Never skip it — every query must be scoped for non-SuperAdmin users |
| Emoji in Razor files | Use actual Unicode characters; copy-pasted emoji from chat tools may corrupt to `??` |
| Leaderboard ranking | Uses dense-rank (tied scores -> same rank), not sequential row numbers |
| SignalR in production | Railway proxies WebSocket; `UseForwardedHeaders` is already configured |
| EC budget cap | Hard-coded at Rs.40,00,000 in `EcPdfModel`; update if EC changes the limit |
| AuditService | Must be called on every create/update/delete of sensitive data |
| JWT vs Cookie | Web pages use cookies; `/api/*` routes use JWT — never mix the two |
| DB migrations | Add via `dotnet ef migrations add <Name>`; app auto-applies on deploy |
| SuperAdmin has no ConstituencyId | Always null-check `ConstituencyId` before scoping queries |
