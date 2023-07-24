namespace MassTransitIssue;

using MassTransit;

public class TestCallbackEvent : CorrelatedBy<Guid>
{
    public Guid CorrelationId { get; set; }
}