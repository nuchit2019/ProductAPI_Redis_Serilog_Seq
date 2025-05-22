using ProductAPIRedisCache.Domain.Entities;

namespace ProductAPIRedisCache.Application.Interfaces
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<Product> CreateAsync(Product productDto);
        Task<bool> UpdateAsync(Product productDto);
        Task<bool> DeleteAsync(int id);
    }
}
