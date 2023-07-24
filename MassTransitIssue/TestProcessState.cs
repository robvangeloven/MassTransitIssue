namespace MassTransitIssue;

using MassTransit;

public class TestProcessState : SagaStateMachineInstance
{
    public string? CurrentState { get; set; }

    public Guid CorrelationId { get; set; }

    public Guid? ScheduleId { get; set; }
}