using Autofac;
using Basket.API.IntegrationEvents.EventHandling;
using Basket.API.Services;
using EventBus;
using EventBus.Abstractions;
using EventBusRabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MQSerice = EventBusRabbitMQ.EventBusRabbitMQ;

namespace Basket.API.Dependency
{
    public static class CustomExtensionMethods
    {
        public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSwaggerGen(options =>
            {
                options.DescribeAllEnumsAsStrings();
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Basket HTTP API",
                    Version = "v1",
                    Description = "The Basket Microservice HTTP API. This is a Data-Driven/CRUD microservice sample"
                });
            });


            return services;
        }

        public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<BasketSettings>(configuration);

            services.AddSingleton<ConnectionMultiplexer>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<BasketSettings>>().Value;
                var configuration = ConfigurationOptions.Parse(settings.ConnectionString, true);

                configuration.ResolveDns = true;

                return ConnectionMultiplexer.Connect(configuration);
            });
            services.AddTransient<IBasketRepository, RedisBasketRepository>();

            return services;

        }

        public static IServiceCollection AddIntegrationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IRabbitMQPersistentConnection>(sp =>
            {

                var logger = new LoggerConfiguration()
                              .Enrich.FromLogContext()
                              .WriteTo.Console()
                              .WriteTo.Seq("http://localhost:5341") // <- Added
                              .CreateLogger();
                var factory = new ConnectionFactory()
                {
                    HostName = configuration["EventBusConnection"],
                    DispatchConsumersAsync = true
                };

                if (!string.IsNullOrEmpty(configuration["EventBusUserName"]))
                {
                    factory.UserName = configuration["EventBusUserName"];
                }

                if (!string.IsNullOrEmpty(configuration["EventBusPassword"]))
                {
                    factory.Password = configuration["EventBusPassword"];
                }

                var retryCount = 5;
                if (!string.IsNullOrEmpty(configuration["EventBusRetryCount"]))
                {
                    retryCount = int.Parse(configuration["EventBusRetryCount"]);
                }

                return new DefaultRabbitMQPersistentConnection(factory, logger, retryCount);
            });
            return services;
        }

        public static IServiceCollection RegisterEventBus(this IServiceCollection services, IConfiguration configuration)
        {
            var subscriptionClientName = configuration["SubscriptionClientName"];
            services.AddSingleton<IEventBus, MQSerice>(sp =>
            {
                var logger = new LoggerConfiguration()
                              .Enrich.FromLogContext()
                              .WriteTo.Console()
                              .WriteTo.Seq("http://localhost:5341") // <- Added
                              .CreateLogger();
                var rabbitMQPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
                var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();

                var eventBusSubcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

                var retryCount = 5;
                if (!string.IsNullOrEmpty(configuration["EventBusRetryCount"]))
                {
                    retryCount = int.Parse(configuration["EventBusRetryCount"]);
                }

                return new MQSerice(rabbitMQPersistentConnection, iLifetimeScope, eventBusSubcriptionsManager,logger, subscriptionClientName, retryCount);
            });

            services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();

            services.AddTransient<ProductPriceChangedIntegrationEventHandler>();
            return services;
        }
    }
}
