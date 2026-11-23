# PathForge — Setup & Run Guide

How to install, run, and keep the database working — including **what to do
after a laptop restart**. For the full feature/architecture reference, see
[README.md](README.md).

---

## 1. Prerequisites

| Tool | Check | Notes |
|---|---|---|
| **.NET 10 SDK** | `dotnet --version` → `10.0.x` | Required to build and run. |
| **SQL Server Express** | service `MSSQL$SQLEXPRESS01` exists | Instance name used by the app. |
| **sqlcmd** (optional) | ships with the SQL Client SDK | Handy for inspecting the DB. |

The app connects to the SQL Server Express instance **`.\SQLEXPRESS01`**
using Windows authentication. The connection string lives in
`appsettings.json`:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS01;Database=PathForgeDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
```

> **Different machine or instance name?** If your SQL Server instance is
> named differently (e.g. `SQLEXPRESS`, or `(localdb)\MSSQLLocalDB`), change
> the `Server=...` part of that line to match. Everything else stays the same.

---

## 2. First-time run

```powershell
cd D:\pathforge

# 1. Make sure the database engine is running (see §4 if the service is stopped)
#    The instance is MSSQL$SQLEXPRESS01.

# 2. Build
dotnet build

# 3. Run — on first start the app will:
#    - create the PathForgeDB database,
#    - apply the EF Core migration (all tables),
#    - seed demo data (12 users, 6 courses, labs, quizzes, etc.)
dotnet run
```

Then open the URL printed in the console, typically:

- **http://localhost:5280**
- **https://localhost:7246**

Log in with any account from the [test accounts table](README.md#10-seeded-demo-data--test-accounts),
e.g. `wei@pathforge.test` / `Student@123`.

> You do **not** need to run `dotnet ef database update` manually. The app
> calls `db.Database.MigrateAsync()` on startup (`Program.cs` → `DataSeeder`),
> so the schema is created/updated automatically. Running the EF command is
> only needed if you want to apply migrations without launching the app.

---

## 3. How it works (startup flow)

```
dotnet run
   │
   ▼
Program.cs builds the web host
   │
   ├─ registers SQL Server DbContext (connection string from appsettings.json)
   ├─ registers session (HttpOnly cookie, 2-hour idle timeout)
   │
   ▼
DataSeeder.SeedAsync(db)
   ├─ db.Database.MigrateAsync()   → creates PathForgeDB + tables if missing
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

## 4. Database start/stop — and what happens on laptop restart

### Does the database start automatically?

The SQL Server Express service (`MSSQL$SQLEXPRESS01`) is normally set to
**Automatic** start, so it **should start on its own when Windows boots**.
In that case, after a restart you just run `dotnet run` and everything works.

### If the database does NOT start after a restart

Sometimes the service is set to *Manual*, or Windows hasn't started it yet.
Symptom: `dotnet run` fails with a connection error like
*"A network-related or instance-specific error… the server was not found"*.

**Fix — start the service (one-time per boot).** Open **PowerShell as
Administrator** and run:

```powershell
net start 'MSSQL$SQLEXPRESS01'
```

or:

```powershell
Start-Service 'MSSQL$SQLEXPRESS01'
```

Check it is running:

```powershell
Get-Service 'MSSQL$SQLEXPRESS01' | Select-Object Name, Status
```

### Make it start automatically on every boot (recommended, do once)

In an **Administrator** PowerShell:

```powershell
Set-Service 'MSSQL$SQLEXPRESS01' -StartupType Automatic
```

After this, the database comes up by itself on every restart and you never
have to start it manually again.

### Stopping the database (if you ever need to)

```powershell
# Administrator PowerShell
net stop 'MSSQL$SQLEXPRESS01'
```

### Your data survives restarts

The database lives inside the SQL Server instance (not in the project folder),
so **all accounts, courses, progress and certificates persist across
restarts**. Restarting the laptop does not wipe anything. The seeder only
inserts demo data when the `Users` table is empty, so it will not duplicate
data on later runs.

---

## 5. Common tasks

### Reset the database to fresh demo data
Drop the database, then run the app again (it recreates and reseeds).
In an Administrator PowerShell with the service running:

```powershell
$sqlcmd = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE"
& $sqlcmd -S ".\SQLEXPRESS01" -E -C -Q "ALTER DATABASE PathForgeDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE PathForgeDB;"
cd D:\pathforge; dotnet run
```

### Inspect the data quickly
```powershell
& "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\SQLCMD.EXE" `
  -S ".\SQLEXPRESS01" -d PathForgeDB -E -C -Q "SELECT COUNT(*) AS Users FROM Users;"
```

### Apply a new migration manually (optional)
```powershell
dotnet ef migrations add <Name>
dotnet ef database update
```

---

## 6. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| *"server was not found / not accessible"* on `dotnet run` | SQL service not running | `net start 'MSSQL$SQLEXPRESS01'` in an admin shell (§4) |
| *"Login failed for user ..."* | Your Windows account isn't a login on the instance | Add it as sysadmin (see below) |
| *"Introducing FOREIGN KEY constraint ... may cause cycles or multiple cascade paths"* | A new FK to `Users` without `Restrict` | Already handled for all current entities; if you add one, set `.OnDelete(DeleteBehavior.Restrict)` in `PathForgeDbContext` |
| Port already in use | A previous `dotnet run` still running | Stop it: `Get-Process dotnet \| Stop-Process -Force` |
| Service won't start, log shows *"misaligned log IOs on master.mdf → Hit Fatal Error"* | Corrupt system databases on that SQL instance | Use a different/freshly-created SQL instance (this project uses `SQLEXPRESS01` for exactly that reason) |

### Granting your Windows account access (if "Login failed")
In an **Administrator** PowerShell, run each line separately:

```powershell
net stop 'MSSQL$SQLEXPRESS01'
net start 'MSSQL$SQLEXPRESS01' /m
sqlcmd -S ".\SQLEXPRESS01" -E -C -Q "CREATE LOGIN [YOURPC\YourUser] FROM WINDOWS; ALTER SERVER ROLE sysadmin ADD MEMBER [YOURPC\YourUser];"
net stop 'MSSQL$SQLEXPRESS01'
net start 'MSSQL$SQLEXPRESS01'
```

Replace `YOURPC\YourUser` with your Windows account (run `whoami` to see it).
`/m` starts SQL in single-user mode so a local admin can add the login.

---

## 7. Quick reference

```powershell
# Start DB (admin shell)        net start 'MSSQL$SQLEXPRESS01'
# Make DB auto-start (admin)    Set-Service 'MSSQL$SQLEXPRESS01' -StartupType Automatic
# Build                         dotnet build
# Run                           dotnet run      →  http://localhost:5280
# Stop a stuck run              Get-Process dotnet | Stop-Process -Force
```
