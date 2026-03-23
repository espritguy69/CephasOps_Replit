using CephasOps.Application.Events;
using CephasOps.Application.Events.Replay;
using CephasOps.Application.Workflow.JobOrchestration.Executors;
using CephasOps.Domain.Events;
using CephasOps.Domain.Workflow.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace CephasOps.Application.Tests.Workflow.JobOrchestration;

/// <summary>
/// Event consistency guard: EventHandlingAsyncJobExecutor must refuse to process an event when job company and event company differ.
/// </summary>
public class EventHandlingAsyncJobExecutorEventConsistencyTests
{
    [Fact]
    public async Task ExecuteAsync_WhenEventCompanyIdDoesNotMatchJobCompanyId_Throws()
    {
        var eventId = Guid.NewGuid();
        var jobCompanyId = Guid.NewGuid();
        var eventCompanyId = Guid.NewGuid();
        var entry = new EventStoreEntry
        {
            EventId = eventId,
            EventType = "WorkflowTransitionCompleted",
            Payload = "{}",
            CompanyId = eventCompanyId
        };

        var mockStore = new Mock<IEventStore>();
        mockStore.Setup(x => x.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>())).ReturnsAsync(entry);

        var mockRegistry = new Mock<IEventTypeRegistry>();
        mockRegistry.Setup(x => x.Deserialize(It.IsAny<string>(), It.IsAny<string>())).Returns((IDomainEvent?)new WorkflowTransitionCompletedEvent());

        var mockRecorder = new Mock<IJobRunRecorderForEvents>();
        mockRecorder.Setup(x => x.StartHandlerRunAsync(It.IsAny<IDomainEvent>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());

        var job = new JobExecution
        {
            JobType = "eventhandlingasync",
            CompanyId = jobCompanyId,
            PayloadJson = JsonSerializer.Serialize(new Dictionary<string, string> { ["eventId"] = eventId.ToString() })
        };

        var executor = new EventHandlingAsyncJobExecutor(
            Mock.Of<IServiceProvider>(),
            mockStore.Object,
            mockRegistry.Object,
            mockRecorder.Object,
            new Mock<ILogger<EventHandlingAsyncJobExecutor>>().Object,
            null);

        var act = () => executor.ExecuteAsync(job);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*event*company*")
            .WithMessage("*Refusing to process*");
    }
}
