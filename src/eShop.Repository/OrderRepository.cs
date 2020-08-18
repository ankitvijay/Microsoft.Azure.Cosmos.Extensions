using System.Threading.Tasks;
using eShop.Domain;
using Microsoft.Azure.Cosmos;

namespace eShop.Repository
{
    public class OrderRepository : IRepository<Order>
    {
        private readonly CosmosClient _cosmosClient;

        public OrderRepository(ICosmosClientFactory cosmosClientFactory)
        {
            _cosmosClient = cosmosClientFactory.GetCosmosClient();
        }

        public async Task Upsert(Order order)
        {
            var container = _cosmosClient.GetContainer(CosmosConstants.Database, CosmosConstants.OrderContainer);
            await container.UpsertItemAsync(order);
        }
    }
}