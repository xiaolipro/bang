using Fake.Domain.Entities;

namespace Fake.TenantManagement.Domain.TenantAggregate;

public class TenantConnectionString : Entity
{
    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = null!;

    public string Value { get; private set; } = null!;
    public static int MaxNameLength { get; set; } = 64;
    public static int MaxValueLength { get; set; } = 1024;

    public TenantConnectionString(Guid tenantId, string name, string value)
    {
        TenantId = tenantId;
        SetName(name);
        SetValue(value);
    }

    public override object[] GetKeys()
    {
        return [TenantId, Name];
    }

    public void SetName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    public void SetValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        Value = value;
    }
}