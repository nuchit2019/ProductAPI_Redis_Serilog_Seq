using ProductAPIRedisCache.Application.Interfaces;
using ProductAPIRedisCache.Domain.Entities;
using ProductAPIRedisCache.Domain.Interfaces;
using ProductAPIRedisCache.Infrastructure.Cache;

namespace ProductAPIRedisCache.Application.Services
{
    public class ProductService(IProductRepository repo, IRedisCacheService cache, ILogger<ProductService> logger) : IProductService
    {
        private static readonly string ProductAllKey = "products_all";
        private static string ProductKey(int id) => $"product_{id}";
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(2);

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            try
            {
                var cached = await cache.GetAsync<IEnumerable<Product>>(ProductAllKey);
                if (cached is not null)
                    return cached!;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis Cache error: GetAllAsync fallback to DB.");
            }

            var products = await repo.GetAllAsync();
            try
            {
                await cache.SetAsync(ProductAllKey, products, DefaultCacheDuration);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis Cache error: SetAllAsync skip caching.");
            }
            return products;
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            var cacheKey = ProductKey(id);
            try
            {
                var cached = await cache.GetAsync<Product>(cacheKey);
                if (cached is not null)
                    return cached!;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis Cache error: GetByIdAsync fallback to DB.");
            }

            var product = await repo.GetByIdAsync(id);
            if (product is not null)
            {
                try
                {
                    await cache.SetAsync(cacheKey, product, DefaultCacheDuration);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Redis Cache error: SetByIdAsync skip caching.");
                }
            }
            return product;
        }

        public async Task<Product> CreateAsync(Product product)
        {
            var prod = await repo.CreateAsync(product);
            await SafeCacheRemove(ProductAllKey);
            return prod;
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            var result = await repo.UpdateAsync(product);
            await SafeCacheRemove(ProductAllKey);
            await SafeCacheRemove(ProductKey(product.Id));
            return result;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await repo.DeleteAsync(id);
            await SafeCacheRemove(ProductAllKey);
            await SafeCacheRemove(ProductKey(id));
            return result;
        }

        private async Task SafeCacheRemove(string key)
        {
            try
            {
                await cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, $"Redis Cache error: RemoveAsync ({key}) skip removal.");
            }
        }
    }


}
