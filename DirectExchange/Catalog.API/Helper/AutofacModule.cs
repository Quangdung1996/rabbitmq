using Autofac;

namespace Catalog.API.Helper
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
        }
    }

    public static class CustomExtensionMethods
    {
        //public static void AddEventBus(this ContainerBuilder builder, IConfiguration configuration)
        //{
        //    var subscriptionClientName = configuration["SubscriptionClientName"];
        //    builder.RegisterType<DefaultRabbitMQPersistentConnection>().As<IEventBusSubscriptionsManager>();
        //    builder.RegisterType<EventBusRabbitMQ>()
        //    services.AddSingleton<IEventBus, EventBusRabbitMQ.EventBusRabbitMQ>(sp =>
        //    {
        //        var rabbitMQPersistentConnection = sp.GetRequiredService<IRabbitMQPersistentConnection>();
        //        var iLifetimeScope = sp.GetRequiredService<ILifetimeScope>();
        //        var eventBusSubcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

        //        var retryCount = 5;
        //        if (!string.IsNullOrEmpty(configuration["EventBusRetryCount"]))
        //        {
        //            retryCount = int.Parse(configuration["EventBusRetryCount"]);
        //        }

        //        return new EventBusRabbitMQ.EventBusRabbitMQ(rabbitMQPersistentConnection, iLifetimeScope, eventBusSubcriptionsManager, subscriptionClientName, retryCount);
        //    });

        //    services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();

        //    return services;

        //}
    }
}