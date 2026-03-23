using CephasOps.Application.Agent.DTOs;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Orders.DTOs;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Application.Workflow.Services;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Orders.Enums;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CephasOps.Application.Agent.Services;

/// <summary>
/// Agent Mode Service - Automated decision-making and workflow execution
/// </summary>
public class AgentModeService : IAgentModeService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IOrderService _orderService;
    private readonly IWorkflowEngineService _workflowEngineService;
    private readonly IWorkflowDefinitionsService _workflowDefinitionsService;
    private readonly IInvoiceSubmissionService _invoiceSubmissionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AgentModeService> _logger;

    public AgentModeService(
        ApplicationDbContext context,
        IEmailTemplateService emailTemplateService,
        IOrderService orderService,
        IWorkflowEngineService workflowEngineService,
        IWorkflowDefinitionsService workflowDefinitionsService,
        IInvoiceSubmissionService invoiceSubmissionService,
        ICurrentUserService currentUserService,
        ILogger<AgentModeService> logger)
    {
        _context = context;
        _emailTemplateService = emailTemplateService;
        _orderService = orderService;
        _workflowEngineService = workflowEngineService;
        _workflowDefinitionsService = workflowDefinitionsService;
        _invoiceSubmissionService = invoiceSubmissionService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<AgentProcessingResult> ProcessEmailReplyAsync(Guid emailMessageId, CancellationToken cancellationToken = default)
    {
        var result = new AgentProcessingResult
        {
            Action = "EmailReplyProcessed",
            EntityId = emailMessageId,
            EntityType = "EmailMessage"
        };

        try
        {
            var email = await _context.EmailMessages
                .FirstOrDefaultAsync(e => e.Id == emailMessageId, cancellationToken);

            if (email == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Email message {emailMessageId} not found";
                return result;
            }

            // Check if this is a reply (has "Re:" in subject or In-Reply-To header)
            var isReply = email.Subject.Contains("Re:", StringComparison.OrdinalIgnoreCase) ||
                         email.Subject.Contains("RE:", StringComparison.OrdinalIgnoreCase);

            if (!isReply)
            {
                result.Success = false;
                result.ErrorMessage = "Email is not a reply";
                return result;
            }

            // Find the original sent email this is replying to
            var originalEmail = await FindOriginalEmailAsync(email, cancellationToken);
            if (originalEmail == null)
            {
                result.Success = false;
                result.ErrorMessage = "Could not find original email this is replying to";
                return result;
            }

            // Check if original email was sent using a template
            var templateDto = await FindTemplateFromEmailAsync(originalEmail, cancellationToken);
            if (templateDto == null || !templateDto.AutoProcessReplies)
            {
                result.Success = false;
                result.ErrorMessage = "No auto-process template found for this reply";
                return result;
            }

            // Extract reply content - prefer full body over preview
            var replyContent = email.BodyText ?? email.BodyHtml ?? email.BodyPreview ?? string.Empty;
            var replySubject = email.Subject ?? string.Empty;

            // Check if reply matches approval pattern
            var isApproved = CheckApprovalPattern(replyContent, replySubject, templateDto.ReplyPattern);

            if (isApproved)
            {
                // Find related entity (Order, Reschedule, etc.)
                var relatedEntity = await FindRelatedEntityFromEmailAsync(originalEmail, cancellationToken);
                
                if (relatedEntity.HasValue && relatedEntity.Value.EntityType == "Order")
                {
                    // Check if there's a pending reschedule
                    var reschedule = await _context.OrderReschedules
                        .Where(r => r.OrderId == relatedEntity.Value.EntityId && r.Status == "Pending")
                        .OrderByDescending(r => r.RequestedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (reschedule != null)
                    {
                        // Auto-approve reschedule
                        reschedule.Status = "Approved";
                        reschedule.ApprovalSource = "EmailAutoApproved";
                        reschedule.ApprovalEmailId = email.Id;
                        reschedule.StatusChangedAt = DateTime.UtcNow;
                        reschedule.StatusChangedByUserId = _currentUserService.UserId;

                        // Update order with new appointment
                        var order = await _context.Orders
                            .FirstOrDefaultAsync(o => o.Id == reschedule.OrderId, cancellationToken);

                        if (order != null)
                        {
                            order.AppointmentDate = reschedule.NewDate;
                            order.AppointmentWindowFrom = reschedule.NewWindowFrom;
                            order.AppointmentWindowTo = reschedule.NewWindowTo;
                            order.HasReschedules = true;
                            order.RescheduleCount++;
                            order.UpdatedAt = DateTime.UtcNow;
                        }

                        await _context.SaveChangesAsync(cancellationToken);

                        if (order != null && order.Status == OrderStatus.ReschedulePendingApproval)
                        {
                            var userId = _currentUserService.UserId ?? Guid.Empty;
                            await _orderService.ChangeOrderStatusAsync(
                                order.Id,
                                new ChangeOrderStatusDto { Status = OrderStatus.Assigned, Reason = "Reschedule approved via email" },
                                order.CompanyId,
                                order.DepartmentId,
                                userId,
                                cancellationToken);
                        }

                        result.Success = true;
                        result.Metadata = new Dictionary<string, object>
                        {
                            { "RescheduleId", reschedule.Id },
                            { "OrderId", reschedule.OrderId },
                            { "NewDate", reschedule.NewDate },
                            { "ApprovalMethod", "AutoApproved" }
                        };

                        _logger.LogInformation(
                            "Agent auto-approved reschedule {RescheduleId} for order {OrderId} based on email reply",
                            reschedule.Id, reschedule.OrderId);
                    }
                }
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Reply does not match approval pattern";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email reply {EmailId}", emailMessageId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<AgentProcessingResult> HandlePaymentRejectionAsync(Guid paymentId, string rejectionReason, CancellationToken cancellationToken = default)
    {
        var result = new AgentProcessingResult
        {
            Action = "PaymentRejected",
            EntityId = paymentId,
            EntityType = "Payment"
        };

        try
        {
            var payment = await _context.Payments
                .Include(p => p.Invoice)
                .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

            if (payment == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Payment {paymentId} not found";
                return result;
            }

            if (payment.Invoice == null)
            {
                result.Success = false;
                result.ErrorMessage = "Payment is not linked to an invoice";
                return result;
            }

            var invoice = payment.Invoice;
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.InvoiceId == invoice.Id, cancellationToken);

            if (order == null)
            {
                result.Success = false;
                result.ErrorMessage = "No order found for this invoice";
                return result;
            }

            // Update invoice status
            invoice.Status = "Rejected";
            invoice.UpdatedAt = DateTime.UtcNow;

            // Preserve SubmissionId for reference and update submission history
            var submissionId = invoice.SubmissionId;
            
            // Update submission history if exists
            if (!string.IsNullOrEmpty(submissionId))
            {
                var activeSubmission = await _invoiceSubmissionService.GetActiveSubmissionAsync(invoice.Id, cancellationToken);
                
                if (activeSubmission != null && activeSubmission.SubmissionId == submissionId)
                {
                    await _invoiceSubmissionService.UpdateSubmissionStatusAsync(
                        activeSubmission.Id,
                        "Rejected",
                        rejectionReason: rejectionReason,
                        paymentStatus: "Rejected",
                        cancellationToken: cancellationToken);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            var oldStatus = order.Status;
            var userId = _currentUserService.UserId ?? Guid.Empty;

            // Transition order to Reinvoice via workflow engine (guard conditions + side effects + audit)
            await _orderService.ChangeOrderStatusAsync(
                order.Id,
                new ChangeOrderStatusDto
                {
                    Status = OrderStatus.Reinvoice,
                    Reason = $"Payment rejected: {rejectionReason}. SubmissionId: {submissionId}",
                    Metadata = new Dictionary<string, object>
                    {
                        ["PaymentId"] = paymentId,
                        ["InvoiceId"] = invoice.Id,
                        ["SubmissionId"] = submissionId ?? string.Empty
                    }
                },
                order.CompanyId,
                order.DepartmentId,
                userId,
                cancellationToken);

            result.Success = true;
            result.Metadata = new Dictionary<string, object>
            {
                { "InvoiceId", invoice.Id },
                { "OrderId", order.Id },
                { "SubmissionId", submissionId ?? "N/A" },
                { "PreviousStatus", oldStatus }
            };

            _logger.LogInformation(
                "Agent handled payment rejection: Payment {PaymentId}, Order {OrderId} → Reinvoice (via workflow)",
                paymentId, order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling payment rejection {PaymentId}", paymentId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<AgentRoutingResult> RouteOrderIntelligentlyAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var result = new AgentRoutingResult
        {
            OrderId = orderId
        };

        try
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

            if (order == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Order {orderId} not found";
                return result;
            }

            var routingFactors = new Dictionary<string, object>();
            decimal confidenceScore = 0.5m;

            // Factor 1: Partner-based routing
            if (order.PartnerId != Guid.Empty)
            {
                var partner = await _context.Partners
                    .FirstOrDefaultAsync(p => p.Id == order.PartnerId, cancellationToken);
                if (partner != null)
                {
                    // Some partners have default departments
                    // This is a simplified example - you'd have partner-department mappings
                    routingFactors["PartnerId"] = partner.Id;
                    routingFactors["PartnerName"] = partner.Name;
                    confidenceScore += 0.2m;
                }
            }

            // Factor 2: Location-based routing (city/state)
            if (!string.IsNullOrWhiteSpace(order.City))
            {
                // Find department that handles this city/state
                var locationDepartment = await _context.Departments
                    .Where(d => d.IsActive && 
                        (d.Name.Contains(order.City, StringComparison.OrdinalIgnoreCase) ||
                         d.Description != null && d.Description.Contains(order.City, StringComparison.OrdinalIgnoreCase)))
                    .FirstOrDefaultAsync(cancellationToken);

                if (locationDepartment != null)
                {
                    result.RecommendedDepartmentId = locationDepartment.Id;
                    routingFactors["LocationMatch"] = order.City;
                    confidenceScore += 0.3m;
                }
            }

            // Factor 3: Order type routing
            var orderType = await _context.OrderTypes
                .FirstOrDefaultAsync(ot => ot.Id == order.OrderTypeId, cancellationToken);

            if (orderType != null)
            {
                routingFactors["OrderType"] = orderType.Code;
                // Some order types have default departments
                confidenceScore += 0.1m;
            }

            // Factor 4: SI availability and skills
            if (order.AssignedSiId.HasValue)
            {
                var si = await _context.ServiceInstallers
                    .FirstOrDefaultAsync(s => s.Id == order.AssignedSiId.Value, cancellationToken);

                if (si != null && si.DepartmentId.HasValue)
                {
                    result.RecommendedDepartmentId = si.DepartmentId.Value;
                    result.RecommendedSiId = si.Id;
                    routingFactors["AssignedSI"] = si.Id;
                    confidenceScore += 0.2m;
                }
            }

            result.Success = true;
            result.ConfidenceScore = Math.Min(confidenceScore, 1.0m);
            result.RoutingReason = BuildRoutingReason(routingFactors);
            result.RoutingFactors = routingFactors;

            _logger.LogInformation(
                "Agent routed order {OrderId} to department {DepartmentId} with confidence {Confidence}",
                orderId, result.RecommendedDepartmentId, result.ConfidenceScore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing order {OrderId}", orderId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<AgentKpiResult> CalculateSmartKpisAsync(Guid? departmentId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var result = new AgentKpiResult
        {
            DepartmentId = departmentId,
            FromDate = fromDate ?? DateTime.UtcNow.AddDays(-30),
            ToDate = toDate ?? DateTime.UtcNow
        };

        try
        {
            var query = _context.Orders.AsQueryable();

            if (departmentId.HasValue)
            {
                query = query.Where(o => o.DepartmentId == departmentId.Value);
            }

            query = query.Where(o => o.CreatedAt >= result.FromDate && o.CreatedAt <= result.ToDate);

            var orders = await query.ToListAsync(cancellationToken);

            // Calculate metrics
            result.Metrics["TotalOrders"] = orders.Count;
            result.Metrics["CompletedOrders"] = orders.Count(o => o.Status == "Completed");
            result.Metrics["InProgressOrders"] = orders.Count(o => o.Status == "InProgress" || o.Status == "Assigned");
            result.Metrics["CancelledOrders"] = orders.Count(o => o.Status == "Cancelled");
            result.Metrics["CompletionRate"] = orders.Count > 0 
                ? (decimal)orders.Count(o => o.Status == "Completed") / orders.Count * 100 
                : 0;

            // Calculate average completion time
            var completedOrders = orders.Where(o => o.Status == "Completed").ToList();
            if (completedOrders.Any())
            {
                var avgCompletionDays = completedOrders
                    .Where(o => o.UpdatedAt > o.CreatedAt)
                    .Average(o => (o.UpdatedAt - o.CreatedAt).TotalDays);
                result.Metrics["AvgCompletionDays"] = (decimal)avgCompletionDays;
            }

            // Reschedule metrics
            result.Metrics["RescheduleCount"] = orders.Sum(o => o.RescheduleCount);
            result.Metrics["OrdersWithReschedules"] = orders.Count(o => o.HasReschedules);

            // KPI breach metrics
            result.Metrics["KpiBreachedOrders"] = orders.Count(o => o.KpiBreachedAt.HasValue);

            // Generate insights
            var insights = new Dictionary<string, object>();
            if (result.Metrics["CompletionRate"] < 80m)
            {
                insights["LowCompletionRate"] = "Completion rate is below 80%. Review workflow bottlenecks.";
            }
            if (result.Metrics["RescheduleCount"] > (decimal)(orders.Count * 0.2))
            {
                insights["HighRescheduleRate"] = "Reschedule rate is high. Review scheduling accuracy.";
            }

            result.Insights = insights;
            result.Success = true;

            _logger.LogInformation(
                "Agent calculated KPIs for department {DepartmentId}: {TotalOrders} orders, {CompletionRate}% completion",
                departmentId, result.Metrics["TotalOrders"], result.Metrics["CompletionRate"]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating smart KPIs");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<AgentProcessingResult> AutoApproveRescheduleAsync(Guid rescheduleId, CancellationToken cancellationToken = default)
    {
        var result = new AgentProcessingResult
        {
            Action = "RescheduleAutoApproved",
            EntityId = rescheduleId,
            EntityType = "OrderReschedule"
        };

        try
        {
            var reschedule = await _context.OrderReschedules
                .FirstOrDefaultAsync(r => r.Id == rescheduleId, cancellationToken);

            if (reschedule == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Reschedule {rescheduleId} not found";
                return result;
            }

            if (reschedule.Status != "Pending")
            {
                result.Success = false;
                result.ErrorMessage = $"Reschedule is not in Pending status (current: {reschedule.Status})";
                return result;
            }

            // Auto-approve logic (can be enhanced with rules)
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == reschedule.OrderId, cancellationToken);

            if (order == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Order {reschedule.OrderId} not found";
                return result;
            }
            var isSameDay = reschedule.NewDate.Date == reschedule.OriginalDate.Date;
            var isTimeOnly = isSameDay && reschedule.NewWindowFrom != TimeSpan.Zero;

            // Auto-approve same-day time changes (with audit trail)
            if (isTimeOnly)
            {
                reschedule.Status = "Approved";
                reschedule.ApprovalSource = "AgentAutoApproved";
                reschedule.StatusChangedAt = DateTime.UtcNow;
                reschedule.StatusChangedByUserId = _currentUserService.UserId;

                order.AppointmentDate = reschedule.NewDate;
                order.AppointmentWindowFrom = reschedule.NewWindowFrom;
                order.AppointmentWindowTo = reschedule.NewWindowTo;
                order.HasReschedules = true;
                order.RescheduleCount++;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(cancellationToken);

                if (order.Status == OrderStatus.ReschedulePendingApproval)
                {
                    var userId = _currentUserService.UserId ?? Guid.Empty;
                    await _orderService.ChangeOrderStatusAsync(
                        order.Id,
                        new ChangeOrderStatusDto { Status = OrderStatus.Assigned, Reason = "Reschedule approved (same-day time change)" },
                        order.CompanyId,
                        order.DepartmentId,
                        userId,
                        cancellationToken);
                }

                result.Success = true;
                result.Metadata = new Dictionary<string, object>
                {
                    { "OrderId", order.Id },
                    { "AutoApprovalReason", "Same-day time change" }
                };

                _logger.LogInformation(
                    "Agent auto-approved reschedule {RescheduleId} for order {OrderId} (same-day time change)",
                    rescheduleId, order.Id);
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = "Reschedule requires manual approval (date change or complex scenario)";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-approving reschedule {RescheduleId}", rescheduleId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<List<AgentProcessingResult>> ProcessPendingTasksAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<AgentProcessingResult>();

        try
        {
            // Find pending email replies
            var pendingReplies = await _context.EmailMessages
                .Where(e => e.Direction == "Inbound" && 
                           e.ParserStatus == "Pending" &&
                           (e.Subject.Contains("Re:", StringComparison.OrdinalIgnoreCase) ||
                            e.Subject.Contains("RE:", StringComparison.OrdinalIgnoreCase)))
                .Take(10)
                .ToListAsync(cancellationToken);

            foreach (var reply in pendingReplies)
            {
                var result = await ProcessEmailReplyAsync(reply.Id, cancellationToken);
                results.Add(result);
            }

            // Find pending reschedules that might be auto-approved
            var pendingReschedules = await _context.OrderReschedules
                .Where(r => r.Status == "Pending" && r.RequestedAt >= DateTime.UtcNow.AddDays(-1))
                .Take(10)
                .ToListAsync(cancellationToken);

            foreach (var reschedule in pendingReschedules)
            {
                var result = await AutoApproveRescheduleAsync(reschedule.Id, cancellationToken);
                results.Add(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending agent tasks");
        }

        return results;
    }

    // Helper methods
    private async Task<EmailMessage?> FindOriginalEmailAsync(EmailMessage reply, CancellationToken cancellationToken)
    {
        // Try to find original email by subject (remove "Re:" prefix)
        var originalSubject = reply.Subject
            .Replace("Re:", "", StringComparison.OrdinalIgnoreCase)
            .Replace("RE:", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return await _context.EmailMessages
            .Where(e => e.Direction == "Outbound" &&
                       e.Subject.Contains(originalSubject, StringComparison.OrdinalIgnoreCase) &&
                       e.SentAt < reply.ReceivedAt)
            .OrderByDescending(e => e.SentAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<EmailTemplateDto?> FindTemplateFromEmailAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        // This is simplified - in reality, you'd track which template was used when sending
        // For now, check templates that match the subject pattern
        var templates = await _emailTemplateService.GetAllAsync(direction: null, companyId: null, cancellationToken);
        return templates
            .Where(t => t.IsActive && t.AutoProcessReplies &&
                       email.Subject.Contains(t.SubjectTemplate.Split('{').FirstOrDefault() ?? "", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.Priority)
            .FirstOrDefault();
    }

    private bool CheckApprovalPattern(string content, string subject, string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            // Default approval patterns
            var defaultPatterns = new[] { "approved", "confirm", "yes", "ok", "proceed", "agreed" };
            return defaultPatterns.Any(p => content.Contains(p, StringComparison.OrdinalIgnoreCase) ||
                                           subject.Contains(p, StringComparison.OrdinalIgnoreCase));
        }

        // Use regex if pattern provided
        try
        {
            return Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase) ||
                   Regex.IsMatch(subject, pattern, RegexOptions.IgnoreCase);
        }
        catch
        {
            // Fallback to simple contains
            return content.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
                   subject.Contains(pattern, StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task<(Guid EntityId, string EntityType)?> FindRelatedEntityFromEmailAsync(EmailMessage email, CancellationToken cancellationToken)
    {
        // Try to find related order from email subject/body
        // Extract order number or service ID - prefer full body over preview
        var orderNumberPattern = @"(?:Order|Service|Ticket)[\s#:]*([A-Z0-9-]+)";
        var emailBody = email.BodyText ?? email.BodyHtml ?? email.BodyPreview ?? "";
        var match = Regex.Match(email.Subject + " " + emailBody, orderNumberPattern, RegexOptions.IgnoreCase);

        if (match.Success)
        {
            var identifier = match.Groups[1].Value;
            var order = await _context.Orders
                .Where(o => o.ServiceId == identifier || o.TicketId == identifier)
                .FirstOrDefaultAsync(cancellationToken);

            if (order != null)
            {
                return (order.Id, "Order");
            }
        }

        return null;
    }

    private string BuildRoutingReason(Dictionary<string, object> factors)
    {
        var reasons = new List<string>();
        if (factors.ContainsKey("LocationMatch"))
            reasons.Add($"Location: {factors["LocationMatch"]}");
        if (factors.ContainsKey("AssignedSI"))
            reasons.Add("Based on assigned SI");
        if (factors.ContainsKey("PartnerName"))
            reasons.Add($"Partner: {factors["PartnerName"]}");
        if (factors.ContainsKey("OrderType"))
            reasons.Add($"Order type: {factors["OrderType"]}");

        return string.Join(", ", reasons) + ".";
    }
}

