# BackOffice Database Scripts

SQL Server DDL scripts for the TechWayFit Pulse BackOffice portal.

## Prerequisites

The **main application's** `src/TechWayFit.Pulse.Infrastructure/Scripts/V1.0/` scripts must be run first to create the `pulse` schema and all main-app tables. BackOffice tables live in the same `[pulse]` schema and same database.

## Structure

```
Scripts/
└── v1.0/
    └── SqlServer/
        ├── 00_MasterSetup.sql   — runs all scripts below in order
        ├── 01_CreateTables.sql  — pulse.BackOfficeUsers, pulse.AuditLogs
        └── 02_CreateIndexes.sql — all indexes for BackOffice tables
```

## Running the Scripts

### Option A — Master script (recommended)

Open `00_MasterSetup.sql` in SSMS, update the `USE [TechWayFitPulse]` database name, and execute.

### Option B — Individual scripts

Run in order against your database:

```sql
-- 1. Tables
:r v1.0/SqlServer/01_CreateTables.sql

-- 2. Indexes
:r v1.0/SqlServer/02_CreateIndexes.sql
```

All scripts are **idempotent** — safe to re-run without duplicating objects.

## Tables Created

| Table | Description |
|---|---|
| `pulse.BackOfficeUsers` | Operator accounts (separate from FacilitatorUsers) |
| `pulse.AuditLogs` | Immutable audit trail for all operator actions |

## Notes

- EF Core **migrations are disabled** for SQL Server in the BackOffice. All schema changes must be scripted here.
- `AuditLogs` rows must **never be deleted**. Archive to cold storage after your retention period.
- `BackOfficeUsers` are completely separate from main-app `FacilitatorUsers`. They authenticate via BCrypt password hash, not OTP.
