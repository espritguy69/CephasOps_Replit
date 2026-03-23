using CephasOps.Domain.Common;

namespace CephasOps.Domain.Tasks.Entities;

/// <summary>
/// Task item entity - represents tasks/todos
/// </summary>
public class TaskItem : CompanyScopedEntity
{
    public Guid? DepartmentId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public Guid AssignedToUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? DueAt { get; set; }
    public string Priority { get; set; } = "Normal"; // Low, Normal, High
    public string Status { get; set; } = "New"; // New, InProgress, OnHold, Completed, Cancelled
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid UpdatedByUserId { get; set; }
    /// <summary>Optional link to an order; used for installer-task idempotency (one task per order).</summary>
    public Guid? OrderId { get; set; }
}

