namespace Fake.SqlSugarCore;

public interface ISugarDbContextProvider<TDbContext> where TDbContext : SugarDbContext<TDbContext>
{
    Task<TDbContext> GetDbContextAsync(CancellationToken cancellationToken);
}