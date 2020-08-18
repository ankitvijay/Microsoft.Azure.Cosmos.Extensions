using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace eShop.Repository
{
    public interface ICosmosClientFactory
    {
        public CosmosClient GetCosmosClient();

        public Task EnsureDbSetup();
    }
}