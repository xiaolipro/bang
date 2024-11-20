using Fake.EventBus;
using Fake.EventBus.Distributed;

namespace Application.IntegrationEvents;

public class OrderStartedIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; set; }

    public OrderStartedIntegrationEvent(Guid userId)
        => UserId = userId;
}