# Nirvachak AI

**Nirvachak AI** — A full-stack **India MLA & Ward Election Management System** with a modern Web App and React Native Mobile App.

---

## 🤖 Tech Stack

| Layer | Technology |
|---|---|
| **Web Backend** | ASP.NET Core 8 · Razor Pages |
| **REST API** | ASP.NET Core 8 · Web API + JWT Auth |
| **Real-time** | SignalR (live election day turnout) |
| **Database** | MySQL via Entity Framework Core 8 (Pomelo) |
| **Auth** | ASP.NET Core Identity + JWT Bearer |
| **API Docs** | Swagger / OpenAPI |
| **Mobile** | React Native + Expo SDK 51 (TypeScript) |

---

## 🚀 Quick Start

### 1. Web App

# From project root
dotnet restore
dotnet run --launch-profile http

| URL | Description |
|---|---|
| http://localhost:5211 | Web Application |
| http://localhost:5211/swagger | Swagger API Explorer |

### 2. Mobile App

cd mobile
npm install
npx expo start

# Press A → Android Emulator
# Press I → iOS Simulator
# Scan QR → Physical Device (install Expo Go first)

> 🔧 Update `mobile/src/api/client.ts` → `API_BASE_URL` to your machine's local IP for physical device testing.

---

## 🔑 Demo Login Credentials

| Role | Email | Password |
|---|---|---|
| **SuperAdmin** | superadmin@nirvachak.ai | SuperAdmin@123 |
| **Admin** | admin@election.com | Admin@123 |
| **Campaign Manager** | manager@election.com | Manager@123 |
| **Field Worker** | worker@election.com | Worker@123 |

---

## 📱 Mobile App Screens

| Screen | Description |
|---|---|
| **Login** | JWT-secured authentication |
| **Dashboard** | Live stats — voters, turnout, sentiment |
| **Voter List** | Search, filter, paginate 1000s of voters |
| **Voter Detail** | Full profile, sentiment update, log door-to-door visits |
| **Election Day** | Live turnout tracking, mark voter as voted |
| **Booths** | Booth-wise turnout progress bars |
| **Grievances** | Submit & view grievances |

---

## 🖥️ Web App Modules

| Module | Features |
|---|---|
| **Dashboard** | Stats, sentiment chart, booth summary, upcoming events |
| **Voters** | Import CSV, search/filter, sentiment tracking, visit logs |
| **Voter Slips** | Print-ready QR code slips (booth-wise) |
| **Booths** | Manage booths, assign agents |
| **Campaign Events** | Create and track campaign events |
| **Volunteers** | Register, assign tasks, activate/deactivate |
| **Election Day** | Live turnout dashboard with SignalR |
| **Analytics** | Sentiment charts, age/gender breakdown |
| **Grievances** | Track with priority — Open → In Progress → Resolved |
| **Expenses** | EC-compliant expense tracking |
| **Admin** | User management, roles, audit logs |

---

## 📡 REST API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/login` | Get JWT token |
| GET | `/api/auth/me` | Current user info |
| GET | `/api/dashboard/stats` | Dashboard statistics |
| GET | `/api/voters` | Paginated voter list (search, filter) |
| GET | `/api/voters/{id}` | Voter detail + visit history |
| PATCH | `/api/voters/{id}/sentiment` | Update voter sentiment |
| POST | `/api/voters/{id}/visit` | Log door-to-door visit |
| GET | `/api/booths` | All booths with turnout |
| GET | `/api/electionday/turnout` | Live booth-wise turnout |
| POST | `/api/electionday/mark-voted` | Mark voter as voted |
| POST | `/api/electionday/mark-absent` | Mark voter as absent |
| GET | `/api/grievances` | List grievances |
| POST | `/api/grievances` | Submit grievance |
| GET | `/api/volunteers` | List volunteers |

---

## 📂 Project Structure

Nirvachak_AI/
├── Controllers/              # REST API controllers (JWT)
├── Domain/
│   ├── Entities/             # EF Core entity models
│   └── Enums/                # Enumerations
├── Hubs/                     # SignalR hubs
├── Infrastructure/
│   ├── Data/                 # DbContext
│   └── Services/             # Business logic services
├── Models/Api/               # API request/response DTOs
├── Pages/                    # Razor Pages (web UI)
│   ├── Account/
│   ├── Admin/
│   ├── Analytics/
│   ├── Booths/
│   ├── Campaign/
│   ├── Dashboard/
│   ├── ElectionDay/
│   ├── Expenses/
│   ├── Grievances/
│   ├── Shared/
│   ├── VoterSlips/
│   └── Voters/
├── SampleData/               # Sample CSV for voter import
├── wwwroot/                  # Static files (CSS, JS)
├── mobile/                   # React Native Expo app
│   └── src/
│       ├── api/              # Axios API client
│       ├── context/          # Auth context
│       ├── navigation/       # React Navigation
│       └── screens/          # App screens
├── Program.cs                # App bootstrap, DI, middleware pipeline
└── appsettings.json

---

## 🔒 Security

- **Web**: ASP.NET Core Identity with cookie-based authentication
- **API**: JWT Bearer tokens (24-hour expiry)
- **CORS**: Configured for React Native / Expo dev server
- **Role-based**: SuperAdmin, Admin, CampaignManager, Candidate, BoothAgent, FieldWorker
- **Audit Logs**: All sensitive actions tracked

---

## 🌱 Seeded Demo Data

- 1 Constituency (Pune Cantonment)
- 8 Booths pre-configured
- 120 sample voters across booths
- 3 demo users with different roles

---

## 🤖 AI_CONTEXT.md — Nirvachak AI

> **Purpose:** This file is the single source of truth for any AI tool working on this project.  
> Read this before touching any module. It answers *what exists*, *where it lives*, and *how it works*.

### 1. Project Identity

| Property | Value |
|---|---|
| **Name** | Nirvachak AI |
| **Type** | India MLA & Ward Election Campaign Management System |
| **Web Stack** | ASP.NET Core 8 · Razor Pages (UI) + Web API (mobile REST) |
| **Mobile** | React Native + Expo SDK (TypeScript) in `mobile/` |
| **Database** | MySQL via EF Core 8 + Pomelo (`AppDbContext`) |
| **Auth (Web)** | ASP.NET Core Identity + Cookie (12 h sliding) |
| **Auth (API)** | JWT Bearer (24 h, zero clock-skew) |
| **Real-time** | SignalR hub at `/hubs/electionday` (`ElectionDayHub`) |
| **Hosting** | Railway.app — MySQL service via MYSQL_CONNECTION_STRING |
| **API Docs** | Swagger at `/swagger` (dev only) |

### 2. Roles & Permissions

SuperAdmin      → platform-wide; no constituency; all access
Admin           → one constituency; can create/manage users except SuperAdmin
CampaignManager → one constituency; manages FieldWorker & BoothAgent
Candidate       → one constituency; read-heavy; analytics & announcements
FieldWorker     → one constituency + assigned booths; voter visits & calls
BoothAgent      → one constituency + assigned booths; limited access

**Key rule:** Every user has a `ConstituencyId` (except `SuperAdmin`).  
All data queries are automatically scoped by `ConstituencyId`.

### 3. Project Structure

Nirvachak_AI/
├── Controllers/              # REST API controllers (JWT auth)
├── Domain/
│   ├── Entities/             # All EF Core entity classes
│   └── Enums/                # UserRole, VoterSentiment, etc.
├── Hubs/                     # ElectionDayHub (SignalR)
├── Infrastructure/
│   ├── Data/
│   │   └── AppDbContext.cs   # EF Core DbContext — single source of all DbSets
│   └── Services/             # Business logic services (scoped & hosted)
├── Migrations/               # EF Core migrations
├── Models/Api/
│   └── ApiModels.cs          # All API request/response DTOs (records)
├── Pages/                    # Razor Pages (web UI)
│   ├── Account/              # Login, Logout, 2FA, ForgotPassword
│   ├── Admin/                # User management, Rewards, Exotel, Backup
│   ├── Analytics/            # Sentiment, demographics, SurveyDemographics
│   ├── Announcements/        # Internal comms with role targeting
│   ├── BoothHeatMap/         # Booth-wise sentiment heat map
│   ├── BoothShifts/          # Volunteer shift planner
│   ├── Booths/               # Booth management
│   ├── Broadcast/            # WhatsApp/SMS message broadcasting
│   ├── Budget/               # Campaign budget planner
│   ├── Campaign/             # Campaign events (rally, padyatra, etc.)
│   ├── Competitor/           # Competitor intelligence tracker
│   ├── Dashboard/            # Main dashboard with live stats
│   ├── ElectionDay/          # Live turnout + booth checklist
│   ├── Expenses/             # EC-compliant expense tracking
│   ├── FieldReports/         # Daily field reports by workers
│   ├── Grievances/           # Voter grievance management
│   ├── Influencers/          # Community/religious leader network
│   ├── Leaderboard/          # Worker performance leaderboard
│   ├── PannaPramukh/         # Panna Pramukh contact tracker
│   ├── PhoneBanking/         # Phone call logging & stats
│   ├── Predictions/          # Predictive analytics (ML-lite)
│   ├── RapidResponse/        # Crisis/competitor rapid response
│   ├── Reports/              # EC PDF export, data exports
│   ├── Results/              | Election result entry (round-wise)
│   ├── Shared/               # _Layout.cshtml, _ValidationScripts
│   ├── Survey/               # Public voter self-survey (anonymous, rate-limited)
│   ├── Surveys/              # Internal survey management
│   ├── SwingVoters/          # Swing voter identification & re-engagement
│   ├── Transport/            # Voter transport request management
│   ├── VoterSlips/           # Print-ready QR voter slips
│   ├── Volunteers/           # Volunteer management
│   └── Voters/               # Voter list, detail, import, create
├── wwwroot/                  # Static assets (CSS, JS, icons, manifest)
├── Program.cs                # App bootstrap, DI, middleware pipeline
├── SampleData/               # Sample voter CSV for import testing
└── mobile/                   # React Native Expo app
    └── src/
        ├── api/              # Axios API clients (auth, voters, calls, etc.)
        ├── context/          # AuthContext, OfflineSyncContext
        ├── navigation/       # React Navigation stack
        └── screens/          # App screens (Login, Dashboard, Voters, etc.)

### 4. Core Entities (Domain/Entities/)

| Entity | Key Fields | Notes |
|---|---|---|
| `AppUser` | `FullName`, `Role`, `ConstituencyId`, `AssignedBoothNumbers`, `IsActive` | Extends `IdentityUser` |
| `Voter` | `VoterId`, `Name`, `BoothNumber`, `Sentiment`, `ElectionDayStatus`, `IsDeleted` | Soft-delete via global query filter |
| `Constituency` | `Name`, `Code`, `ElectionType`, `CandidateName`, `ElectionDate` | Top-level scoping entity |
| `Booth` | `BoothNumber`, `BoothName`, `TotalVoters`, `ConstituencyId` | |
| `Ward` | `WardNumber`, `WardName`, `ConstituencyId` | |
| `DoorToDoorVisit` | `WorkerUserId`, `VoterId`, `VisitedAt`, `SentimentAfterVisit` | Core field activity log |
| `PhoneCallLog` | `CalledByUserId`, `VoterId`, `CalledAt`, `Outcome`, `SentimentAfterCall` | Phone banking log |
| `Grievance` | `Title`, `Status`, `Priority`, `Ward`, `BoothNumber` | Open → InProgress → Resolved |
| `Expense` | `Amount`, `Category`, `ExpenseDate`, `IsECCompliant` | EC budget tracked against ₹40L cap |
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

### 5. Key Enums (Domain/Enums/)

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

### 6. Scoring & Leaderboard Logic

File: `Pages/Leaderboard/Index.cshtml` + `Index.cshtml.cs`

Score = (Visits × 2) + (Calls × 1) + (Favour Conversions × 3)

- Only `FieldWorker`, `BoothAgent`, `CampaignManager` are ranked.
- Period: `today` | `week` (Sun–Sat) | `month` | `alltime`.
- **Dense ranking** — tied scores share the same rank number.
- Top 3 displayed as podium cards (Gold 🥇, Silver 🥈, Bronze 🥉).

### 7. Authentication Flow

#### Web (Cookie)
- Login → `Pages/Account/Login.cshtml` → `SignInManager.PasswordSignInAsync`
- Optional 2FA → `Pages/Account/TwoFactorLogin.cshtml`
- 12-hour sliding cookie; all pages under `/` are authorized by default.
- Anonymous: `/Account/*`, `/Survey/*`

#### API (JWT)
- `POST /api/auth/login` → returns `{ token, expiresAt, role, ... }`
- All API controllers inherit `ApiBaseController` which provides:
  - `GetConstituencyId()` — from JWT claim
  - `GetUserRole()` — from JWT claim
- Mobile app stores token in `AsyncStorage` via `AuthContext`.

### 8. API Controllers (Controllers/)

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

### 9. Background Services (Infrastructure/Services/)

| Service | Type | Purpose |
|---|---|---|
| `DatabaseBackupService` | `IHostedService` (Singleton) | Scheduled MySQL dump backup (`mysqldump`) |
| `ExpenseBudgetAlertService` | `IHostedService` | Alert when expense cap is near |
| `SwingVoterAlertService` | `IHostedService` | Alert on significant sentiment swings |
| `DailyBriefingService` | `IHostedService` | Auto-post morning briefing announcement |
| `NoonAlertService` | `IHostedService` | Midday nudge for low-coverage booths |

### 10. Scoped Services (Infrastructure/Services/)

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

### 11. Important Patterns & Conventions

#### Razor Pages
- Each page has `IndexModel`, `CreateModel`, `EditModel`, `DeleteModel` in `Pages/<Module>/`.
- `[BindProperty(SupportsGet = true)]` for filter parameters.
- Role checks via `[Authorize(Roles = "...")]` on the `PageModel`.
- ConstituencyId is always read from the current `AppUser`, not from query params (except SuperAdmin).

#### API Controllers
- All API controllers use JWT Bearer auth: `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`
- Inherit `ApiBaseController` for `GetConstituencyId()` and `GetUserRole()` helpers.
- DTOs are `record` types defined in `Models/Api/ApiModels.cs`.

#### EF Core
- `AppDbContext` has all `DbSet<T>` properties.
- Voter has a **global soft-delete query filter** (`IsDeleted == false`). Use `.IgnoreQueryFilters()` in admin restore scenarios.
- Performance indexes are defined in `OnModelCreating()`.
- New migrations go in `Migrations/` with naming `<timestamp>_<PascalCaseName>.cs`.

#### Constituency Scoping
// Pattern used everywhere:
var cId = IsAdmin
    ? (SelectedConstituencyId ?? user.ConstituencyId)
    : user.ConstituencyId;

#### Audit Logging
// Async (awaited):
await _audit.LogAsync(userId, userName, "Action", "EntityType", entityId, "Details", constituencyId);

// Fire-and-forget (tracked via EF change tracking):
_audit.Track(userId, userName, "Action", "EntityType", entityId, "Details", constituencyId);

### 12. Database Initialization Strategy (Program.cs)

- **Startup:** `MigrateAsync()` applies pending EF migrations safely (never drops data).

### 13. Mobile App (React Native / Expo)

| Folder | Purpose |
|---|---|
| `mobile/src/api/` | Axios API clients — one file per domain (voters, auth, calls, etc.) |
| `mobile/src/context/AuthContext` | JWT token storage, login/logout state |
| `mobile/src/context/OfflineSyncContext` | Queues visits offline; syncs on reconnect |
| `mobile/src/navigation/` | React Navigation stack definition |
| `mobile/src/screens/` | One screen per feature (Login, Dashboard, VoterList, VoterDetail, etc.) |

Key mobile API base URL: `mobile/src/api/client.ts` → `API_BASE_URL`  
Update this to your LAN IP for physical device testing.

### 14. Environment Variables (Railway)

| Variable | Purpose |
|---|---|
| `MYSQL_CONNECTION_STRING` | Full MySQL connection string |
| `Jwt__Key` | JWT signing secret (min 32 chars) |
| `Jwt__Issuer` | JWT issuer string |
| `Jwt__Audience` | JWT audience string |
| `ConnectionStrings__DefaultConnection` | Local MySQL connection string (dev) |

### 15. Common Gotchas

| Gotcha | Detail |
|---|---|
| Voter soft-delete | Always filtered globally; use `.IgnoreQueryFilters()` to see deleted voters |
| Constituency scoping | Never skip it — every query must be constituency-scoped for non-SuperAdmin |
| Emoji in Razor | Use actual Unicode characters or HTML entities, not copy-pasted emoji that may corrupt |
| Leaderboard ranking | Uses dense-rank (tied scores → same rank number), not sequential row numbers |
| SignalR in production | Railway proxies WebSocket; `UseForwardedHeaders` is configured for this |
| EC budget cap | Hard-coded at ₹40,00,000 in `EcPdfModel`; update if EC changes the limit |
| AuditService | Call on every create/update/delete of sensitive data (users, votes, expenses) |
| JWT vs Cookie | Web pages use cookies; `/api/*` routes use JWT — never mix them |
| DB migrations | Add via `dotnet ef migrations add <Name>`; deploy handles apply automatically |



