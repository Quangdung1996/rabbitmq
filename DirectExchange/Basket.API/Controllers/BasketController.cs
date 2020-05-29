using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Basket.API.Models;
using Basket.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Basket.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BasketController : ControllerBase
    {
        private readonly IBasketRepository _repository;
        private readonly Serilog.ILogger _logger;

        public BasketController(IBasketRepository repository, Serilog.ILogger logger)
        {
            _repository = repository;
            _logger = logger;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(CustomerBasket), (int)HttpStatusCode.OK)]
        public async Task<ActionResult<CustomerBasket>> GetBasketByIdAsync(string id)
        {
            var basket = await _repository.GetBasketAsync(id);

            return Ok(basket ?? new CustomerBasket(id));
        }
    }
}