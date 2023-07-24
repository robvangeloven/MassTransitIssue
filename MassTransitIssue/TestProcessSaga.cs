namespace MassTransitIssue;

using MassTransit;

public class TestProcessSaga : MassTransitStateMachine<TestProcessState>
{
    Event<TestEvent>? TestEvent { get; set; }

    Schedule<TestProcessState, TestCallbackEvent>? TestSchedule { get; set; }

    public TestProcessSaga()
    {
        InstanceState(x => x.CurrentState);

        Event(() => TestEvent);

        Schedule(() => TestSchedule, instance => instance.ScheduleId, x =>
        {
            x.Received = e => e.CorrelateById(context => context.Message.CorrelationId);
        });

        Initially(
            When(TestEvent)
            .Schedule(TestSchedule, context => context.Init<TestCallbackEvent>(new TestCallbackEvent
            {
                CorrelationId = context.Saga.CorrelationId
            }), context => DateTime.Now.AddSeconds(3)));
    }
}
