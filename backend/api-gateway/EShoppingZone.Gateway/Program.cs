using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    );
});

// Add health checks
builder.Services.AddHealthChecks();

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
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

            // Get addresses from configuration to avoid hardcoded localhost in production
            var profileUrl =
                builder.Configuration[
                    "ReverseProxy:Clusters:profile-cluster:Destinations:destination1:Address"
                ]
                ?? "http://localhost:5001/";
            var productUrl =
                builder.Configuration[
                    "ReverseProxy:Clusters:product-cluster:Destinations:destination1:Address"
                ]
                ?? "http://localhost:5002/";
            var cartUrl =
                builder.Configuration[
                    "ReverseProxy:Clusters:cart-cluster:Destinations:destination1:Address"
                ]
                ?? "http://localhost:5003/";
            var orderUrl =
                builder.Configuration[
                    "ReverseProxy:Clusters:order-cluster:Destinations:destination1:Address"
                ]
                ?? "http://localhost:5004/";
            var walletUrl =
                builder.Configuration[
                    "ReverseProxy:Clusters:wallet-cluster:Destinations:destination1:Address"
                ]
                ?? "http://localhost:5005/";

            var result = System.Text.Json.JsonSerializer.Serialize(
                new
                {
                    status = report.Status.ToString(),
                    gateway = "YARP Gateway",
                    timestamp = DateTime.UtcNow,
                    services = new
                    {
                        profile = profileUrl + "health",
                        product = productUrl + "health",
                        cart = cartUrl + "health",
                        order = orderUrl + "health",
                        wallet = walletUrl + "health",
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
