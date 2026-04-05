using Gokt.Application;
using Gokt.Application.Interfaces;
using Gokt.Hubs;
using Gokt.Infrastructure;
using Gokt.Infrastructure.Messaging;
using Gokt.MatchingWorker.Workers;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [MatchingWorker] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/matching-worker-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog();

    var config = builder.Configuration;

    // ── Redis ────────────────────────────────────────────────────────────────
    var redisConn = config.GetConnectionString("Redis")
        ?? throw new InvalidOperationException("Redis connection string is required");

    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConn));

    // ── SignalR + Redis Backplane ─────────────────────────────────────────────
    // Enables IHubContext<RideHub> to push through shared Redis to API-connected clients
    builder.Services
        .AddSignalR()
        .AddStackExchangeRedis(redisConn, opts =>
            opts.Configuration.ChannelPrefix = RedisChannel.Literal("gokt"));

    // ── Application + Infrastructure ─────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(config);

    // ── IRealtimeService (pushes via Redis backplane to API hubs) ─────────────
    builder.Services.AddScoped<IRealtimeService, SignalRRealtimeService>();

    // ── Kafka consumer ────────────────────────────────────────────────────────
    builder.Services.Configure<KafkaOptions>(config.GetSection("Kafka"));
    builder.Services.AddHostedService<KafkaMatchingConsumer>();

    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MatchingWorker terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
