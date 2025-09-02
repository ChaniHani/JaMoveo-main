using JaMoveo.Application.Hubs;
using JaMoveo.Application.Interfaces;
using JaMoveo.Application.Providers;
using JaMoveo.Application.Services;
using JaMoveo.Core.Interfaces;
using JaMoveo.Core.Repositories;
using JaMoveo.Core.Services;
using JaMoveo.Infrastructure.Data;
using JaMoveo.Infrastructure.Entities;
using JaMoveo.Infrastructure.Enums;
using JaMoveo.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Diagnostics.Metrics;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


// ASP.NET Core Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
{
    // Password settings
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    // User settings
    options.User.RequireUniqueEmail = false;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+אבגדהוזחטיכלמנסעפצקרשתךםןףץ";

    // Sign in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();



// Repository Layer - Unit of Work Pattern
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ISongRepository, SongRepository>();
builder.Services.AddScoped<IRehearsalSessionRepository, RehearsalSessionRepository>();


// Service Layer - Business Logic


builder.Services.AddScoped<ISongService, SongService>();
builder.Services.AddScoped<IRehearsalService, RehearsalService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IExternalSongProvider, Tab4UProvider>();

builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();


// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };

    // For SignalR Authentication
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/rehearsalhub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Authorization with Roles
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("PlayerOnly", policy => policy.RequireRole("Player"));
    options.AddPolicy("AdminOrPlayer", policy => policy.RequireRole("Admin", "Player"));
});

// SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://jamoveo-frontend.onrender.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Logging
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();

    if (builder.Environment.IsProduction())
    {
        logging.SetMinimumLevel(LogLevel.Information);
    }
    else
    {
        logging.SetMinimumLevel(LogLevel.Debug);
    }
});

// Health Checks
builder.Services.AddHealthChecks();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AllowAngularApp");
app.MapHub<RehearsalHub>("/rehearsalhub");
app.UseAuthentication(); // Must be before UseAuthorization
app.UseAuthorization();
app.MapControllers();

// Health Check endpoint
app.MapHealthChecks("/health");
// Initialize database and Identity
await InitializeDatabaseAsync(app);

app.Run();


// Database and Identity Initialization Method
static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Apply pending migrations
        await context.Database.MigrateAsync();
        logger.LogInformation("מיגרציות בסיס הנתונים הושלמו בהצלחה");

        // Seed Roles if they don't exist
        await SeedRolesAsync(roleManager, logger);

        // Seed Demo Users for development
        if (app.Environment.IsDevelopment())
        {
            await SeedDemoUsersAsync(userManager, logger);
        }

        logger.LogInformation("בסיס הנתונים אותחל בהצלחה");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "שגיאה באתחול בסיס הנתונים");
        throw;
    }
}


// Seed Roles
static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager, ILogger logger)
{
    var roles = new[] { "Admin", "Player" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole<int> { Name = role });
            if (result.Succeeded)
            {
                logger.LogInformation("תפקיד נוצר בהצלחה: {Role}", role);
            }
            else
            {
                logger.LogError("שגיאה ביצירת תפקיד {Role}: {Errors}",
                    role, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}

// Seed Demo Users for Development
static async Task SeedDemoUsersAsync(UserManager<ApplicationUser> userManager, ILogger logger)
{
    // Demo Admin User
    var adminEmail = "admin";
    var adminUser = await userManager.FindByNameAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Role = UserRole.Admin,
            Instrument = EInstrument.Guitar,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, "123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            logger.LogInformation("משתמש מנהל לדוגמה נוצר: {Username}", adminEmail);
        }
        else
        {
            logger.LogError("שגיאה ביצירת מנהל לדוגמה: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    // Demo Player User  
    var playerEmail = "player1";
    var playerUser = await userManager.FindByNameAsync(playerEmail);

    if (playerUser == null)
    {
        playerUser = new ApplicationUser
        {
            UserName = playerEmail,
            Role = UserRole.Player,
            Instrument = EInstrument.Singers,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await userManager.CreateAsync(playerUser, "123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(playerUser, "Player");
            logger.LogInformation("משתמש נגן לדוגמה נוצר: {Username}", playerEmail);
        }
        else
        {
            logger.LogError("שגיאה ביצירת נגן לדוגמה: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }


    var playerEmail2 = "player2";
    var playerUser2 = await userManager.FindByNameAsync(playerEmail);

    if (playerUser == null)
    {
        playerUser = new ApplicationUser
        {
            UserName = playerEmail,
            Role = UserRole.Player,
            Instrument = EInstrument.Bass,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await userManager.CreateAsync(playerUser, "123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(playerUser, "Player");
            logger.LogInformation("משתמש נגן לדוגמה נוצר: {Username}", playerEmail);
        }
        else
        {
            logger.LogError("שגיאה ביצירת נגן לדוגמה: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}