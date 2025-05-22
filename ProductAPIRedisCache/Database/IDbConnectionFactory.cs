using System.Data;

namespace ProductAPIRedisCache.Database
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
