using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Load YARP configuration
builder
    .Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add JWT Authentication
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
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"] ?? "YourSecretKeyHereAtLeast32CharactersLong!"
                )
            ),
        };
    });

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:3000", "http://localhost:5173")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    );
});

// Add health checks
builder.Services.AddHealthChecks();

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<
        HttpContext,
        string
    >(httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Request.Headers.Host.ToString(),
            factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1),
            }
        )
    );
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Health check endpoint
app.MapHealthChecks(
    "/health",
    new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    status = report.Status.ToString(),
                    gateway = "YARP Gateway",
                    timestamp = DateTime.UtcNow,
                    services = new
                    {
                        profile = "http://localhost:5001/health",
                        product = "http://localhost:5002/health",
                        cart = "http://localhost:5003/health",
                        order = "http://localhost:5004/health",
                        wallet = "http://localhost:5005/health",
                    },
                }
            );
            await context.Response.WriteAsync(result);
        },
    }
);

// Map reverse proxy
app.MapReverseProxy();

app.Run();
