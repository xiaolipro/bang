using Fake.DependencyInjection;
using Tests;

namespace Fake.SqlSugarCore.Tests.AppTests;

public class UowTests : UowTests<FakeSqlSugarCoreTestModule>, ITransientDependency
{
}