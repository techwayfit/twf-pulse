# IIS Deployment Troubleshooting - PUT/POST Not Allowed

## Issue: "PUT and POST are not enabled" Error

When deploying to IIS or Azure App Service, you may encounter HTTP 405 (Method Not Allowed) errors for PUT, POST, DELETE, or PATCH requests.

## Root Causes

1. **WebDAV Module** blocking PUT/DELETE verbs
2. **IIS Request Filtering** limiting allowed HTTP verbs
3. **Handler Mappings** not configured for all verbs

---

## Solution 1: Update web.config (DONE ?)

The `web.config` has been updated with explicit verb permissions:

```xml
<security>
  <requestFiltering>
    <verbs allowUnlisted="true">
      <add verb="GET" allowed="true" />
      <add verb="POST" allowed="true" />
      <add verb="PUT" allowed="true" />
      <add verb="DELETE" allowed="true" />
      <add verb="PATCH" allowed="true" />
      <add verb="HEAD" allowed="true" />
      <add verb="OPTIONS" allowed="true" />
    </verbs>
  </requestFiltering>
</security>
```

---

## Solution 2: Disable WebDAV Module

WebDAV commonly blocks PUT and DELETE. Add this to `web.config` inside `<system.webServer>`:

```xml
<!-- Remove WebDAV module if it's blocking PUT/DELETE -->
<modules>
  <remove name="WebDAVModule" />
</modules>

<handlers>
  <remove name="WebDAV" />
  <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
</handlers>
```

---

## Solution 3: IIS Server-Level Configuration

If web.config changes don't work, the restriction might be at the **IIS server level**.

### Option A: IIS Manager (GUI)

1. Open **IIS Manager**
2. Select your site ? **Request Filtering** ? **HTTP Verbs** tab
3. Click **Allow Verb...** in the right panel
4. Add: `POST`, `PUT`, `DELETE`, `PATCH`
5. Restart the site

### Option B: Command Line (PowerShell as Administrator)

```powershell
# Allow all verbs for your application pool
Import-Module WebAdministration

$siteName = "YourSiteName"
$path = "IIS:\Sites\$siteName"

# Remove WebDAV if installed
Remove-WebConfigurationProperty -PSPath $path -Filter "system.webServer/modules" -Name "." -AtElement @{name='WebDAVModule'}

# Allow all verbs
Set-WebConfigurationProperty -PSPath $path -Filter "system.webServer/security/requestFiltering/verbs" -Name "allowUnlisted" -Value $true
```

---

## Solution 4: Azure App Service Configuration

If deploying to **Azure App Service**, add this to your `web.config`:

```xml
<system.webServer>
  <!-- Remove WebDAV for Azure -->
  <modules runAllManagedModulesForAllRequests="false">
    <remove name="WebDAVModule" />
  </modules>
  
  <handlers>
    <remove name="WebDAV" />
    <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
  </handlers>

  <!-- Explicit verb configuration -->
  <security>
    <requestFiltering>
    <verbs allowUnlisted="true">
   <add verb="GET" allowed="true" />
      <add verb="POST" allowed="true" />
        <add verb="PUT" allowed="true" />
        <add verb="DELETE" allowed="true" />
        <add verb="PATCH" allowed="true" />
        <add verb="HEAD" allowed="true" />
        <add verb="OPTIONS" allowed="true" />
      </verbs>
      <requestLimits maxAllowedContentLength="52428800" /> <!-- 50MB -->
    </requestFiltering>
  </security>
</system.webServer>
```

---

## Solution 5: Verify ASP.NET Core Configuration

Ensure your `Program.cs` has proper routing:

```csharp
// Map API controllers first (highest priority)
app.MapControllers(); // ? This should be present

// Map SignalR hub
app.MapHub<WorkshopHub>("/hubs/workshop");

// Map MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

**Already configured correctly in your Program.cs ?**

---

## Testing After Deployment

### Test API Endpoints with cURL

```bash
# Test POST (Create Group)
curl -X POST https://yoursite.com/api/session-groups \
  -H "Content-Type: application/json" \
  -H "Cookie: YourAuthCookie" \
  -d '{"name":"Test Group","level":1}'

# Test PUT (Assign Session to Group)
curl -X PUT https://yoursite.com/api/sessions/ABC123/group \
  -H "Content-Type: application/json" \
  -H "Cookie: YourAuthCookie" \
  -d '{"groupId":"some-guid"}'

# Test DELETE (Delete Group)
curl -X DELETE https://yoursite.com/api/session-groups/some-guid \
  -H "Cookie: YourAuthCookie"
```

### Expected Response

? **200/201/204** - Success  
? **405 Method Not Allowed** - HTTP verb still blocked (try next solution)

---

## Quick Diagnostic Checklist

- [ ] `web.config` has `<verbs allowUnlisted="true">`
- [ ] WebDAV module is removed from `<modules>` and `<handlers>`
- [ ] IIS Request Filtering allows PUT/POST/DELETE
- [ ] Application pool is set to "No Managed Code" (for ASP.NET Core)
- [ ] ASP.NET Core Module V2 is installed on the server
- [ ] Site has been restarted after config changes

---

## Common Deployment Platforms

### IIS on Windows Server
- **Most likely cause**: WebDAV module
- **Fix**: Add WebDAV removal to `web.config` (see Solution 2)

### Azure App Service
- **Most likely cause**: Request Filtering at platform level
- **Fix**: Use Solution 4 configuration

### Docker/Linux
- **Should not have this issue** - No IIS, no WebDAV
- If you see 405 errors, check your reverse proxy (nginx/Apache) configuration

---

## Next Steps

1. **Apply Solution 2** (WebDAV removal) to your `web.config`
2. **Redeploy** the application
3. **Test** the drag-and-drop feature
4. If still failing, **check IIS server-level settings** (Solution 3)

Would you like me to update your `web.config` with the WebDAV removal configuration?
