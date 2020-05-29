using EventBus.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Catalog.API.IntegrationEvents
{
    public interface ICatalogIntegrationEventService
    {
        Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent @event);
        Task PublishThroughEventBusAsync(IntegrationEvent @event);
    }
}
