# Railway Performance Monitoring Guide

## ? What Was Implemented

### Enhanced Health Endpoint (`/health`)

The health check now returns comprehensive performance metrics:

```json
{
  "status": "ok",
  "time": "2025-01-21T10:30:00Z",
  "responseTimeMs": 45.32,
  "db": "/data/election.db",
  "dbReady": true,
  "dbSizeMB": 12.45,
  "dbQueryMs": 23.15,
  "voterCount": 1240,
  "memoryUsageMB": 145.67,
  "performance": "good"
}
```

### Performance Thresholds

- **Good**: DB query < 100ms
- **Acceptable**: DB query 100–500ms
- **Slow**: DB query > 500ms

---

## ?? How to Check Railway Performance

### 1. **Via Browser**
Visit: `https://your-railway-domain.com/health`

### 2. **Via Command Line**
```bash
curl https://your-railway-domain.com/health | jq .
```

### 3. **From Railway Dashboard**
- Go to your Railway project
- Click on your service
- Click "Deployments" ? View Logs
- Look for startup logs showing DB path and performance

---

## ?? How to Improve Performance if Slow

### **A. Database Optimizations (Already Applied)**

? **WAL Mode Enabled** – Concurrent reads don't block writes  
? **Performance Indexes Added** – Voters, Grievances, Expenses indexed  
? **Query Filters** – Soft-delete filter on Voters prevents full scans

### **B. Railway Resource Upgrades**

If `dbQueryMs` consistently > 500ms or `memoryUsageMB` > 400:

1. **Upgrade RAM** (Railway dashboard ? Settings ? Service ? Resources)
   - Default: 512MB
   - Recommended: 1GB for 5K+ voters

2. **Add Volume** (for faster DB I/O)
   - Railway dashboard ? Add Volume
   - Mount at `/data`
   - Set `DATABASE_PATH=/data/election.db`

### **C. Code-Level Optimizations**

If specific pages are slow:

1. **Add `.AsNoTracking()` to read-only queries**
   ```csharp
   var voters = await _db.Voters.AsNoTracking().ToListAsync();
   ```

2. **Paginate large lists** (already implemented for voters)

3. **Cache frequently-accessed data**
   ```csharp
   services.AddMemoryCache();
   ```

### **D. Monitor Railway Metrics**

Railway Dashboard ? Metrics shows:
- CPU usage
- Memory usage
- Network I/O
- Disk I/O

---

## ?? Troubleshooting Slow Performance

| Symptom | Likely Cause | Solution |
|---------|--------------|----------|
| `dbQueryMs` > 1000ms | Large DB file (> 50MB) | Add indexes, archive old data |
| `memoryUsageMB` > 450MB | Memory leak or large dataset | Upgrade to 1GB plan |
| `responseTimeMs` > 2000ms | Railway cold start | Enable "Always On" (paid plan) |
| DB file in `/app/` instead of `/data/` | Volume not mounted | Set `DATABASE_PATH=/data/election.db` |

---

## ?? Expected Performance Benchmarks

| Dataset Size | DB Query Time | Memory Usage |
|--------------|---------------|--------------|
| < 1,000 voters | < 50ms | ~120MB |
| 1,000–5,000 voters | 50–150ms | ~180MB |
| 5,000–10,000 voters | 150–300ms | ~250MB |
| 10,000+ voters | 300–500ms | ~350MB+ |

---

## ?? Railway Environment Variables (Performance-Related)

```bash
# Database
DATABASE_PATH=/data/election.db

# Logging (reduce log verbosity in production)
Logging__LogLevel__Default=Warning
Logging__LogLevel__Microsoft.EntityFrameworkCore=Warning

# Connection pooling (optional)
ConnectionStrings__DefaultConnection=Data Source=/data/election.db;Pooling=True;Max Pool Size=100
```

---

## ??? Quick Performance Check Script

```bash
#!/bin/bash
DOMAIN="your-railway-domain.com"

echo "Checking Railway app performance..."
RESPONSE=$(curl -s "https://$DOMAIN/health" -w "\nHTTP_CODE:%{http_code}\nTOTAL_TIME:%{time_total}\n")
echo "$RESPONSE"

# Extract metrics
DB_QUERY_MS=$(echo "$RESPONSE" | jq -r '.dbQueryMs')
PERF_STATUS=$(echo "$RESPONSE" | jq -r '.performance')

if [ "$PERF_STATUS" == "slow" ]; then
  echo "??  WARNING: Application is slow. Consider upgrading resources."
else
  echo "? Performance is $PERF_STATUS (DB query: ${DB_QUERY_MS}ms)"
fi
```

---

## ?? Next Steps

1. Visit `/health` endpoint after Railway deployment
2. Check if `performance` is "good" or "acceptable"
3. If "slow", upgrade Railway plan or optimize queries
4. Monitor logs for `[DB]` startup messages
5. Set up Railway alerting for high memory usage

---

**Last Updated**: January 2025  
**Works with**: Railway + SQLite + .NET 8 + ASP.NET Core
