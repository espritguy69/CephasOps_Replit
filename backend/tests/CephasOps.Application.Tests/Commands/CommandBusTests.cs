using CephasOps.Application.Commands;
using CephasOps.Application.Workflow.DTOs;
using CephasOps.Application.Workflow.Services;
using CephasOps.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CephasOps.Application.Tests.Commands;

/// <summary>
/// Command bus: SendAsync returns success and result; idempotency reuses result when same key.
/// </summary>
public class CommandBusTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;

    public CommandBusTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new ApplicationDbContext(options);
        _dbContext.Database.EnsureCreated();

        var workflowMock = new Mock<IWorkflowEngineService>();
        workflowMock
            .Setup(w => w.ExecuteTransitionAsync(It.IsAny<Guid>(), It.IsAny<ExecuteTransitionDto>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkflowJobDto
            {
                Id = Guid.NewGuid(),
                EntityType = "Order",
                EntityId = Guid.NewGuid(),
                CurrentStatus = "Draft",
                TargetStatus = "Submitted",
                State = "Succeeded",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

        var services = new ServiceCollection();
        services.AddScoped(_ => _dbContext);
        services.AddScoped<ICommandBus, CommandBus>();
        services.AddScoped<ICommandProcessingLogStore, CommandProcessingLogStore>();
        services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(CephasOps.Application.Commands.Pipeline.ValidationBehavior<,>));
        services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(CephasOps.Application.Commands.Pipeline.IdempotencyBehavior<,>));
        services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(CephasOps.Application.Commands.Pipeline.LoggingBehavior<,>));
        services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(CephasOps.Application.Commands.Pipeline.RetryBehavior<,>));
        services.AddScoped<ICommandHandler<ExecuteWorkflowTransitionCommand, WorkflowJobDto>, ExecuteWorkflowTransitionHandler>();
        services.AddScoped<IWorkflowEngineService>(_ => workflowMock.Object);
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddLogging();
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task SendAsync_ExecuteWorkflowTransitionCommand_ReturnsSuccessAndResult()
    {
        var bus = _serviceProvider.GetRequiredService<ICommandBus>();
        var command = new ExecuteWorkflowTransitionCommand
        {
            CompanyId = Guid.NewGuid(),
            EntityId = Guid.NewGuid(),
            EntityType = "Order",
            TargetStatus = "Submitted"
        };

        var result = await bus.SendAsync<ExecuteWorkflowTransitionCommand, WorkflowJobDto>(command);

        result.Success.Should().BeTrue();
        result.Result.Should().NotBeNull();
        result.Result!.EntityType.Should().Be("Order");
        result.Result.TargetStatus.Should().Be("Submitted");
        result.ExecutionId.Should().NotBe(Guid.Empty);
        result.IdempotencyReused.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_SameIdempotencyKeyTwice_SecondReturnsIdempotencyReused()
    {
        var bus = _serviceProvider.GetRequiredService<ICommandBus>();
        var idemKey = "idem-" + Guid.NewGuid();
        var command = new ExecuteWorkflowTransitionCommand
        {
            CompanyId = Guid.NewGuid(),
            EntityId = Guid.NewGuid(),
            EntityType = "Order",
            TargetStatus = "Submitted",
            IdempotencyKey = idemKey
        };

        var first = await bus.SendAsync<ExecuteWorkflowTransitionCommand, WorkflowJobDto>(command);
        first.Success.Should().BeTrue();
        first.IdempotencyReused.Should().BeFalse();
        var firstJobId = first.Result!.Id;

        var second = await bus.SendAsync<ExecuteWorkflowTransitionCommand, WorkflowJobDto>(command);
        second.Success.Should().BeTrue();
        second.IdempotencyReused.Should().BeTrue();
        second.Result.Should().NotBeNull();
        second.Result!.Id.Should().Be(firstJobId);
    }
}
