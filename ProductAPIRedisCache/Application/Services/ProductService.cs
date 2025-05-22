using ProductAPIRedisCache.Application.Interfaces;
using ProductAPIRedisCache.Domain.Entities;
using ProductAPIRedisCache.Domain.Interfaces;
using ProductAPIRedisCache.Infrastructure.Cache;

namespace ProductAPIRedisCache.Application.Services
{
    public class ProductService(IProductRepository repo, IRedisCacheService cache) : IProductService
    { 
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            var cacheKey = "products_all";
            var cached = await cache.GetAsync<IEnumerable<Product>>(cacheKey);
            if (cached is not null)
                return cached;

            var products = await repo.GetAllAsync();
            await cache.SetAsync(cacheKey, products, TimeSpan.FromMinutes(2));
            return products;
        }
         
        public async Task<Product?> GetByIdAsync(int id)
        {
            //throw new Exception("Test Exception: Service Layer!");

            var cacheKey = $"product_{id}";
            var cached = await cache.GetAsync<Product>(cacheKey);
            if (cached is not null)
                return cached;

            var product = await repo.GetByIdAsync(id);
            if (product is not null)
                await cache.SetAsync(cacheKey, product, TimeSpan.FromMinutes(2));
            return product;
        }

         
        public async Task<Product> CreateAsync(Product product)
        {
            var prod = await repo.CreateAsync(product);
          
            await cache.RemoveAsync("products_all");
            return prod;
        }
         
        public async Task<bool> UpdateAsync(Product product)
        {
            var result = await repo.UpdateAsync(product);
            await cache.RemoveAsync("products_all");
            await cache.RemoveAsync($"product_{product.Id}");
            return result;
        }
         
        public async Task<bool> DeleteAsync(int id)
        {
            var result = await repo.DeleteAsync(id);
            await cache.RemoveAsync("products_all");
            await cache.RemoveAsync($"product_{id}");
            return result;
        }
    }
}
