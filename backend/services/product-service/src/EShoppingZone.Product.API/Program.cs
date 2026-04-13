using System.Text;
using EShoppingZone.Product.Application.Services;
using EShoppingZone.Product.Infrastructure.Data;
using EShoppingZone.Product.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL - Single Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Register repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Register services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// Configure JWT Authentication
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
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors();

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

// Apply migrations instead of EnsureCreatedAsync
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Force-load the Infrastructure assembly so EF can find migrations
        var infraAssembly =
            typeof(EShoppingZone.Product.Infrastructure.Data.ApplicationDbContext).Assembly;
        Console.WriteLine($"[DB INFO] Loaded assembly: {infraAssembly.GetName().Name}");

        var migrations = dbContext.Database.GetMigrations().ToList();
        Console.WriteLine($"[DB INFO] Total Migrations in Assembly: {migrations.Count}");

        if (migrations.Count == 0)
        {
            // Fallback: create all tables directly from the EF model
            Console.WriteLine(
                "[DB INFO] No migrations found - using EnsureCreated() to create schema..."
            );
            await dbContext.Database.EnsureCreatedAsync();
            Console.WriteLine("[DB INFO] EnsureCreated() completed - all tables created.");
        }
        else
        {
            var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();
            Console.WriteLine($"[DB INFO] Pending Migrations: {pending.Count}");
            if (pending.Any())
            {
                Console.WriteLine("Applying migrations...");
                await dbContext.Database.MigrateAsync();
                Console.WriteLine("Migration successful!");
            }
            else
            {
                Console.WriteLine("No pending migrations.");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"DB SETUP FAILED: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"Inner: {ex.InnerException.Message}");
    }
}
app.Run();
