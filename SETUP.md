# PathForge — Setup & Run Guide

How to install and run the project. For the full feature/architecture
reference, see [README.md](README.md).

> **Database:** PathForge uses **SQLite** — a single local file
> (`PathForgeDB.db`) created automatically on first run. There is **no
> database server to install, start, or configure.** This also means it
> works the same on any machine and survives laptop restarts with no extra
> steps.

---

## 1. Prerequisites

| Tool | Check | Notes |
|---|---|---|
| **.NET 10 SDK** | `dotnet --version` → `10.0.x` | The only requirement to build and run. |

That's it. No SQL Server, no LocalDB, no services.

The connection string in `appsettings.json` simply points at a file:

```json
"DefaultConnection": "Data Source=PathForgeDB.db"
```

---

## 2. First-time run

```powershell
cd D:\pathforge

# 1. Build
dotnet build

# 2. Run — on first start the app will:
#    - create the PathForgeDB.db SQLite file,
#    - apply the EF Core migration (all tables),
#    - seed demo data (12 users, 6 courses, labs, quizzes, etc.)
dotnet run
```

Then open the URL printed in the console, typically:

- **http://localhost:5280**
- **https://localhost:7246**

Log in with any account from the
[test accounts table](README.md#10-seeded-demo-data--test-accounts),
e.g. `wei@pathforge.test` / `Student@123`.

> You do **not** need to run `dotnet ef database update` manually. The app
> calls `db.Database.MigrateAsync()` on startup (`Program.cs` → `DataSeeder`),
> so the schema is created automatically and the demo data is seeded.

---

## 3. How it works (startup flow)

```
dotnet run
   │
   ▼
Program.cs builds the web host
   │
   ├─ registers SQLite DbContext (connection string from appsettings.json)
   ├─ registers session (HttpOnly cookie, 2-hour idle timeout)
   │
   ▼
DataSeeder.SeedAsync(db)
   ├─ db.Database.MigrateAsync()   → creates PathForgeDB.db + tables if missing
   └─ if Users table is empty      → inserts all demo data (idempotent)
   │
   ▼
Middleware pipeline: HTTPS redirect → static files → routing → session → authorization → MVC routes
   │
   ▼
Listening on http://localhost:5280
```

Authentication is session-based: on login the user's id, name, role and email
are written to the session; protected controller actions check the role via
`SessionHelper`. No ASP.NET Identity is used.

---

## 4. The database & laptop restarts

The entire database is the **`PathForgeDB.db`** file in the project folder
(SQLite may also create `PathForgeDB.db-wal` and `PathForgeDB.db-shm` helper
files — this is normal).

- **Nothing to start.** Because SQLite is a file, there is no service to
  launch after a reboot. Just run `dotnet run` and it works.
- **Your data persists.** All accounts, courses, progress and certificates
  stay in `PathForgeDB.db` across restarts.
- **The seeder is idempotent** — it only inserts demo data when the `Users`
  table is empty, so it never duplicates data on later runs.

> These `.db*` files are git-ignored (see `.gitignore`), so your local data
> is never committed to the repository.

---

## 5. Common tasks

### Reset the database to fresh demo data
Stop the app, delete the database file, and run again:

```powershell
cd D:\pathforge
# Stop any running instance first
Get-Process dotnet | Stop-Process -Force -ErrorAction SilentlyContinue
# Delete the database (and its WAL/SHM sidecars)
Remove-Item PathForgeDB.db* -Force
# Run — recreates and reseeds automatically
dotnet run
```

### Inspect the data (optional)
Use any SQLite viewer, e.g. **DB Browser for SQLite** (free), and open
`PathForgeDB.db`. Or with the `sqlite3` CLI:

```powershell
sqlite3 PathForgeDB.db "SELECT COUNT(*) FROM Users;"
```

### Apply a new migration (only if you change the entity classes)
```powershell
dotnet ef migrations add <Name>
dotnet ef database update
```

---

## 6. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Port already in use | A previous `dotnet run` is still running | `Get-Process dotnet \| Stop-Process -Force` |
| Build error: *"file is locked by PathForge"* | App still running while building | Stop it first (command above), then `dotnet build` |
| Pages render with a plain font for a split second | Fonts load locally with `font-display: swap` | Normal; no action needed |
| Want a totally clean slate | — | Delete `PathForgeDB.db*` and run again (§5) |

---

## 7. Quick reference

```powershell
# Build                 dotnet build
# Run                   dotnet run          →  http://localhost:5280
# Reset DB              Remove-Item PathForgeDB.db* -Force ; dotnet run
# Stop a stuck run      Get-Process dotnet | Stop-Process -Force
```

---

## Note on the database choice

Earlier versions of this project targeted SQL Server Express. It now uses
**SQLite** for simplicity and speed: no server to install or keep running,
instant startup, identical EF Core code (LINQ, migrations, relationships),
and a single portable file. All database requirements in the assignment brief
(INSERT / SELECT / UPDATE / DELETE, relationships, connectivity via EF Core)
are fully satisfied by SQLite.
