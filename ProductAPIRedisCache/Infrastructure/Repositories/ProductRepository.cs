using Dapper;
using ProductAPIRedisCache.Database;
using ProductAPIRedisCache.Domain.Entities;
using ProductAPIRedisCache.Domain.Interfaces;

namespace ProductAPIRedisCache.Infrastructure.Repositories
{
    public class ProductRepository(IDbConnectionFactory connectionFactory) : IProductRepository
    {
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            using var conn = connectionFactory.CreateConnection();
            return await conn.QueryAsync<Product>("SELECT * FROM Products");
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            using var conn = connectionFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Product>(
                "SELECT * FROM Products WHERE Id=@Id", new { Id = id });
        }

        public async Task<Product> CreateAsync(Product product)
        {
            using var conn = connectionFactory.CreateConnection();
            var sql = @"INSERT INTO Products (Name, Description, Price, Stock) 
                    VALUES (@Name, @Description, @Price, @Stock);
                    SELECT CAST(SCOPE_IDENTITY() as int)";

            var newId = await conn.QuerySingleAsync<int>(sql, product);
            product.Id = newId;
            return product;

        } 
        public async Task<bool> UpdateAsync(Product product)
        {
            //throw new Exception("Test Exception: Repository Layer!");

            using var conn = connectionFactory.CreateConnection();

            var sql = @"UPDATE Products SET 
                    Name = @Name, 
                    Description = @Description, 
                    Price = @Price, 
                    Stock = @Stock 
                    WHERE Id = @Id";
            var affected = await conn.ExecuteAsync(sql, product);
            return affected > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = connectionFactory.CreateConnection();
            var affected = await conn.ExecuteAsync("DELETE FROM Products WHERE Id = @Id", new { Id = id });
            return affected > 0;
        }

    }
}
