using CephasOps.Application.Billing.DTOs;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Events;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Settings.DTOs;
using CephasOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CephasOps.Application.Automation.Handlers;

/// <summary>
/// When OrderCompletedEvent fires, evaluates automation rules (TriggerType=StatusChange, TriggerStatus=OrderCompleted) and executes actions.
/// Supports ActionType "GenerateInvoice": creates invoice from order if not already invoiced (idempotent).
/// </summary>
public sealed class OrderCompletedAutomationHandler : IDomainEventHandler<OrderCompletedEvent>
{
    public const string ActionTypeGenerateInvoice = "GenerateInvoice";

    private readonly ApplicationDbContext _context;
    private readonly IAutomationRuleService _automationRuleService;
    private readonly IBillingService _billingService;
    private readonly ILogger<OrderCompletedAutomationHandler> _logger;

    public OrderCompletedAutomationHandler(
        ApplicationDbContext context,
        IAutomationRuleService automationRuleService,
        IBillingService billingService,
        ILogger<OrderCompletedAutomationHandler> logger)
    {
        _context = context;
        _automationRuleService = automationRuleService;
        _billingService = billingService;
        _logger = logger;
    }

    public async Task HandleAsync(OrderCompletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var companyId = domainEvent.CompanyId ?? Guid.Empty;
        var orderId = domainEvent.OrderId;
        var userId = domainEvent.TriggeredByUserId ?? Guid.Empty;

        var order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CompanyId == companyId, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("OrderCompletedAutomationHandler: Order {OrderId} not found for company {CompanyId}", orderId, companyId);
            return;
        }

        if (order.InvoiceId.HasValue)
        {
            _logger.LogDebug("OrderCompletedAutomationHandler: Order {OrderId} already has InvoiceId {InvoiceId}, skipping", orderId, order.InvoiceId);
            return;
        }

        var orderType = await _context.OrderTypes
            .AsNoTracking()
            .Where(ot => ot.Id == order.OrderTypeId)
            .Select(ot => new { ot.Code })
            .FirstOrDefaultAsync(cancellationToken);
        var orderTypeCode = orderType?.Code;
        var rules = await _automationRuleService.GetApplicableRulesAsync(
            companyId,
            "Order",
            "OrderCompleted",
            order.PartnerId,
            order.DepartmentId,
            orderTypeCode,
            cancellationToken);

        foreach (var rule in rules.Where(r => string.Equals(r.ActionType, ActionTypeGenerateInvoice, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var buildResult = await _billingService.BuildInvoiceLinesFromOrdersAsync(
                    new[] { orderId },
                    companyId,
                    DateTime.UtcNow,
                    cancellationToken);
                if (buildResult.LineItems.Count == 0)
                {
                    _logger.LogWarning("OrderCompletedAutomationHandler: No invoice lines resolved for order {OrderId}, rule {RuleName}", orderId, rule.Name);
                    continue;
                }
                var dto = new CreateInvoiceDto
                {
                    IdempotencyKey = $"order-invoice-{orderId}",
                    PartnerId = order.PartnerId,
                    InvoiceDate = DateTime.UtcNow.Date,
                    LineItems = buildResult.LineItems
                };
                var invoice = await _billingService.CreateInvoiceAsync(dto, companyId, userId, cancellationToken);
                var orderToUpdate = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.CompanyId == companyId, cancellationToken);
                if (orderToUpdate != null)
                {
                    orderToUpdate.InvoiceId = invoice.Id;
                    orderToUpdate.InvoiceEligible = true;
                    await _context.SaveChangesAsync(cancellationToken);
                }
                _logger.LogInformation("OrderCompletedAutomationHandler: Created invoice {InvoiceId} for order {OrderId} (rule {RuleName})", invoice.Id, orderId, rule.Name);
                if (rule.StopOnMatch) break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrderCompletedAutomationHandler: Failed to generate invoice for order {OrderId}, rule {RuleName}", orderId, rule.Name);
            }
        }
    }
}
