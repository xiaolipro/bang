using Fake.Modularity;

// ReSharper disable once CheckNamespace
namespace Fake.Caching.StackExchangeRedis;

[DependsOn(typeof(FakeCachingModule))]
public class FakeCachingStackExchangeRedisModule : FakeModule
{
}