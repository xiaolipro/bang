using Fake.Data;
using Fake.Domain.Entities.Auditing;

namespace Fake.TenantManagement.Domain.TenantAggregate;

public class Tenant : FullAuditedAggregateRoot<Guid>
{
    public string Name { get; private set; } = null!;
    public static int MaxNameLength { get; set; } = 64;
    
    public List<TenantConnectionString> ConnectionStrings { get; private set; }

    public Tenant(string name)
    {
        SetName(name);
        ConnectionStrings = new List<TenantConnectionString>();
    }

    public void SetName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    public string? GetDefaultConnectionString()
    {
        return GetConnectionString(Data.ConnectionStrings.DefaultConnectionStringName);
    }

    public string? GetConnectionString(string name)
    {
        return ConnectionStrings.FirstOrDefault(c => c.Name == name)?.Value;
    }

    public void SetDefaultConnectionString(string connectionString)
    {
        SetConnectionString(Data.ConnectionStrings.DefaultConnectionStringName, connectionString);
    }

    public void SetConnectionString(string name, string connectionString)
    {
        var tenantConnectionString = ConnectionStrings.FirstOrDefault(x => x.Name == name);

        if (tenantConnectionString != null)
        {
            tenantConnectionString.SetValue(connectionString);
        }
        else
        {
            tenantConnectionString = new TenantConnectionString(Id, name, connectionString);
            ConnectionStrings.Add(tenantConnectionString);
        }
    }
}