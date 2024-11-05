using System;
using Fake.Data;

namespace Fake.MultiTenant;

public class TenantConfiguration(Guid id, string name, string code)
{
    public Guid Id { get; set; } = id;

    public string Name { get; set; } = name;

    public string Code { get; set; } = code;

    public ConnectionStrings? ConnectionStrings { get; set; }
    
    public bool IsEnable { get; set; } = true;
}