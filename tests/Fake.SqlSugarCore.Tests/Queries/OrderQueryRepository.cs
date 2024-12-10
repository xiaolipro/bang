using Domain.Aggregates.OrderAggregate;
using Domain.Queries;
using Fake.DomainDrivenDesign.Repositories.SqlSugarCore;

namespace Fake.SqlSugarCore.Tests.Queries;

public class OrderQueries : SugarRootlessRepository<OrderingContext>, IOrderQueries
{
    public async Task<List<OrderSummary>> GetOrderSummaryAsync(Guid orderId)
    {
        var context = await GetDbContextAsync();

        var res = context.Ado.SqlQuery<OrderSummary>(
                @$"SELECT o.[Id] as ordernumber,o.[CreateTime] as [date],o.[OrderStatus] as [status], SUM(oi.units*oi.unitprice) as total
               FROM orders o
               LEFT JOIN orderitems oi ON  o.Id = oi.orderid
               Where o.[Id] = {orderId}
               GROUP BY o.[Id], o.[CreateTime], o.[OrderStatus] 
               ORDER BY o.[Id]")
            .ToList();

        return res;
    }


    public async Task<Order> AddAsync(Order order)
    {
        var context = await GetDbContextAsync();
        var entry = await context.Insertable(order).ExecuteReturnEntityAsync();
        return entry;
    }

    public async Task AddBySqlAsync(Order order)
    {
        var context = await GetDbContextAsync();

        await context.Ado.ExecuteCommandAsync($"INSERT INTO \"cardtypes\" (\"Name\")VALUES ('Alipay');");
    }
}