using Ardalis.GuardClauses;

namespace Microsoft.Azure.Cosmos
{
    internal class CosmosOperation
    {
        public CosmosOperation(CosmosOperationType cosmosOperationType, string itemId)
        {
            Guard.Against.NullOrWhiteSpace(itemId, nameof(itemId));

            OperationType = cosmosOperationType;
            ItemId = itemId;
        }

        public CosmosOperationType OperationType { get; }
        public string ItemId { get; }
    }

    internal enum CosmosOperationType
    {
        Create,
        Upsert,
        Delete
    }
}