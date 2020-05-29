using Basket.API.Models;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API.Services
{
    public class RedisBasketRepository : IBasketRepository
    {
        private readonly ConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly Serilog.ILogger _logger;

        public RedisBasketRepository(ConnectionMultiplexer redis,Serilog.ILogger logger)
        {
            _redis = redis;
            _logger = logger;
            _database = redis.GetDatabase();
        }

        public IEnumerable<string> GetUsers()
        {
            var server = GetServer();
            var data = server.Keys();

            return data?.Select(k => k.ToString());
        }
        public async Task<bool> DeleteBasketAsync(string id)
        {
            return await _database.KeyDeleteAsync(id);
        }


        public async Task<CustomerBasket> GetBasketAsync(string customerId)
        {
            try
            {
                var test = GetUsers();
                var data = await _database.StringGetAsync(customerId);

                if (data.IsNullOrEmpty)
                {
                    return null;
                }

                return JsonConvert.DeserializeObject<CustomerBasket>(data);
            }
            catch (Exception ex)
            {

                throw;
            }
         
        }

        public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
        {
            var created = await _database.StringSetAsync(basket.BuyerId, JsonConvert.SerializeObject(basket));

            if (!created)
            {
                _logger.Information("Problem occur persisting the item.");
                return null;
            }

            _logger.Information("Basket item persisted succesfully.");

            return await GetBasketAsync(basket.BuyerId);
        }

   
        private IServer GetServer()
        {
            var endpoint = _redis.GetEndPoints();
            return _redis.GetServer(endpoint.First());
        }
    }
}
