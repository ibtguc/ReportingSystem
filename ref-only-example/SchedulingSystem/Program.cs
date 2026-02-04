using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchedulingSystem.Data;
using SchedulingSystem.Models;
using SchedulingSystem.Services;
using SchedulingSystem.Services.Constraints;
using SchedulingSystem.Services.LessonMovement;
using SchedulingSystem.Hubs;
using SchedulingSystem.Filters;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to allow long-running requests (for scheduling algorithms)
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
});

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // Require authentication for all Admin pages
    options.Conventions.AuthorizeFolder("/Admin");

    // Allow anonymous access to Auth pages (login, verify, logout)
    options.Conventions.AllowAnonymousToFolder("/Auth");

    // Allow anonymous access to root pages (if any)
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Privacy");
    options.Conventions.AllowAnonymousToPage("/Error");
}).AddMvcOptions(options =>
{
    // Add automatic backup filter to ensure daily backups on data modifications
    options.Filters.Add<AutomaticBackupFilter>();
});

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Register application services
builder.Services.AddScoped<SchedulingService>();
builder.Services.AddScoped<SchedulingServiceEnhanced>();
builder.Services.AddScoped<SchedulingServiceSimulatedAnnealing>();
builder.Services.AddScoped<UntisImportService>();
builder.Services.AddScoped<TimetableConflictService>();
builder.Services.AddScoped<DatabaseValidationService>();

// Register constraint validation service
builder.Services.AddScoped<IConstraintValidator, ConstraintValidatorService>();
builder.Services.AddScoped<ConstraintValidatorService>();

// Register lesson movement services
builder.Services.AddScoped<AvailableSlotFinder>();
builder.Services.AddScoped<SwapChainSolver>();
builder.Services.AddScoped<LessonMovementService>();
builder.Services.AddScoped<KempeChainTabuSearch>();
builder.Services.AddScoped<MultiStepKempeChainTabuSearch>();
builder.Services.AddScoped<MusicalChairsAlgorithm>();
builder.Services.AddScoped<RecursiveConflictResolutionAlgorithm>();
builder.Services.AddScoped<SimpleTimetableGenerationService>();

// Register substitution services
builder.Services.AddScoped<SubstitutionService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<NotificationService>();

// Register authentication service
builder.Services.AddScoped<MagicLinkService>();

// Register backup service
builder.Services.AddScoped<DatabaseBackupService>();

// Register background service for daily automatic backups
builder.Services.AddHostedService<DailyBackupHostedService>();

// Configure settings
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<DatabaseValidationOptions>(
    builder.Configuration.GetSection("DatabaseValidation"));

// Configure soft constraint weights and SA config
builder.Services.AddSingleton(SoftConstraintWeights.Default);
builder.Services.AddSingleton(SimulatedAnnealingConfig.Balanced);

// Configure Database
var databaseProvider = builder.Configuration["DatabaseSettings:Provider"];
var connectionString = databaseProvider == "SQLite"
    ? builder.Configuration["DatabaseSettings:ConnectionStrings:SQLite"]
    : builder.Configuration["DatabaseSettings:ConnectionStrings:SqlServer"];

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (databaseProvider == "SQLite")
    {
        options.UseSqlite(connectionString);
    }
    else if (databaseProvider == "SqlServer")
    {
        options.UseSqlServer(connectionString);
    }
});

// Configure Data Protection to persist keys across app restarts/deployments
// This ensures users stay logged in after publishing updates
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keysFolder);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("SchedulingSystem");

// Configure Cookie Authentication (for magic link login)
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        // In development, allow cookies over HTTP; in production, require HTTPS
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdministratorOnly", policy =>
        policy.RequireRole("Administrator"));
});

var app = builder.Build();

// Ensure database is created and apply migrations, then seed
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var configuration = services.GetRequiredService<IConfiguration>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Ensure database is created with current schema (no migrations)
        logger.LogInformation("Ensuring database exists with current schema...");
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("Database is ready.");

        // Validate database schema if enabled
        var validationOptions = configuration.GetSection("DatabaseValidation").Get<DatabaseValidationOptions>()
            ?? new DatabaseValidationOptions();

        if (validationOptions.Enabled)
        {
            logger.LogInformation("Database schema validation is enabled. Running validation...");
            var validationService = services.GetRequiredService<DatabaseValidationService>();
            var validationResult = await validationService.ValidateSchemaAsync();

            if (!validationResult.IsValid)
            {
                logger.LogWarning("Database schema validation failed:");
                foreach (var error in validationResult.Errors)
                {
                    logger.LogWarning("  - {Error}", error);
                }

                if (validationOptions.FailOnMismatch)
                {
                    throw new InvalidOperationException(
                        $"Database schema validation failed. The database structure does not match the DbContext model.\n" +
                        $"{validationResult.GetSummary()}\n\n" +
                        $"To fix this issue:\n" +
                        $"1. Delete the database file (scheduling.db for SQLite) to recreate with correct schema\n" +
                        $"2. OR disable validation in appsettings by setting DatabaseValidation:Enabled to false\n" +
                        $"3. OR set DatabaseValidation:FailOnMismatch to false to allow startup with warnings");
                }
                else
                {
                    logger.LogWarning("Database schema validation failed, but FailOnMismatch is false. Application will continue to start.");
                    logger.LogWarning("Consider deleting the database to recreate it with the correct schema.");
                }
            }

            if (validationResult.Warnings.Any())
            {
                foreach (var warning in validationResult.Warnings)
                {
                    logger.LogInformation("  - {Warning}", warning);
                }
            }
        }
        else
        {
            logger.LogDebug("Database schema validation is disabled in configuration.");
        }

        // Seed the database with initial data
        logger.LogInformation("Seeding database with initial data...");
        await SeedData.InitializeAsync(context);
        await UserSeeder.SeedAdminUsersAsync(context);
        logger.LogInformation("Database seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while ensuring database exists or seeding the database.");
        throw; // Re-throw to prevent application startup if database setup fails
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<TimetableGenerationHub>("/hubs/timetableGeneration");
app.MapHub<DebugRecursiveHub>("/hubs/debugRecursive");

app.Run();
