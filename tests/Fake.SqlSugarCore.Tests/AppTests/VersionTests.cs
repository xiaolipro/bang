using Fake.DependencyInjection;
using Tests;

namespace Fake.SqlSugarCore.Tests.AppTests;

public class VersionTests : VersionTests<FakeSqlSugarCoreTestModule>, ITransientDependency
{
}