namespace GestoraWebApi.Services
{
    public interface IService<T>
    {
        Task<T> GetByIdAsync(long id);
        Task AddAsync(T entity);
        Task UpdateAsync(T entity);
        Task DeleteAsync(long id);
    }
}
