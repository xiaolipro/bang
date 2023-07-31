﻿using System;
using Fake.ObjectMapping.Mapster.Profiles;
using Fake.ObjectMapping.Models;
using Fake.Testing;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute.Extensions;

namespace Fake.ObjectMapping.Mapster;

public class MapsterTest : FakeIntegrationTest<FakeObjectMappingMapsterTestModule>
{
    private readonly IObjectMapper _objectMapper;
    public MapsterTest() => _objectMapper = ServiceProvider.GetRequiredService<IObjectMapper>();

    [Fact]
    void 使用Mapster作为ObjectMapper供应商()
    {
        var provider = ServiceProvider.GetRequiredService<IObjectMappingProvider>();
        Assert.True(provider is MapsterObjectMappingProvider);
    }

    [Fact]
    void 映射()
    {
        var dto = _objectMapper.Map<MyEntity, MyDto>(new MyEntity { Number = 42 });
        Assert.Equal(dto.Number, 42);

        // ReverseMap
        var entity = _objectMapper.Map<MyDto, MyEntity>(new MyDto { Number = 42 });
        Assert.Equal(entity.Number, 42);
    }

    [Fact]
    void 自定义规则映射()
    {
        new MyProfile();
        var time = DateTime.Now;
        var dto = _objectMapper.Map<TestEntity, TestDto>(new TestEntity(){CreateTime = time});
        Assert.Equal(dto.CreateTime, time.ToString("yyyy-MM-dd"));
    }
}