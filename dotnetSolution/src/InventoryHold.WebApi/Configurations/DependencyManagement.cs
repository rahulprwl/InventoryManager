using InventoryHold.Infrastructure.Mongo;
using InventoryHold.Infrastructure.RabbitMq;
using InventoryHold.Infrastructure.Redis;
using InventoryHold.Domain.Repositories;
using InventoryHold.Domain.Services;
using InventoryHold.Domain.Adapters;
using InventoryHold.WebApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using InventoryHold.Contracts.DTO;
using RabbitMQ.Client;
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
        services.AddScoped<IHoldStateAdapter, HoldStateAdapter>();
        services.AddScoped<IHoldLockAdapter, HoldLockAdapter>();

        return services;
    }

    public static IServiceCollection AddRabbitMqDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        IConfigurationSection rabbitMq = configuration.GetSection("RabbitMQ");
        string host = rabbitMq["Host"]
            ?? throw new InvalidOperationException("RabbitMQ:Host is not configured.");
        string username = rabbitMq["Username"]
            ?? throw new InvalidOperationException("RabbitMQ:Username is not configured.");
        string password = rabbitMq["Password"]
            ?? throw new InvalidOperationException("RabbitMQ:Password is not configured.");

        var connectionFactory = new ConnectionFactory
        {
            HostName = host,
            Port = rabbitMq.GetValue("Port", 5672),
            UserName = username,
            Password = password,
            VirtualHost = rabbitMq.GetValue("VirtualHost", "/")
        };

        services.AddSingleton(connectionFactory);
        services.AddSingleton<IRabbitMqConnector, RabbitMqConnector>();
        services.AddScoped<IHoldMessageAdapter, HoldMessageAdapter>();

        return services;
    }

    public static IServiceCollection AddMongoDependencies(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        if (!BsonClassMap.IsClassMapRegistered(typeof(Item)))
        {
            BsonClassMap.RegisterClassMap<Item>(classMap =>
            {
                classMap.AutoMap();
                classMap.SetIgnoreExtraElements(true);
            });
        }

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
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IHoldRepository>(serviceProvider =>
            serviceProvider.GetRequiredService<IHoldMongoRepository>());
        services.AddScoped<IHoldService, HoldService>();
        services.AddHostedService<HoldExpiryWorker>();

        return services;
    }
}