using System.Text;
using EShoppingZone.Order.Application.Services;
using EShoppingZone.Order.Infrastructure.Data;
using EShoppingZone.Order.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PostgreSQL - Single Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
            npgsqlOptions.CommandTimeout(30);
            npgsqlOptions.MigrationsHistoryTable("__OrderMigrationsHistory");
        }
    );
});

builder.Services.AddHttpClient<IProfileServiceClient, ProfileServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:ProfileService"] ?? "http://localhost:5001"
    );
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<ICartServiceClient, CartServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:CartService"] ?? "http://localhost:5003"
    );
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:ProductService"] ?? "http://localhost:5002"
    );
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IWalletServiceClient, WalletServiceClient>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ServiceUrls:WalletService"] ?? "http://localhost:5005"
    );
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register HTTP clients for inter-service communication
builder.Services.AddHttpClient<IProfileServiceClient, ProfileServiceClient>();
builder.Services.AddHttpClient<ICartServiceClient, CartServiceClient>();
builder.Services.AddHttpClient<IProductServiceClient, ProductServiceClient>();
builder.Services.AddHttpClient<IWalletServiceClient, WalletServiceClient>();

// Register repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Register services
builder.Services.AddScoped<IOrderService, OrderService>();

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
    await dbContext.Database.MigrateAsync();
}

app.Run();
