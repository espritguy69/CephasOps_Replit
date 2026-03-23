namespace CephasOps.Application.Departments.Services;

/// <summary>
/// Represents the set of departments a user can work with.
/// </summary>
public sealed record DepartmentAccessResult(
    bool HasGlobalAccess,
    IReadOnlyList<Guid> DepartmentIds,
    Guid? DefaultDepartmentId)
{
    public static DepartmentAccessResult Global { get; } =
        new(true, Array.Empty<Guid>(), null);

    public static DepartmentAccessResult None { get; } =
        new(false, Array.Empty<Guid>(), null);
}


