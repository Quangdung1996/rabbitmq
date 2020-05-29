using Catalog.API.Data;
using EventBus.Abstractions;
using EventBus.Events;
using IntegrationEventLogEF.Services;
using IntegrationEventLogEF.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace Catalog.API.IntegrationEvents
{
    public class CatalogIntegrationEventService : ICatalogIntegrationEventService
    {
        private readonly Func<DbConnection, IIntegrationEventLogService> _integrationEventLogServiceFactory;
        private readonly IEventBus _eventBus;
        private readonly CatalogContext _catalogContext;
        private readonly IIntegrationEventLogService _eventLogService;

        private readonly Serilog.ILogger _logger;
        public CatalogIntegrationEventService(IEventBus eventBus, CatalogContext catalogContext,
                                              Func<DbConnection, IIntegrationEventLogService> integrationEventLogServiceFactory,
                                              Serilog.ILogger logger)
        {
            _catalogContext = catalogContext ?? throw new ArgumentNullException(nameof(catalogContext));
            _integrationEventLogServiceFactory = integrationEventLogServiceFactory ?? throw new ArgumentNullException(nameof(integrationEventLogServiceFactory));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _eventLogService = _integrationEventLogServiceFactory(_catalogContext.Database.GetDbConnection());
            _logger = logger;
        }

        public async Task PublishThroughEventBusAsync(IntegrationEvent @event)
        {
            try
            {
                await _eventLogService.MarkEventAsInProgressAsync(@event.Id);
                await _eventBus.PublishAsync(@event);
                await _eventLogService.MarkEventAsPublishedAsync(@event.Id);
                _logger.Information($"PublishThroughEventBusAsync {@event}");
            }
            catch (Exception ex)
            {
                await _eventLogService.MarkEventAsFailedAsync(@event.Id);
                _logger.Error($"PublishThroughEventBusAsync erorr {@event}");
            }
        }

        public async Task SaveEventAndCatalogContextChangesAsync(IntegrationEvent @event)
        {
            await ResilientTransaction.New(_catalogContext).ExecuteAsync(async () =>
            {
                _logger.Information($"SaveEventAndCatalogContextChangesAsync  {@event}");
                await _catalogContext.SaveChangesAsync();
                await _eventLogService.SaveEventAsync(@event, _catalogContext.Database.CurrentTransaction);
            });
        }
    }
}