# Distributed Issue Tracker — Feature-Wise Local Walkthrough

This project uses a clean **Feature-Wise Architecture** (Modular Vertical Slice).
Everything runs in a single application with zero gateway complexity, zero port conflicts, and one unified database.

---

## 1. Project Structure

```
backend/src/IssueTracker.Api/
├── Features/
│   ├── Identity/          <-- Milestone 2: Auth, Users, JWT Tokens
│   │   └── IdentityEndpoints.cs
│   ├── Projects/          <-- Milestone 3: Projects & Workspaces
│   │   └── ProjectEndpoints.cs
│   └── Issues/            <-- Milestone 4: Tickets, Comments, Workflow
│       └── IssueEndpoints.cs
├── Data/
│   └── AppDbContext.cs    <-- Entity Framework Core (connected to dit_db)
├── Common/
│   └── Middleware/        <-- Correlation ID (X-Request-ID) & Error Handling
├── Program.cs
└── appsettings.json
```

---

## 2. Database

- **Database Name**: `dit_db` (PostgreSQL on port `5432`)
- **Connection String**: `Host=localhost;Port=5432;Database=dit_db;Username=postgres;Password=postgres`
- Automatic migrations are enabled on startup (`"ApplyMigrationsOnStartup": true`).

---

## 3. How to Run the Project

You only need **ONE terminal command**:

```powershell
dotnet run --project backend/src/IssueTracker.Api
```

The application starts on **`http://localhost:5000`**.

---

## 4. How to Test & Verify in Your Browser

Once running, test these endpoints directly:

### A. Interactive API Documentation (Scalar / Swagger)
Open in your browser:
- [http://localhost:5000/scalar/v1](http://localhost:5000/scalar/v1)
- Shows all features, endpoints, and allows interactive API testing with a single click.

### B. Health & Database Connectivity
- **Process Liveness**: [http://localhost:5000/health/live](http://localhost:5000/health/live) &rarr; `{"status":"Healthy"}`
- **Database Connection**: [http://localhost:5000/health/ready](http://localhost:5000/health/ready) &rarr; `{"status":"Healthy","entries":[{"key":"dit_db","status":"Healthy"}]}`

### C. Feature Pings
- **Identity Feature**: [http://localhost:5000/api/v1/identity/health/ping](http://localhost:5000/api/v1/identity/health/ping)
- **Projects Feature**: [http://localhost:5000/api/v1/projects/health/ping](http://localhost:5000/api/v1/projects/health/ping)
- **Issues Feature**: [http://localhost:5000/api/v1/issues/health/ping](http://localhost:5000/api/v1/issues/health/ping)

---

## 5. Next Step: Milestone 2 (Identity & Auth)
We are now ready to build out `Features/Identity/`:
- `User` and `RefreshToken` domain models
- Registration endpoint (`POST /api/v1/identity/auth/register`)
- Password hashing (Argon2id)
- Login endpoint with JWT tokens (`POST /api/v1/identity/auth/login`)
