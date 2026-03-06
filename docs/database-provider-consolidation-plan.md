# Database Provider Consolidation - MariaDB Only

## Decision

**Remove SQLite and SQL Server support. Keep only MariaDB as the single database provider.**

## Rationale

### Maintenance Burden
- 3 DbContext implementations to maintain
- 3 sets of repository implementations (SQLite, SQL Server, MariaDB)
- 3 sets of migration scripts (SQL Server, MariaDB, SQLite auto-migration)
- 3 sets of configuration examples
- Complexity in `DatabaseServiceExtensions.cs`

### Reality Check
- **Production deployment** will use MariaDB (open source, cloud-friendly, scalable)
- **Development** can use MariaDB locally via Docker or native install
- **Testing** can use MariaDB with Testcontainers
- SQLite limitations (single writer, no real concurrency) make it unsuitable even for staging
- SQL Server licensing costs make it impractical for indie/small team projects

### Simplification Benefits
? Single set of repositories  
? Single DbContext implementation  
? Single set of migration scripts  
? Simpler configuration  
? Easier onboarding  
? Reduced NuGet package dependencies  

---

## Estimated Impact

**Files to delete:** ~70 files  
**Files to modify:** ~15 files  
**Effort:** 2 days (1 engineer)  
**Risk:** Low (all changes are deletions, not logic changes)

---

**Next Step:** Should I proceed with the consolidation?
