using Fake.Auditing;
using Fake.Domain.Events;
using Fake.IdGenerators.GuidGenerator;

namespace Fake.Domain.Entities;

[Serializable]
public abstract class AggregateRoot : Entity, IAggregateRoot
{
    private List<DomainEvent>? _domainEvents;

    public IReadOnlyCollection<DomainEvent>? DomainEvents => _domainEvents?.AsReadOnly();

    [DisableAuditing] public virtual Guid ConcurrencyStamp { get; set; } = SimpleGuidGenerator.Instance.Generate();


    protected virtual void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents ??= new List<DomainEvent>();
        _domainEvents.Add(domainEvent);
    }

    protected virtual void RemoveDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents?.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }

    public override string ToString()
    {
        return $"[聚合根: {GetType().Name}] Keys：{string.Join(", ", GetKeys())} 领域事件: {_domainEvents?
            .Select(e => e.GetType().Name).JoinAsString(", ")}";
    }
}

[Serializable]
public abstract class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot<TKey> where TKey : struct
{
    private List<DomainEvent>? _domainEvents;

    public IReadOnlyCollection<DomainEvent>? DomainEvents => _domainEvents?.AsReadOnly();

    [DisableAuditing] public virtual Guid ConcurrencyStamp { get; set; } = SimpleGuidGenerator.Instance.Generate();

    protected virtual void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents ??= [];
        _domainEvents.Add(domainEvent);
    }

    protected virtual void RemoveDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents?.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }

    public override string ToString()
    {
        return $"[聚合根: {GetType().Name}] Id: {Id} 领域事件: {_domainEvents?
            .Select(e => e.GetType().Name).JoinAsString(", ")}";
    }
}