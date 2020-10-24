using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Ardalis.GuardClauses;

namespace Microsoft.Azure.Cosmos
{
    public class CosmosRepository<T> : IRepository<T>
        where T : CosmosItem
    {
        private readonly ICosmosCacheService<T> _cosmosCacheService;
        private readonly string _partitionKeyPath;
        private readonly string _partitionKeyValue;
        private readonly Container _container;

        private readonly List<CosmosOperation> _cosmosOperations;

        public CosmosRepository(ICosmosCacheService<T> cosmosCacheService,
            CosmosClient cosmosClient,
            string databaseName,
            string containerName,
            string partitionKeyPath,
            string partitionKeyValue)
        {
            Guard.Against.Null(cosmosCacheService, nameof(cosmosCacheService));
            Guard.Against.Null(cosmosClient, nameof(cosmosClient));
            Guard.Against.Null(databaseName, nameof(databaseName));
            Guard.Against.Null(containerName, nameof(containerName));

            _cosmosCacheService = cosmosCacheService;
            _partitionKeyPath = partitionKeyPath;
            _partitionKeyValue = partitionKeyValue;
            _container = cosmosClient.GetContainer(databaseName, containerName);

            _cosmosOperations = new List<CosmosOperation>();
        }

        public virtual async ValueTask Add(T entity)
        {
            Guard.Against.Null(entity, nameof(entity));

            await _cosmosCacheService.Upsert(entity.Id, entity.ETag, entity);
            _cosmosOperations.Add(new CosmosOperation(CosmosOperationType.Create, entity.Id));
        }

        public virtual async ValueTask Upsert(T entity)
        {
            Guard.Against.Null(entity, nameof(entity));

            await _cosmosCacheService.Upsert(entity.Id, entity.ETag, entity);
            _cosmosOperations.Add(new CosmosOperation(CosmosOperationType.Create, entity.Id));
        }

        public virtual async ValueTask Delete(string id)
        {
            Guard.Against.Null(id, nameof(id));

            var entity = await _cosmosCacheService.Get(id);

            if (entity != null)
            {
                await _cosmosCacheService.Upsert(id, entity.ETag, null);
            }
            else
            {
                await _cosmosCacheService.Upsert(id, null, null);
            }
        }

        public virtual async ValueTask<T> Get(string id)
        {
            Guard.Against.Null(id, nameof(id));

            var fromCache = await _cosmosCacheService.Get(id);

            if (fromCache != null)
            {
                return fromCache;
            }

            ItemResponse<T> itemResponse;
            try
            {
                itemResponse = await _container.ReadItemAsync<T>(id, new PartitionKey(_partitionKeyValue))
                    .ConfigureAwait(false);
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                await _cosmosCacheService.Upsert(id, null, null);
                return default;
            }

            await _cosmosCacheService.Upsert(id, itemResponse.ETag, itemResponse.Resource);
            return await _cosmosCacheService.Get(id);
        }

        public virtual async ValueTask<ICollection<T>> Get(ICollection<string> ids)
        {
            Guard.Against.Null(ids, nameof(ids));

            var result = new List<T>();
            var itemsToQueryFromCosmos = new List<string>();

            foreach (var id in ids)
            {
                var cachedItem = await _cosmosCacheService.Get(id);
                if (cachedItem != null)
                {
                    result.Add(cachedItem);
                }
                else
                {
                    itemsToQueryFromCosmos.Add(id);
                }
            }

            if (itemsToQueryFromCosmos.Count > 0)
            {
                var queryResult = await QueryItemsAndSaveToCache(itemsToQueryFromCosmos);
                result.AddRange(queryResult);
            }

            return result;
        }

        public virtual async ValueTask Save()
        {
            var batch = _container.CreateTransactionalBatch(new PartitionKey(_partitionKeyValue));

            foreach (var operation in _cosmosOperations)
            {
                var item = await _cosmosCacheService.Get(operation.ItemId);

                switch (operation.OperationType)
                {
                    case CosmosOperationType.Create:
                        batch.CreateItem(item, new TransactionalBatchItemRequestOptions()
                        {
                            IfMatchEtag = item.ETag
                        });
                        break;

                    case CosmosOperationType.Upsert:
                        batch.UpsertItem(item, new TransactionalBatchItemRequestOptions
                        {
                            IfMatchEtag = item.ETag
                        });
                        break;

                    case CosmosOperationType.Delete:
                        batch.DeleteItem(item.Id, new TransactionalBatchItemRequestOptions
                        {
                            IfMatchEtag = item.ETag
                        });
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            using (var response = await batch.ExecuteAsync().ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw new CosmosException(response.ErrorMessage, response.StatusCode, 0, response.ActivityId,
                        response.RequestCharge);
                }
            }

            // After save, clear cosmosOperations and cosmosCacheService
            _cosmosOperations.Clear();
            await _cosmosCacheService.Clear();
        }

        private async Task<ICollection<T>> QueryItemsAndSaveToCache(ICollection<string> itemsToQuery)
        {
            var queryDefinition = GetQueryDefinition(itemsToQuery);
            var iterator = _container.GetItemQueryIterator<T>(queryDefinition);
            var result = new List<T>();

            while (iterator.HasMoreResults)
            {
                result.AddRange(await iterator.ReadNextAsync().ConfigureAwait(false));
            }

            foreach (var cosmosItem in result)
            {
                await _cosmosCacheService.Upsert(cosmosItem.Id, cosmosItem.ETag, cosmosItem);
            }

            return result;
        }

        private QueryDefinition GetQueryDefinition(ICollection<string> ids)
        {
            var queryDefinition = new QueryDefinition(
                $"SELECT * FROM c WHERE c.{_partitionKeyPath} = @partitionKey AND ARRAY_CONTAINS(@ids, c.id)");

            queryDefinition.WithParameter("@partitionKey", $"{_partitionKeyValue}");
            queryDefinition.WithParameter("@ids", ids);
            return queryDefinition;
        }
    }
}