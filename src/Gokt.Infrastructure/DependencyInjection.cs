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

        // Cache — falls back to in-memory if Redis is not configured
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            services.AddStackExchangeRedisCache(opts => opts.Configuration = redisConnection);
            // Register IConnectionMultiplexer for direct GEO / SET NX / rate-limit operations
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConnection));
        }
        else
        {
            services.AddDistributedMemoryCache(); // Development fallback
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
        services.AddSingleton<ILocationService, RedisLocationService>();
        services.AddSingleton<IRateLimiter, RedisRateLimiter>();
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

    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
