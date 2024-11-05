using Microsoft.Extensions.Configuration;

namespace Fake.Data;

public interface IConnectionStringResolver
{
    Task<string> ResolveAsync(string connectionStringName);
}

public class DefaultConnectionStringResolver(IConfiguration configuration) : IConnectionStringResolver
{
    public virtual Task<string> ResolveAsync(string connectionStringName)
    {
        if (connectionStringName == null) throw new ArgumentNullException(nameof(connectionStringName));
        
        // 从配置文件ConnectionStrings中获取连接字符串
        var connectionString = configuration.GetConnectionString(connectionStringName);

        return Task.FromResult(connectionString!);
    }
}