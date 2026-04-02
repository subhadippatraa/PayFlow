using PayFlow.Api.Filters;
using PayFlow.Api.Middleware;
using PayFlow.Application.Interfaces;
using PayFlow.Application.Services;
using PayFlow.Application.Validators;
using PayFlow.Domain.Interfaces;
using PayFlow.Infrastructure.Auth;
using PayFlow.Infrastructure.Caching;
using PayFlow.Infrastructure.Messaging;
using PayFlow.Infrastructure.Persistence;
using PayFlow.Infrastructure.Persistence.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// ─── Database ───────────────────────────────────────────────────────────────
builder.Services.AddDbContext<PayFlowDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("PayFlow"),
        sql =>
        {
            sql.MigrationsAssembly("PayFlow.Infrastructure");
            sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
        }));

// ─── Redis ──────────────────────────────────────────────────────────────────
var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// ─── Authentication ─────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? "PayFlow-Dev-SecretKey-At-Least-32-Characters-Long!!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "PayFlow",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "PayFlow.Api",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();

// ─── Repositories & Unit of Work ────────────────────────────────────────────
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ILedgerRepository, LedgerRepository>();

// ─── Application Services ───────────────────────────────────────────────────
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ILedgerService, LedgerService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// ─── Messaging ──────────────────────────────────────────────────────────────
builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();
builder.Services.AddHostedService<OutboxPublisher>();

// ─── Validators ─────────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssemblyContaining<TransferRequestValidator>();

// ─── Controllers & Swagger ──────────────────────────────────────────────────
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PayFlow API",
        Version = "v1",
        Description = "Production-grade payment processing service with double-entry bookkeeping, idempotent APIs, and event-driven architecture."
    });

    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─── Health Checks ──────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("PayFlow") ?? string.Empty,
        name: "sqlserver",
        tags: new[] { "ready" })
    .AddRedis(redisConnectionString, name: "redis", tags: new[] { "ready" });

// ─── CORS ───────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// ─── Middleware Pipeline ────────────────────────────────────────────────────
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "PayFlow API v1"));
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<IdempotencyMiddleware>();

// ─── Health Check Endpoints ─────────────────────────────────────────────────
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Live check — just confirms process is running
});

app.MapControllers();

// ─── Auto-migrate in development ────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PayFlowDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();

// Required for integration test WebApplicationFactory
public partial class Program { }
