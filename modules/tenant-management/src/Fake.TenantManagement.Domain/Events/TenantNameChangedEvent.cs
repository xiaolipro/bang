using Fake.EventBus;

namespace Fake.TenantManagement.Domain.Events;

public class TenantNameChangedEvent(Guid tenantId, string oldName, string newName) : Event
{
    public Guid TenantId { get; } = tenantId;
    public string OldName { get; } = oldName;
    public string NewName { get; } = newName;
}