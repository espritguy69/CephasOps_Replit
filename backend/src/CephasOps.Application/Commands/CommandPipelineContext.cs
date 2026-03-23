namespace CephasOps.Application.Commands;

/// <summary>
/// Ambient context for the current command execution (options and execution id). Set by CommandBus.
/// </summary>
public static class CommandPipelineContext
{
    private static readonly AsyncLocal<CommandOptions?> Options = new();
    private static readonly AsyncLocal<Guid> ExecutionId = new();

    public static CommandOptions? CurrentOptions
    {
        get => Options.Value;
        set => Options.Value = value;
    }

    public static Guid CurrentExecutionId
    {
        get => ExecutionId.Value;
        set => ExecutionId.Value = value;
    }

    public static void Set(CommandOptions? options, Guid executionId)
    {
        Options.Value = options;
        ExecutionId.Value = executionId;
    }

    public static void Clear()
    {
        Options.Value = null;
        ExecutionId.Value = default;
    }
}
