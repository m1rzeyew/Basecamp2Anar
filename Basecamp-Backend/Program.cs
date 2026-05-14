using Basecamp_Backend.Data;
using Basecamp_Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// ──────────────────────────────────────────────────────────────────
//  Legacy timestamp behaviour must be set BEFORE the host is built
// ──────────────────────────────────────────────────────────────────
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ──────────────────────────────────────────────────────────────────
//  Railway injects PORT at runtime – Kestrel MUST honour it
// ──────────────────────────────────────────────────────────────────
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// ──────────────────────────────────────────────────────────────────
//  PostgreSQL – prefer the Railway DATABASE_URL env-var when present
// ──────────────────────────────────────────────────────────────────
var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL")          // Railway native URL
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default") // Railway secret mapping
    ?? builder.Configuration.GetConnectionString("Default")     // appsettings fallback
    ?? throw new InvalidOperationException(
           "No PostgreSQL connection string found. " +
           "Set DATABASE_URL or ConnectionStrings__Default in Railway environment variables.");

// Npgsql accepts the postgres:// URI format directly; no manual parsing needed.
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connectionString, npgsql =>
    {
        // Retry transient failures (helpful at cold-start on Railway)
        npgsql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null);
    }));

// ──────────────────────────────────────────────────────────────────
//  ASP.NET Core Identity
// ──────────────────────────────────────────────────────────────────
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

// ──────────────────────────────────────────────────────────────────
//  MVC + Health checks
// ──────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: ["db", "ready"]);

// ──────────────────────────────────────────────────────────────────
//  Build
// ──────────────────────────────────────────────────────────────────
var app = builder.Build();

// ──────────────────────────────────────────────────────────────────
//  Auto-migrate + seed roles/admin on every startup
// ──────────────────────────────────────────────────────────────────
await using (var scope = app.Services.CreateAsyncScope())
{
    var db          = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var logger      = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("Applying EF Core migrations…");
        await db.Database.MigrateAsync();
        logger.LogInformation("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Database migration failed. Application will not start.");
        throw; // Surface the real error – do NOT swallow it silently
    }

    // Seed roles
    string[] roles = ["Admin", "Member"];
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            logger.LogInformation("Created role: {Role}", role);
        }
    }

    // Promote first registered user to Admin if no Admin exists yet
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

// ──────────────────────────────────────────────────────────────────
//  Middleware pipeline
// ──────────────────────────────────────────────────────────────────

// On Railway TLS is terminated at the edge proxy – do NOT redirect
// to HTTPS inside the container (causes redirect loops).
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Health-check endpoints (used by Railway's health checks / uptime monitors)
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "admin",
    pattern: "{area:exists}/{controller=dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=dashboard}/{action=Index}/{id?}");

await app.RunAsync();
