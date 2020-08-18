using System.Threading.Tasks;

namespace eShop.Repository
{
    public interface IRepository<in T>
    {
        Task Upsert(T entity);
    }
}