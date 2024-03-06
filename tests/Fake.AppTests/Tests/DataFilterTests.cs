﻿using System.Diagnostics;
using Domain.Aggregates.OrderAggregate;
using Fake.Data.Filtering;
using Fake.DomainDrivenDesign.Entities.Auditing;
using Fake.DomainDrivenDesign.Repositories;
using Fake.Modularity;
using Shouldly;
using Xunit;

namespace Tests;

public abstract class DataFilterTests<TStartupModule> : AppTestBase<TStartupModule> where TStartupModule : IFakeModule
{
    protected readonly IDataFilter<ISoftDelete> SoftDeleteDataFilter;
    protected readonly IRepository<Order> OrderRepository;

    public DataFilterTests()
    {
        OrderRepository = ServiceProvider.GetRequiredService<IRepository<Order>>();
        SoftDeleteDataFilter = ServiceProvider.GetRequiredService<IDataFilter<ISoftDelete>>();
    }

    [Fact]
    public async Task 禁用软删过滤()
    {
        await SoftDeleteAsync();

        using (SoftDeleteDataFilter.Disable())
        {
            var order = await OrderRepository.FirstOrDefaultAsync(x => x.Id == AppTestDataBuilder.OrderId);
            order.ShouldNotBeNull();
        }

        var order2 = await OrderRepository.FirstOrDefaultAsync(x => x.Id == AppTestDataBuilder.OrderId);
        order2.ShouldBeNull();
    }

    private async Task SoftDeleteAsync()
    {
        var order = await OrderRepository.FirstOrDefaultAsync(x => x.Id == AppTestDataBuilder.OrderId);
        order.ShouldNotBeNull();
        order.CreateTime.ShouldBeLessThanOrEqualTo(FakeClock.Now);
        order.UpdateUserId.ShouldBeNull();
        await OrderRepository.DeleteAsync(order);

        order.IsDeleted.ShouldBe(true);
        order.UpdateUserId.ShouldBeNull();
        Debug.Assert(order.UpdateTime != null, "order.UpdateTime != null");
        order.UpdateTime.Value.ShouldBeLessThanOrEqualTo(FakeClock.Now);
    }
}