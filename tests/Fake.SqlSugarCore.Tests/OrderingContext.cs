using System.Reflection;
using SqlSugar;

namespace Fake.SqlSugarCore.Tests;

public class OrderingContext : SugarDbContext<OrderingContext>
{
    public OrderingContext(SugarDbConnOptions<OrderingContext> options) : base(options)
    {
        System.Diagnostics.Debug.WriteLine("OrderingContext::ctor ->" + base.GetHashCode());
    }

    protected override void ConfigureEntityService(PropertyInfo property, EntityColumnInfo column)
    {
        base.ConfigureEntityService(property, column);
    }
}