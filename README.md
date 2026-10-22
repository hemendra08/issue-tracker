# Distributed Issue Tracker — Feature-Wise API

A modern, production-ready Issue Tracking platform built with ASP.NET Core, PostgreSQL, and Entity Framework Core following a **Feature-Wise Architecture** (Modular Vertical Slice).

> [!NOTE]
> For the comprehensive master architecture guide, Mermaid diagrams, complete API contracts, and upcoming roadmap, see **[PROJECT_GUIDE.md](docs/PROJECT_GUIDE.md)**.

---

## 1. Project Architecture

The application is structured feature-by-feature inside a single cohesive Web API:

```
backend/
├── src/
│   └── IssueTracker.Api/
│       ├── Features/
│       │   ├── Auth/          # Auth, Users, Roles, JWT Tokens
│       │   └── Projects/      # Projects, Workspaces, Teams
│       ├── Data/              # Entity Framework Core DbContext (dit_db)
│       ├── Middleware/        # Pipeline filters (Correlation ID, Global Error Handling)
│       ├── Program.cs         # App entry point & endpoint registration
│       └── appsettings.json   # PostgreSQL configuration
└── tests/
    └── IssueTracker.Api.Tests/ # Automated integration test suite
```

---

## 2. Requirements

- **.NET 9 SDK** (`9.0.318` or later)
- **PostgreSQL 16+** running on `localhost:5432`

---

## 3. Database Configuration

The application connects to a single PostgreSQL database: `dit_db`.

Default connection in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=dit_db;Username=postgres;Password=postgres"
}
```

Migrations are applied automatically upon application startup when running in Development mode (`ApplyMigrationsOnStartup: true`).

---

## 4. How to Build & Run

### Build the Solution
```powershell
dotnet build backend/DistributedIssueTracker.sln
```

### Run All Tests
```powershell
dotnet test backend/DistributedIssueTracker.sln
```

### Start the API Locally
```powershell
dotnet run --project backend/src/IssueTracker.Api
```
The API starts on **`http://localhost:5000`**.

---

## 5. API Endpoints & Verification

### Interactive Documentation (Scalar UI)
- Open [http://localhost:5000/scalar/v1](http://localhost:5000/scalar/v1) for interactive API testing.
