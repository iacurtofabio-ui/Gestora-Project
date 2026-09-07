namespace GestoraWebApi.Repositories
{
    public interface IRepository<T>
    {
        Task<T?> GetByIdAsync(long id);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
    }
}
