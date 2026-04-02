using PayFlow.Infrastructure.Persistence;
using PayFlow.Worker.Workers;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// ─── Serilog ────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// ─── Database ───────────────────────────────────────────────────────────────
builder.Services.AddDbContext<PayFlowDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("PayFlow"),
        sql => sql.MigrationsAssembly("PayFlow.Infrastructure")));

// ─── Workers ────────────────────────────────────────────────────────────────
builder.Services.AddHostedService<NotificationWorker>();
builder.Services.AddHostedService<ReconciliationWorker>();

// ─── Health Checks ──────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddSqlServer(
        builder.Configuration.GetConnectionString("PayFlow") ?? string.Empty,
        name: "sqlserver");

var host = builder.Build();
host.Run();
