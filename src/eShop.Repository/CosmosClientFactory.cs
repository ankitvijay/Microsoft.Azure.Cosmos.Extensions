using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace eShop.Repository
{
    public class CosmosClientFactory : ICosmosClientFactory
    {
        // CosmosDbEmulator Connection string
        private const string AccountEndpoint = "https://localhost:8081";
        private const string AccountAuthKey =
            "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        public CosmosClient GetCosmosClient()
        {
            // Warning: Bad code, read settings from configuration
            return new CosmosClient(AccountEndpoint, AccountAuthKey);
        }

        public async Task EnsureDbSetup()
        {
            var containerProperties = new ContainerProperties
            {
                Id = CosmosConstants.OrderContainer,
                PartitionKeyPath = $"/OrderNumber"
            };
            var cosmosClient = new CosmosClient(AccountEndpoint, AccountAuthKey);
            await cosmosClient.CreateDatabaseIfNotExistsAsync(CosmosConstants.Database);
            var database = cosmosClient.GetDatabase(CosmosConstants.Database);
            await database.CreateContainerIfNotExistsAsync(containerProperties);
        }
    }
}