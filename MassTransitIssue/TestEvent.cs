namespace MassTransitIssue;

using MassTransit;

public class TestEvent : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
}