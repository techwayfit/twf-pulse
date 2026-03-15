# SignalR Database Backplane Configuration

## Overview
TechWayFit Pulse includes a custom database-backed SignalR backplane that enables real-time communication across multiple servers in a web farm environment without requiring Redis or Azure SignalR Service.

## How It Works

1. **Message Interception**: When a SignalR message is sent to a group, it's stored in the `SignalRMessages` table
2. **Cross-Server Polling**: A background service on each server polls the database every 500ms for new messages from other servers
3. **Local Broadcast**: Messages from other servers are broadcast to local SignalR clients
4. **Automatic Cleanup**: Old messages are automatically deleted after 5 minutes

## Configuration

### Enable Database Backplane

Add this to your `appsettings.json`:

```json
{
  "SignalR": {
    "UseDatabaseBackplane": true
  }
}
```

### For Azure App Service Web Farms

1. **Enable ARR Affinity** (Sticky Sessions) in Azure Portal:
   - Go to your App Service
   - Navigate to **Configuration** ? **General settings**
   - Set **ARR affinity** to **On**

2. **Scale Out** your App Service:
   - Go to **Scale out (App Service plan)**
   - Increase **Instance count** to 2 or more

### Database Requirements

The backplane uses your existing MariaDB/MySQL database. A migration will create the `SignalRMessages` table with:
- **Id**: Auto-increment primary key
- **GroupName**: SignalR group name (e.g., "SESSION_ABC-DEF-GHI")
- **MethodName**: Hub method name (e.g., "ResponseReceived")
- **PayloadJson**: JSON-serialized message arguments
- **ServerId**: Unique identifier for the server that created the message
- **CreatedAt**: Timestamp when message was created
- **IsProcessed**: Whether this server has processed the message
- **ProcessedAt**: When the message was processed

### Performance Considerations

- **Polling Interval**: 500ms (configurable in `DatabaseBackplaneService.cs`)
- **Message Retention**: 5 minutes (configurable in `DatabaseBackplaneService.cs`)
- **Batch Size**: 100 messages per poll (configurable)
- **Database Load**: Minimal - one SELECT query every 500ms per server, plus INSERT on each SignalR message

### Advantages Over Redis

? **No Additional Infrastructure**: Uses your existing MariaDB database  
? **Zero Configuration**: Just enable in appsettings.json  
? **Cost-Effective**: No Redis hosting costs  
? **Simple Deployment**: No separate backplane service to manage  

### Limitations

?? **Scale Limit**: Suitable for 2-10 servers. For larger deployments, consider Redis or Azure SignalR Service  
?? **Latency**: ~500ms delay for cross-server messages (vs. ~50ms with Redis)  
?? **Database Load**: Adds polling queries to your database  

## Testing Multi-Server Setup Locally

### Using Docker Compose

```yaml
version: '3.8'
services:
  mariadb:
    image: mariadb:latest
    environment:
      MYSQL_ROOT_PASSWORD: YourPassword123
      MYSQL_DATABASE: pulse
ports:
      - "3306:3306"
  
  pulse-server-1:
    build: .
    ports:
      - "5001:80"
    environment:
  - ConnectionStrings__DefaultConnection=Server=mariadb;Database=pulse;User=root;Password=YourPassword123
 - SignalR__UseDatabaseBackplane=true
    depends_on:
      - mariadb
  
  pulse-server-2:
    build: .
ports:
      - "5002:80"
    environment:
      - ConnectionStrings__DefaultConnection=Server=mariadb;Database=pulse;User=root;Password=YourPassword123
      - SignalR__UseDatabaseBackplane=true
    depends_on:
    - mariadb
```

### Verification

1. Create a session on Server 1 (localhost:5001)
2. Join as a participant on Server 2 (localhost:5002)
3. Send a response from Server 2
4. Verify the facilitator on Server 1 receives the update in real-time

## Monitoring

Check the logs for backplane activity:

```
[Information] Database backplane initialized for server: MYSERVER_abc123
[Information] Enabling SignalR database backplane for web farm support
[Information] Cleaned up 42 old SignalR messages
```

## Troubleshooting

### Messages Not Syncing Between Servers

1. **Check Configuration**:
   ```bash
   # Verify setting is enabled
   dotnet user-secrets list | grep SignalR
   ```

2. **Check Database Table**:
   ```sql
   SELECT * FROM SignalRMessages WHERE IsProcessed = 0;
   ```

3. **Check Server Logs**:
   - Look for "Database backplane initialized" message
   - Check for any errors in the polling loop

### High Database Load

- Increase `_pollInterval` from 500ms to 1000ms in `DatabaseBackplaneService.cs`
- Reduce `_messageRetention` from 5 minutes to 2 minutes
- Add database indexes (already included in migration)

## Migration from In-Memory to Database Backplane

1. Update `appsettings.json` with `"UseDatabaseBackplane": true"`
2. Run migration: `dotnet ef database update`
3. Deploy to all servers
4. No downtime required - each server will start using the backplane when deployed

## Disabling the Backplane

Set in `appsettings.json`:

```json
{
  "SignalR": {
    "UseDatabaseBackplane": false
  }
}
```

Or remove the setting entirely (defaults to false).
