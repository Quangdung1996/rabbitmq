using EventBus.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EventBus.Abstractions
{
    public interface IIntegrationEventHandler<in TIntegrationEvent>:IIntegrationEventHandler where TIntegrationEvent: IntegrationEvent
    {
        Task HandleAsync(TIntegrationEvent @event);
    }

    public interface IIntegrationEventHandler
    {
    }
}
