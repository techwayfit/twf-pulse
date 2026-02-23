# Database Configuration Examples

This folder contains example configuration files for different database providers. Copy the appropriate example file and customize it for your environment.

## Quick Start

### Option 1: Local Development (Recommended)

Create `appsettings.local.json` (already in .gitignore):

```json
{
  "Pulse": {
  "UseInMemory": false,
    "DatabaseProvider": "Sqlite"
  },
  "ConnectionStrings": {
    "PulseDb": "Data Source=pulse.db"
  }
}
```

### Option 2: Environment-Specific Configuration

Copy one of the example files and remove `.example` extension:

```bash
# For SQLite
cp appsettings.Sqlite.json.example appsettings.Development.json

# For SQL Server
cp appsettings.SqlServer.json.example appsettings.Production.json

# For MariaDB/MySQL
cp appsettings.MariaDB.json.example appsettings.Production.json

# For InMemory (testing)
cp appsettings.InMemory.json.example appsettings.Testing.json
```

## Database Provider Options

### 1. SQLite (Default - Development)

**File**: `appsettings.Sqlite.json.example`

**Use Case**: Local development, small deployments, embedded scenarios

**Configuration**:
```json
{
  "Pulse": {
    "UseInMemory": false,
    "DatabaseProvider": "Sqlite"
  },
  "ConnectionStrings": {
    "PulseDb": "Data Source=pulse.db"
  }
}
```

**Features**:
- ? No installation required
- ? File-based database
- ? Good for 1-100 concurrent users
- ? Automatic schema creation
- ? Limited to single server

**Database Location**: `src/TechWayFit.Pulse.Web/pulse.db`

---

### 2. SQL Server (Production)

**File**: `appsettings.SqlServer.json.example`

**Use Case**: Production deployments, enterprise scenarios, high concurrency

**Configuration**:
```json
{
  "Pulse": {
    "UseInMemory": false,
    "DatabaseProvider": "SqlServer"
  },
  "ConnectionStrings": {
    "PulseDb": "Server=localhost;Database=TechWayFitPulse;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

**Connection String Options**:

**Windows Authentication** (Recommended for development):
```
Server=localhost;Database=TechWayFitPulse;Integrated Security=true;TrustServerCertificate=true;
```

**SQL Authentication**:
```
Server=localhost;Database=TechWayFitPulse;User Id=sa;Password=YourPassword;TrustServerCertificate=true;
```

**Azure SQL**:
```
Server=tcp:yourserver.database.windows.net,1433;Database=TechWayFitPulse;User Id=yourusername;Password=yourpassword;Encrypt=True;
```

**Features**:
- ? Scales to 1000+ concurrent users
- ? Advanced query optimization
- ? Enterprise security features
- ? Backup and high availability
- ? Requires SQL Server installation
- ?? Manual schema setup required

**Setup Steps**:
1. Install SQL Server (Express, Standard, or Enterprise)
2. Create database: `CREATE DATABASE TechWayFitPulse`
3. Run schema scripts from `src/TechWayFit.Pulse.Infrastructure/Scripts/MSQL/V1.0/`
4. Update connection string

---

### 3. MariaDB/MySQL (Production)

**File**: `appsettings.MariaDB.json.example`

**Use Case**: Production deployments, cloud hosting, cross-platform scenarios

**Configuration**:
```json
{
  "Pulse": {
    "UseInMemory": false,
    "DatabaseProvider": "MariaDB"
  },
  "ConnectionStrings": {
    "PulseDb": "Server=localhost;Port=3306;Database=pulse;Uid=pulseuser;Pwd=yourpassword;SslMode=Preferred;"
  }
}
```

**Connection String Options**:

**Local Development**:
```
Server=localhost;Port=3306;Database=pulse;Uid=root;Pwd=yourpassword;
```

**With SSL** (recommended for production):
```
Server=localhost;Port=3306;Database=pulse;Uid=pulseuser;Pwd=yourpassword;SslMode=Required;
```

**Azure Database for MySQL**:
```
Server=yourserver.mysql.database.azure.com;Port=3306;Database=pulse;Uid=pulseuser@yourserver;Pwd=yourpassword;SslMode=Required;
```

**AWS RDS for MariaDB/MySQL**:
```
Server=yourinstance.region.rds.amazonaws.com;Port=3306;Database=pulse;Uid=pulseuser;Pwd=yourpassword;SslMode=Required;
```

**Features**:
- ? Scales to 1000+ concurrent users
- ? Open source and free
- ? Cross-platform support
- ? Cloud-friendly (AWS, Azure, GCP)
- ? Strong community support
- ?? Manual schema setup required

**Setup Steps**:
1. Install MariaDB 10.3+ or MySQL 8.0+
2. Create database: `CREATE DATABASE pulse CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;`
3. Run schema scripts from `src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.0/`
4. Update connection string

**Compatibility**:
- MariaDB 10.3 or later (recommended 10.5+)
- MySQL 8.0 or later

---

### 4. InMemory (Testing)

**File**: `appsettings.InMemory.json.example`

**Use Case**: Unit tests, integration tests, CI/CD pipelines

**Configuration**:
```json
{
  "Pulse": {
    "UseInMemory": true,
    "DatabaseProvider": "Sqlite"
  }
}
```

**Features**:
- ? No database setup required
- ? Fast test execution
- ? Automatic cleanup between tests
- ? Data lost on restart
- ? Not suitable for production

**Use For**:
- Automated testing
- Quick demos
- Development without persistence

---

## Configuration Priority

ASP.NET Core loads configuration in this order (later sources override earlier):

1. `appsettings.json` (base configuration)
2. `appsettings.{Environment}.json` (e.g., `appsettings.Development.json`)
3. `appsettings.local.json` (developer-specific, gitignored)
4. User Secrets (Development only)
5. Environment Variables
6. Command-line arguments

### Recommended Approach

**For Local Development**:
- Use `appsettings.local.json` (already gitignored)
- Keeps personal settings out of source control
- No risk of committing credentials

**For Deployments**:
- Use `appsettings.{Environment}.json` in source control
- Override sensitive values with Environment Variables
- Use Azure Key Vault or AWS Secrets Manager for production

---

## Provider-Specific Features

### SQLite

**Repositories**: Located in `Persistence/Sqlite/Repositories/`
- Inherits from base repository classes
- Uses server-side sorting
- Automatic migrations (EnsureCreated)

**Performance**:
- Great for < 100 concurrent users
- Single-threaded writes
- Read concurrency supported

### SQL Server

**Repositories**: Located in `Persistence/SqlServer/Repositories/`
- Optimized pagination (OFFSET/FETCH)
- Bulk delete operations (ExecuteDeleteAsync)
- Server-side sorting and filtering

**Performance**:
- Scales to 1000+ concurrent users
- Parallel query execution
- Advanced indexing strategies

### MariaDB/MySQL

**Repositories**: Located in `Persistence/MariaDb/Repositories/`
- Optimized pagination (LIMIT/OFFSET)
- Bulk delete operations (ExecuteDeleteAsync)
- Server-side sorting and filtering

**Performance**:
- Scales to 1000+ concurrent users
- InnoDB engine with ACID compliance
- Excellent for cloud deployments
- Row-level locking
- Advanced indexing strategies

---

## Environment Variables (Alternative)

Instead of config files, use environment variables:

### SQLite
```bash
export Pulse__UseInMemory=false
export Pulse__DatabaseProvider=Sqlite
export ConnectionStrings__PulseDb="Data Source=pulse.db"
```

### SQL Server
```bash
export Pulse__UseInMemory=false
export Pulse__DatabaseProvider=SqlServer
export ConnectionStrings__PulseDb="Server=localhost;Database=TechWayFitPulse;Integrated Security=true;TrustServerCertificate=true;"
```

### MariaDB/MySQL
```bash
export Pulse__UseInMemory=false
export Pulse__DatabaseProvider=MariaDB
export ConnectionStrings__PulseDb="Server=localhost;Port=3306;Database=pulse;Uid=pulseuser;Pwd=yourpassword;"
```

### InMemory
```bash
export Pulse__UseInMemory=true
export Pulse__DatabaseProvider=Sqlite
```

**Note**: Use double underscores `__` for nested properties.

---

## Switching Providers

### From SQLite to SQL Server

1. **Update configuration**:
   ```json
   {
     "Pulse": {
       "DatabaseProvider": "SqlServer"
},
     "ConnectionStrings": {
       "PulseDb": "Server=localhost;..."
     }
   }
   ```

2. **Create SQL Server database**:
 ```sql
   CREATE DATABASE TechWayFitPulse;
 ```

3. **Run schema scripts**:
   - Located in: `src/TechWayFit.Pulse.Infrastructure/Scripts/MSQL/V1.0/`
 - Run: `00_MasterSetup.sql`

4. **Restart application**

### From SQLite to MariaDB

1. **Update configuration**:
   ```json
   {
     "Pulse": {
       "DatabaseProvider": "MariaDB"
     },
   "ConnectionStrings": {
   "PulseDb": "Server=localhost;Port=3306;Database=pulse;Uid=pulseuser;Pwd=yourpassword;"
     }
   }
   ```

2. **Create MariaDB database**:
   ```sql
   CREATE DATABASE pulse CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
   ```

3. **Run schema scripts**:
   - Located in: `src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.0/`
   - Run: `00_MasterSetup.sql`

4. **Restart application**

### From InMemory to SQLite

1. **Update configuration**:
   ```json
   {
     "Pulse": {
       "UseInMemory": false,
 "DatabaseProvider": "Sqlite"
     }
   }
   ```

2. **Restart application** - database file will be created automatically

---

## Troubleshooting

### SQLite Issues

**Problem**: "Database is locked"
- **Cause**: Multiple processes accessing the same file
- **Solution**: Close other instances or use SQL Server/MariaDB for multi-process scenarios

**Problem**: "Unable to open database file"
- **Cause**: Permission issues
- **Solution**: Check write permissions on the database file directory

### SQL Server Issues

**Problem**: "Login failed for user"
- **Cause**: Authentication failure
- **Solution**: Check username/password or enable Windows Authentication

**Problem**: "Cannot open database"
- **Cause**: Database doesn't exist
- **Solution**: Create database manually or run schema scripts

**Problem**: "Network-related or instance-specific error"
- **Cause**: SQL Server not running or incorrect server name
- **Solution**: Verify SQL Server is running and connection string is correct

### MariaDB/MySQL Issues

**Problem**: "Access denied for user"
- **Cause**: Authentication failure
- **Solution**: Verify username/password and user permissions

**Problem**: "Unknown database 'pulse'"
- **Cause**: Database doesn't exist
- **Solution**: Create database: `CREATE DATABASE pulse CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;`

**Problem**: "Can't connect to MySQL server"
- **Cause**: Server not running or incorrect host/port
- **Solution**: Verify MariaDB/MySQL is running: `sudo systemctl status mariadb`

**Problem**: "SSL connection error"
- **Cause**: SSL mode misconfiguration
- **Solution**: Try `SslMode=Preferred` or `SslMode=None` for local development

### InMemory Issues

**Problem**: "Data disappears after restart"
- **Cause**: Expected behavior - InMemory is ephemeral
- **Solution**: Switch to SQLite, SQL Server, or MariaDB for persistence

---

## Security Best Practices

### ? DO

- Use `appsettings.local.json` for local development
- Store production credentials in Azure Key Vault / AWS Secrets Manager
- Use environment variables in deployment pipelines
- Enable SSL/TLS for SQL Server and MariaDB connections
- Use Windows Authentication when possible (SQL Server)
- Create dedicated database users with minimal permissions
- Add `.local.json` files to `.gitignore`
- Use strong passwords

### ? DON'T

- Commit connection strings with credentials to source control
- Use `sa` or `root` accounts in production
- Store passwords in plain text config files
- Use authentication without encryption
- Share production connection strings in chat/email
- Grant excessive database permissions

---

## Additional Resources

- **Architecture Documentation**: `/docs/repository-reorganization-complete.md`
- **SQL Server Scripts**: `/src/TechWayFit.Pulse.Infrastructure/Scripts/MSQL/V1.0/`
- **MariaDB Scripts**: `/src/TechWayFit.Pulse.Infrastructure/Scripts/MariaDB/V1.0/`
- **Provider Registration**: `/src/TechWayFit.Pulse.Infrastructure/Extensions/DatabaseServiceExtensions.cs`

---

## Quick Reference

| Provider | UseInMemory | DatabaseProvider | Connection String Required | Auto-Migration |
|----------|-------------|------------------|----------------------------|----------------|
| SQLite   | false       | Sqlite           | Yes (file path)   | ? Yes      |
| SQL Server | false     | SqlServer  | Yes (full connection)      | ? Manual      |
| MariaDB  | false       | MariaDB or MySQL | Yes (full connection)      | ? Manual      |
| InMemory | true        | Sqlite           | No| ? Yes       |

---

**Last Updated**: 2025-02-02  
**Workspace**: TechWayFit Pulse - Repository Reorganization Complete
