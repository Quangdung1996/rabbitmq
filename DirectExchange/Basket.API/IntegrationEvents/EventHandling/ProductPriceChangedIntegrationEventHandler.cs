using Basket.API.IntegrationEvents.Events;
using Basket.API.Models;
using Basket.API.Services;
using EventBus.Abstractions;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API.IntegrationEvents.EventHandling
{
    public class ProductPriceChangedIntegrationEventHandler : IIntegrationEventHandler<ProductPriceChangedIntegrationEvent>
    {
        private readonly IBasketRepository _repository;
        private readonly Serilog.ILogger _logger;

        public ProductPriceChangedIntegrationEventHandler(IBasketRepository repository, Serilog.ILogger logger)
        {
            _repository = repository;
            _logger = logger;
        }
        public async Task HandleAsync(ProductPriceChangedIntegrationEvent @event)
        {
            using (LogContext.PushProperty("IntegrationEventContext", $"{@event.Id}-{Program.AppName}"))
            {
                _logger.Information("----- Handling integration event: {IntegrationEventId} at {AppName} - ({@IntegrationEvent})", @event.Id, Program.AppName, @event);

                var userIds = _repository.GetUsers();

                foreach (var id in userIds)
                {
                    var basket = await _repository.GetBasketAsync(id);

                    await UpdatePriceInBasketItemsAsync(@event.ProductId, @event.NewPrice, @event.OldPrice, basket);
                }
            }
        }
        private async Task UpdatePriceInBasketItemsAsync(int productId, decimal newPrice, decimal oldPrice, CustomerBasket basket)
        {
            var itemsToUpdate = basket?.Items?.Where(x => x.ProductId == productId).ToList();

            if (itemsToUpdate != null)
            {
                _logger.Information("----- ProductPriceChangedIntegrationEventHandler - Updating items in basket for user: {BuyerId} ({@Items})", basket.BuyerId, itemsToUpdate);

                foreach (var item in itemsToUpdate)
                {
                    if (item.UnitPrice == oldPrice)
                    {
                        var originalPrice = item.UnitPrice;
                        item.UnitPrice = newPrice;
                        item.OldUnitPrice = originalPrice;
                    }
                }
                await _repository.UpdateBasketAsync(basket);
            }
        }
    }
}
