using System.Text;
using EShoppingZone.Profile.Application.Services;
using EShoppingZone.Profile.Infrastructure.Data;
using EShoppingZone.Profile.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services
builder
    .Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        );
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Register repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IAddressRepository, AddressRepository>();

builder.Services.AddScoped<IAdminService, AdminService>();

// Configure Google Authentication
builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context
                    .Request.Headers["Authorization"]
                    .FirstOrDefault()
                    ?.Split(" ")
                    .Last();
                if (!string.IsNullOrEmpty(token))
                    context.Token = token;
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Apply migrations safely with deep diagnostics
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        var migrations = dbContext.Database.GetMigrations().ToList();
        var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
        var applied = (await dbContext.Database.GetAppliedMigrationsAsync()).ToList();

        Console.WriteLine($"[DB INFO] Profile - Total Migrations in Assembly: {migrations.Count}");
        Console.WriteLine($"[DB INFO] Profile - Pending Migrations: {pending.Count}");
        Console.WriteLine($"[DB INFO] Profile - Already Applied: {applied.Count}");

        foreach (var m in pending)
            Console.WriteLine($"[DB INFO] Will apply: {m}");

        if (pending.Any())
        {
            Console.WriteLine("Applying Profile database migrations...");
            await dbContext.Database.MigrateAsync();
            Console.WriteLine("Profile migration successful!");
        }
        else
        {
            Console.WriteLine("Profile migration check finished: No pending migrations found.");

            // Check if tables actually exist if 0 migrations were found
            var tableNames = dbContext
                .Model.GetEntityTypes()
                .Select(t => t.GetTableName())
                .ToList();
            Console.WriteLine(
                $"[DB INFO] Profile context expects tables: {string.Join(", ", tableNames)}"
            );
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"MIGRATION FAILED for Profile: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
    }
}

app.Run();
