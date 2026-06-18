namespace GestoraWebApi.Repositories
{
    public interface IRepository<T>
    {
        Task<T?> GetByIdAsync(long id);
        IQueryable<T> GetAllQueryableAsync();
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
    }
}
