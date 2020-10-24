using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos
{
    public interface ICosmosCacheService<T>  where T : CosmosItem
    {
        ValueTask Upsert(string id, string etag, T item);

        ValueTask<T> Get(string id);
        
        ValueTask Clear();
    }
}