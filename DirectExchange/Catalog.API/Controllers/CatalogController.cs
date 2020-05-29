using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Catalog.API.Data;
using Catalog.API.IntegrationEvents;
using Catalog.API.IntegrationEvents.Events;
using Catalog.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Catalog.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        private readonly CatalogContext _catalogContext;
        private readonly ICatalogIntegrationEventService _catalogIntegrationEventService;

        private readonly Serilog.ILogger _logger;
        public CatalogController(CatalogContext catalogContext, ICatalogIntegrationEventService catalogIntegrationEventService, Serilog.ILogger logger)
        {
            _catalogContext = catalogContext ?? throw new ArgumentNullException(nameof(catalogContext));
            _catalogIntegrationEventService = catalogIntegrationEventService ?? throw new ArgumentNullException(nameof(catalogIntegrationEventService));
            
            catalogContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        [HttpGet]
        [Route("items")]
        [ProducesResponseType(typeof(IEnumerable<CatalogItem>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> ItemsAsync([FromQuery]int pageSize = 10, [FromQuery]int pageIndex = 0, string ids = null)
        {
            if (!string.IsNullOrEmpty(ids))
            {
                var items = await GetItemsByIdsAsync(ids);

                if (!items.Any())
                {
                    return BadRequest("ids value invalid. Must be comma-separated list of numbers");
                }

                return Ok(items);
            }

            var totalItems = await _catalogContext.CatalogItems
                .LongCountAsync();

            var itemsOnPage = await _catalogContext.CatalogItems
                .OrderBy(c => c.Name)
                .Skip(pageSize * pageIndex)
                .Take(pageSize)
                .ToListAsync();
            return Ok(itemsOnPage);
        }

        [Route("items")]
        [HttpPost]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public async Task<ActionResult> CreateProductAsync([FromBody]CatalogItem product)
        {
            var item = new CatalogItem
            {
                CatalogBrandId = product.CatalogBrandId,
                CatalogTypeId = product.CatalogTypeId,
                Description = product.Description,
                Name = product.Name,
                PictureFileName = product.PictureFileName,
                Price = product.Price
            };

            _catalogContext.CatalogItems.Add(item);

            await _catalogContext.SaveChangesAsync();

            
            return Ok(item);
          
        }
        [Route("items")]
        [HttpPut]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.Created)]
        public async Task<ActionResult> UpdateProductAsync([FromBody]CatalogItem productToUpdate)
        {
            var catalogItem = await _catalogContext.CatalogItems.SingleOrDefaultAsync(i => i.Id == productToUpdate.Id);

            if (catalogItem == null)
            {
                _logger.Error($"Error not found item {catalogItem}");
                return NotFound(new { Message = $"Item with id {productToUpdate.Id} not found." });
            }

            var oldPrice = catalogItem.Price;
            var raiseProductPriceChangedEvent = oldPrice != productToUpdate.Price;

            // Update current product
            catalogItem = productToUpdate;
            _catalogContext.CatalogItems.Update(catalogItem);

            if (raiseProductPriceChangedEvent) // Save product's data and publish integration event through the Event Bus if price has changed
            {
                _logger.Information($"update item {catalogItem}");
                //Create Integration Event to be published through the Event Bus
                var priceChangedEvent = new ProductPriceChangedIntegrationEvent(catalogItem.Id, productToUpdate.Price, oldPrice);

                // Achieving atomicity between original Catalog database operation and the IntegrationEventLog thanks to a local transaction
                await _catalogIntegrationEventService.SaveEventAndCatalogContextChangesAsync(priceChangedEvent);

                // Publish through the Event Bus and mark the saved event as published
                await _catalogIntegrationEventService.PublishThroughEventBusAsync(priceChangedEvent);
            }
            else // Just save the updated product because the Product's Price hasn't changed.
            {
                await _catalogContext.SaveChangesAsync();
            }

            return Ok(catalogItem);
        }


        [HttpGet]
        [Route("items/{id:int}")]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(CatalogItem), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<CatalogItem>> ItemByIdAsync(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var item = await _catalogContext.CatalogItems.SingleOrDefaultAsync(ci => ci.Id == id);

            if (item != null)
            {
                return item;
            }

            return NotFound();
        }

        private async Task<List<CatalogItem>> GetItemsByIdsAsync(string ids)
        {
            var numIds = ids.Split(',').Select(id => (Ok: int.TryParse(id, out int x), Value: x));

            if (!numIds.All(nid => nid.Ok))
            {
                return new List<CatalogItem>();
            }

            var idsToSelect = numIds
                .Select(id => id.Value);

            var items = await _catalogContext.CatalogItems.Where(ci => idsToSelect.Contains(ci.Id)).ToListAsync();

            return items;
        }
    }
}