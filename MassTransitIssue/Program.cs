using System.Text.Json;

using MassTransit;
using MassTransit.Serialization;
using MassTransit.Transports;

using MassTransitIssue;

using Microsoft.Extensions.DependencyInjection;

IServiceCollection services = new ServiceCollection();

services.AddMassTransit(config =>
{
    config.AddSagaStateMachine<TestProcessSaga, TestProcessState>()
        .InMemoryRepository();

    config.AddServiceBusMessageScheduler();

    config.UsingAzureServiceBus((ctx, cfg) =>
    {
        cfg.Host("hostname");

        cfg.UseServiceBusMessageScheduler();

        cfg.ReceiveEndpoint("test-process", endpointConfigurator =>
        {
            const int ConcurrencyLimit = 20; // this can go up, depending upon the database capacity

            endpointConfigurator.ConfigureConsumeTopology = false;

            endpointConfigurator.PrefetchCount = ConcurrencyLimit;

            endpointConfigurator.UseMessageRetry(r => r.Interval(5, 1000));
            endpointConfigurator.UseInMemoryOutbox();

            endpointConfigurator.UsePublishMessageScheduler();

            endpointConfigurator.ConfigureSaga<TestProcessState>(ctx, s =>
            {
                var partition = endpointConfigurator.CreatePartitioner(ConcurrencyLimit);

                s.Message<TestEvent>(x => x.UsePartitioner(partition, m => m.Message.CorrelationId));
                s.Message<TestCallbackEvent>(x => x.UsePartitioner(partition, m => m.Message.CorrelationId));
            });
        });

        cfg.ConfigureEndpoints(ctx);
    });
});

var serviceProvider = services.BuildServiceProvider();
var busControl = serviceProvider.GetRequiredService<IBusControl>();
var endpoint = serviceProvider.GetRequiredService<IReceiveEndpointDispatcher<TestProcessState>>();

await busControl.StartAsync();

var result = busControl.GetProbeResult();

var resultText = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
Console.WriteLine(resultText);

using var memoryStream = new MemoryStream();

JsonSerializer.Serialize(memoryStream, new JsonMessageEnvelope
{
    MessageId = NewId.NextSequentialGuid().ToString(),
    MessageType = new[] { MessageUrn.ForTypeString<TestEvent>() },
    Message = new TestEvent
    {
        CorrelationId = NewId.NextSequentialGuid(),
    }
});

await endpoint.Dispatch(memoryStream.ToArray(), new Dictionary<string, object>().AsReadOnly(), CancellationToken.None);

await busControl.StopAsync();

Console.ReadLine();

