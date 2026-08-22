using InventoryHold.Infrastructure.Mongo;
using InventoryHold.Infrastructure.Redis;
using InventoryHold.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StackExchange.Redis;

namespace InventoryHold.WebApi.Configurations;

public static class DependencyManagement
{
    public static IServiceCollection AddRedisDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("ConnectionStrings:Redis is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(connectionString));
        services.AddScoped<IRedisRepository, RedisRepository>();

        return services;
    }

    public static IServiceCollection AddMongoDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Mongo")
            ?? throw new InvalidOperationException("ConnectionStrings:Mongo is not configured.");
        string databaseName = configuration["Mongo:Database"] ?? "inventory-hold";

        services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));
        services.AddSingleton<IMongoDatabase>(serviceProvider =>
            serviceProvider.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
        services.AddScoped<IInventoryMongoRepository, InventoryMongoRepository>();
        services.AddScoped<IHoldMongoRepository, HoldMongoRepository>();
        services.AddScoped<IItemRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<IInventoryMongoRepository>());
        services.AddScoped<IHoldRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<IHoldMongoRepository>());

        return services;
    }
}