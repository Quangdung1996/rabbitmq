using EventBus.Events;
using System.Threading.Tasks;

namespace EventBus.Abstractions
{
    public interface IEventBus
    {
        Task PublishAsync(IntegrationEvent @event);

        Task SubscribeAsync<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>;

        Task SubscribeDynamicAsync<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler;

        Task UnsubscribeDynamicAsync<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler;

        Task UnsubscribeAsync<T, TH>()
            where TH : IIntegrationEventHandler<T>
            where T : IntegrationEvent;
    }
}