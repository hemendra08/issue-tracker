# Distributed Issue Tracker — System Architecture

## 1. System Overview & Feature Boundaries

The **Distributed Issue Tracker** is designed using a **Feature-Wise Architecture** (Modular Vertical Slice).
Instead of distributed microservice network overhead, features are organized into isolated slices within a unified ASP.NET Core API, sharing a clean domain structure and a single database (`dit_db`).

```
                           +------------------------+
                           |  Client / Browser / UI |
                           +-----------+------------+
                                       |
                                       | Port 5000
                                       v
                    +--------------------------------------+
                    |           IssueTracker.Api           |
                    +------------------+-------------------+
                                       |
                   +-------------------+-------------------+
                   |                   |                   |
                   v                   v                   v
          +-----------------+ +-----------------+ +-----------------+
          | Features/       | | Features/       | | Features/       |
          | Identity        | | Projects        | | Issues          |
          | (Auth & Users)  | | (Projects/Teams)| | (Issues/Tasks)  |
          +--------+--------+ +--------+--------+ +--------+--------+
                   |                   |                   |
                   +-------------------+-------------------+
                                       |
                                       v
                              [ AppDbContext ]
                                       |
                                       v
                             [ dit_db (PostgreSQL) ]
```

### Feature Module Summary

| Feature | Primary Responsibility | Milestone | Endpoints Prefix |
| :--- | :--- | :--- | :--- |
| **Identity** | Authentication, registration, password hashing, JWT tokens | Milestone 2 | `/api/v1/identity/*` |
| **Projects** | Workspaces, project keys, team memberships | Milestone 3 | `/api/v1/projects/*` |
| **Issues** | Issue tracking, statuses, priorities, assignments, comments | Milestone 4 | `/api/v1/issues/*` |

---

## 2. Shared Cross-Cutting Capabilities

- **Observability**: Correlation Request IDs are generated or extracted in `RequestIdMiddleware`, forwarded in headers (`X-Request-ID`), and captured across structured JSON log scopes.
- **Error Shielding**: `GlobalExceptionHandlerMiddleware` returns standard RFC 7807 problem details upon unhandled exceptions.
- **Health Checks**: `/health/live` (process health) and `/health/ready` (PostgreSQL connectivity).
- **API Documentation**: Scalar UI at `/scalar/v1` generated via `Microsoft.AspNetCore.OpenApi`.
