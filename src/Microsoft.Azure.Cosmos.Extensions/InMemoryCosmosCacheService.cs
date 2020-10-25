using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.Cosmos
{
    public class InMemoryCosmosCacheService<T> : ICosmosCacheService<T> where T : CosmosItem
    {
        private readonly IDictionary<(string,string), T> _cachedItems = new ConcurrentDictionary<(string, string), T>();

        public ValueTask Upsert(string id, string etag, T item)
        {
            if (item != null && item.Id != id)
            {
                throw new InvalidOperationException("Item id does not match key id");
            }

            var cachedItem = _cachedItems.Keys.FirstOrDefault(e => e.Item1 == id);

            if (cachedItem.Item1 != null)
            {
                _cachedItems.Remove(cachedItem);
            }

            _cachedItems[(id, etag)] = item;
            return new ValueTask();
        }

        public ValueTask<T> Get(string id)
        {
            var cachedItemKey = _cachedItems.Keys.FirstOrDefault(e => e.Item1 == id);

            return cachedItemKey.Item1 == null
                ? new ValueTask<T>(default(T))
                : new ValueTask<T>(_cachedItems[cachedItemKey]);
        }

        public ValueTask Clear()
        {
            _cachedItems.Clear();
            return new ValueTask();
        }
    }
}