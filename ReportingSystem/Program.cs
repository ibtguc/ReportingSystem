using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ReportingSystem.Data;
using ReportingSystem.Services;
using ReportingSystem.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    // Require authentication for all Admin and Reports pages
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AuthorizeFolder("/Reports");
    options.Conventions.AuthorizeFolder("/Directives");

    // Allow anonymous access to Auth pages (login, verify, logout)
    options.Conventions.AllowAnonymousToFolder("/Auth");

    // Allow anonymous access to root pages
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Error");
}).AddMvcOptions(options =>
{
    // Add automatic backup filter to ensure daily backups on data modifications
    options.Filters.Add<AutomaticBackupFilter>();
});

// Register application services
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<MagicLinkService>();
builder.Services.AddScoped<DatabaseBackupService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<DirectiveService>();

// Register background service for daily automatic backups
builder.Services.AddHostedService<DailyBackupHostedService>();

// Configure settings
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

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
var keysFolder = Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keysFolder);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder))
    .SetApplicationName("ReportingSystem");

// Configure Cookie Authentication (for magic link login)
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SystemAdminOnly", policy =>
        policy.RequireRole("SystemAdmin"));
});

var app = builder.Build();

// Ensure database is created and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        logger.LogInformation("Ensuring database exists with current schema...");
        await context.Database.EnsureCreatedAsync();
        logger.LogInformation("Database is ready.");

        // Seed the database with initial data
        logger.LogInformation("Seeding database with initial data...");
        await SeedData.InitializeAsync(context);
        await OrganizationSeeder.SeedAsync(context);
        await UserSeeder.SeedAdminUsersAsync(context);
        logger.LogInformation("Database seeding completed.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while ensuring database exists or seeding the database.");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
