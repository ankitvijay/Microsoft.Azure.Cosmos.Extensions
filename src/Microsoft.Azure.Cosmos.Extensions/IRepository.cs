using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos
{
    public interface IRepository<T> where T : CosmosItem
    {
        ValueTask Add(T entity);
        ValueTask Upsert(T entity);

        ValueTask Delete(string id);

        ValueTask<T> Get(string id);

        ValueTask<ICollection<T>> Get(ICollection<string> ids);
        
        ValueTask Save();
    }
}