using Gokt.Application.Interfaces;
using Gokt.Infrastructure.BackgroundServices;
using Gokt.Infrastructure.Messaging;
using Gokt.Infrastructure.Persistence;
using Gokt.Infrastructure.Repositories;
using Gokt.Infrastructure.Services;
using Gokt.Infrastructure.Services.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Gokt.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.EnableRetryOnFailure(3)));

        // Cache — falls back to in-memory when Redis is not configured.
        var redisConnection = ResolveRedisConnectionString(configuration);
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(opts => opts.Configuration = redisConnection);
            services.AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var options = ConfigurationOptions.Parse(redisConnection);
                options.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(options);
            });

            services.AddSingleton<ILocationService, RedisLocationService>();
            services.AddSingleton<IRateLimiter, RedisRateLimiter>();
        }
        else
        {
            services.AddDistributedMemoryCache();
            services.AddSingleton<ILocationService, InMemoryLocationService>();
            services.AddSingleton<IRateLimiter, InMemoryRateLimiter>();
        }

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IDriverRepository, DriverRepository>();
        services.AddScoped<IVehicleRepository, VehicleRepository>();
        services.AddScoped<IRideRequestRepository, RideRequestRepository>();
        services.AddScoped<ITripRepository, TripRepository>();
        services.AddScoped<IPricingRepository, PricingRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // Services
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICacheService, CacheService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IMatchingService, MatchingService>();
        services.AddScoped<AutoMatchingStrategy>();
        services.AddScoped<DriverCodeMatchingStrategy>();
        services.AddHostedService<RideExpiryWorker>();
        services.AddHostedService<OutboxProcessor>();

        // Kafka event publisher
        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        return services;
    }

    private static string? ResolveRedisConnectionString(IConfiguration configuration)
    {
        var direct = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(direct)) return direct;

        var host = configuration["Redis:Host"];
        if (string.IsNullOrWhiteSpace(host)) return null;

        var port = configuration.GetValue("Redis:Port", 6379);
        var password = configuration["Redis:Password"];
        var username = configuration["Redis:Username"];
        var ssl = configuration.GetValue("Redis:Ssl", false);

        var options = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            Ssl = ssl
        };
        options.EndPoints.Add(host, port);

        if (!string.IsNullOrWhiteSpace(password)) options.Password = password;
        if (!string.IsNullOrWhiteSpace(username)) options.User = username;

        return options.ToString();
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
