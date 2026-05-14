using Basecamp_Backend.Data;
using Basecamp_Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Must be set BEFORE builder is created
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ── PORT: Railway injects this dynamically ────────────────────────
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}
else if (builder.Environment.IsProduction())
{
    builder.WebHost.UseUrls("http://0.0.0.0:8080");
}

// ── PostgreSQL: parse DATABASE_URL → Npgsql connection string ─────
var connectionString = BuildConnectionString(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    }));

// ── Identity ──────────────────────────────────────────────────────
builder.Services.AddIdentity<AppUser, IdentityRole>(opt =>
{
    opt.User.RequireUniqueEmail = true;
    opt.Password.RequiredLength = 8;
    opt.Lockout.AllowedForNewUsers = true;
    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(3);
    opt.Lockout.MaxFailedAccessAttempts = 3;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();

var app = builder.Build();

// ── Auto-migrate + seed ───────────────────────────────────────────
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying EF Core migrations...");
        await db.Database.MigrateAsync();
        logger.LogInformation("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration failed!");
        throw;
    }

    string[] roles = ["Admin", "Member"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            logger.LogInformation("Created role: {Role}", role);
        }
    }

    var adminUsers = await userManager.GetUsersInRoleAsync("Admin");
    if (!adminUsers.Any())
    {
        var firstUser = await userManager.Users
            .OrderBy(u => u.UserName)
            .FirstOrDefaultAsync();

        if (firstUser is not null)
        {
            await userManager.AddToRoleAsync(firstUser, "Admin");
            logger.LogInformation("Promoted {User} to Admin.", firstUser.UserName);
        }
    }
}

// ── Middleware pipeline ───────────────────────────────────────────
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Railway healthcheck pings this endpoint
app.MapHealthChecks("/health");

app.MapStaticAssets();

app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=dashboard}/{action=Index}/{id?}");

await app.RunAsync();

// ─────────────────────────────────────────────────────────────────
// Builds a valid Npgsql connection string from any input format.
//
// Railway provides DATABASE_URL as:
//   postgresql://user:password@host:port/database
//
// Npgsql does NOT accept URI format — must be key=value pairs.
// This function handles both formats safely.
// ─────────────────────────────────────────────────────────────────
static string BuildConnectionString(IConfiguration config)
{
    // Priority 1: Railway DATABASE_URL (URI format)
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrWhiteSpace(databaseUrl))
    {
        return ParsePostgresUri(databaseUrl);
    }

    // Priority 2: Individual Railway PG variables
    var pgHost = Environment.GetEnvironmentVariable("PGHOST");
    var pgPort = Environment.GetEnvironmentVariable("PGPORT") ?? "5432";
    var pgUser = Environment.GetEnvironmentVariable("PGUSER");
    var pgPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
    var pgDatabase = Environment.GetEnvironmentVariable("PGDATABASE");

    if (!string.IsNullOrWhiteSpace(pgHost) && !string.IsNullOrWhiteSpace(pgUser))
    {
        return $"Host={pgHost};Port={pgPort};Database={pgDatabase};" +
               $"Username={pgUser};Password={pgPassword};" +
               $"SSL Mode=Require;Trust Server Certificate=true";
    }

    // Priority 3: appsettings.json (development fallback)
    var appSettingsCs = config.GetConnectionString("Default");
    if (!string.IsNullOrWhiteSpace(appSettingsCs))
    {
        return appSettingsCs;
    }

    throw new InvalidOperationException(
        "No PostgreSQL connection string found. " +
        "Set DATABASE_URL environment variable in Railway.");
}

static string ParsePostgresUri(string uri)
{
    // Handles both postgres:// and postgresql:// schemes
    if (!uri.StartsWith("postgres://") && !uri.StartsWith("postgresql://"))
        return uri; // Already in key=value format, return as-is

    // Replace scheme so Uri class parses it correctly
    var normalized = uri.Replace("postgresql://", "http://")
                        .Replace("postgres://", "http://");

    var parsed = new Uri(normalized);
    var userInfo = parsed.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1
                   ? Uri.UnescapeDataString(userInfo[1])
                   : string.Empty;
    var host = parsed.Host;
    var portNum = parsed.Port > 0 ? parsed.Port : 5432;
    var database = parsed.AbsolutePath.TrimStart('/');

    return $"Host={host};Port={portNum};Database={database};" +
           $"Username={username};Password={password};" +
           $"SSL Mode=Require;Trust Server Certificate=true";
}
