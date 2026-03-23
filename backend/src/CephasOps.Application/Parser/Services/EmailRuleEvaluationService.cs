using System.Text.RegularExpressions;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Domain.Parser.Enums;

namespace CephasOps.Application.Parser.Services;

/// <summary>
/// Service for evaluating email rules and VIP status
/// </summary>
public interface IEmailRuleEvaluationService
{
    /// <summary>
    /// Evaluate an email against VIP list and rules
    /// </summary>
    EmailRuleEvaluationResult Evaluate(
        string fromAddress,
        string subject,
        IEnumerable<VipEmail> vipEmails,
        IEnumerable<ParserRule> rules);

    /// <summary>
    /// Check if an email address matches a pattern
    /// </summary>
    bool MatchesPattern(string emailAddress, string pattern);
}

/// <summary>
/// Result of email rule evaluation
/// </summary>
public class EmailRuleEvaluationResult
{
    /// <summary>
    /// Whether the email is from a VIP sender
    /// </summary>
    public bool IsVip { get; set; }

    /// <summary>
    /// The matched VIP email entry (if any)
    /// </summary>
    public VipEmail? MatchedVipEmail { get; set; }

    /// <summary>
    /// The matched rule (if any)
    /// </summary>
    public ParserRule? MatchedRule { get; set; }

    /// <summary>
    /// The action to take based on the matched rule
    /// </summary>
    public EmailRuleActionType? Action { get; set; }

    /// <summary>
    /// Whether to ignore/skip this email
    /// </summary>
    public bool ShouldIgnore { get; set; }

    /// <summary>
    /// Target department ID for routing (if applicable)
    /// </summary>
    public Guid? TargetDepartmentId { get; set; }

    /// <summary>
    /// Target user ID for routing (if applicable)
    /// </summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// User ID to notify (from VipEmail or rule)
    /// </summary>
    public Guid? NotifyUserId { get; set; }

    /// <summary>
    /// Role to notify (from VipEmail)
    /// </summary>
    public string? NotifyRole { get; set; }
}

/// <summary>
/// Implementation of email rule evaluation service
/// </summary>
public class EmailRuleEvaluationService : IEmailRuleEvaluationService
{
    /// <inheritdoc />
    public EmailRuleEvaluationResult Evaluate(
        string fromAddress,
        string subject,
        IEnumerable<VipEmail> vipEmails,
        IEnumerable<ParserRule> rules)
    {
        var result = new EmailRuleEvaluationResult();

        if (string.IsNullOrEmpty(fromAddress))
        {
            return result;
        }

        var normalizedFrom = fromAddress.Trim().ToLowerInvariant();

        // Step 1: Check VIP email list (exact match)
        var matchedVip = vipEmails
            .Where(v => v.IsActive)
            .FirstOrDefault(v => v.EmailAddress.Trim().Equals(normalizedFrom, StringComparison.OrdinalIgnoreCase));

        if (matchedVip != null)
        {
            result.IsVip = true;
            result.MatchedVipEmail = matchedVip;
            result.NotifyUserId = matchedVip.NotifyUserId;
            result.NotifyRole = matchedVip.NotifyRole;
        }

        // Step 2: Evaluate rules in priority order (highest first)
        var activeRules = rules
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.Priority)
            .ToList();

        foreach (var rule in activeRules)
        {
            if (RuleMatches(normalizedFrom, subject, rule))
            {
                result.MatchedRule = rule;

                // Parse action type
                if (Enum.TryParse<EmailRuleActionType>(rule.ActionType, true, out var actionType))
                {
                    result.Action = actionType;

                    switch (actionType)
                    {
                        case EmailRuleActionType.Ignore:
                            result.ShouldIgnore = true;
                            return result; // Stop evaluation immediately

                        case EmailRuleActionType.MarkVipOnly:
                            result.IsVip = true;
                            break;

                        case EmailRuleActionType.RouteToDepartment:
                            result.TargetDepartmentId = rule.TargetDepartmentId;
                            break;

                        case EmailRuleActionType.RouteToUser:
                            result.TargetUserId = rule.TargetUserId;
                            break;

                        case EmailRuleActionType.MarkVipAndRouteToDepartment:
                            result.IsVip = true;
                            result.TargetDepartmentId = rule.TargetDepartmentId;
                            break;

                        case EmailRuleActionType.MarkVipAndRouteToUser:
                            result.IsVip = true;
                            result.TargetUserId = rule.TargetUserId;
                            break;
                    }
                }

                // Also check IsVip flag on rule
                if (rule.IsVip)
                {
                    result.IsVip = true;
                }

                // First matching rule wins
                break;
            }
        }

        return result;
    }

    /// <inheritdoc />
    public bool MatchesPattern(string emailAddress, string pattern)
    {
        if (string.IsNullOrEmpty(emailAddress) || string.IsNullOrEmpty(pattern))
        {
            return false;
        }

        var normalizedEmail = emailAddress.Trim().ToLowerInvariant();
        var normalizedPattern = pattern.Trim().ToLowerInvariant();

        // Domain pattern: @domain.com matches any email from that domain
        if (normalizedPattern.StartsWith("@"))
        {
            return normalizedEmail.EndsWith(normalizedPattern, StringComparison.OrdinalIgnoreCase);
        }

        // Convert wildcard pattern to regex
        // * = any characters (zero or more)
        // ? = single character
        var regexPattern = "^" + Regex.Escape(normalizedPattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return Regex.IsMatch(normalizedEmail, regexPattern, RegexOptions.IgnoreCase);
    }

    private bool RuleMatches(string fromAddress, string subject, ParserRule rule)
    {
        // Check FROM address pattern
        if (!string.IsNullOrEmpty(rule.FromAddressPattern))
        {
            if (!MatchesPattern(fromAddress, rule.FromAddressPattern))
            {
                return false;
            }
        }

        // Check domain pattern
        if (!string.IsNullOrEmpty(rule.DomainPattern))
        {
            var domainPattern = rule.DomainPattern.StartsWith("@") 
                ? rule.DomainPattern 
                : "@" + rule.DomainPattern;

            if (!MatchesPattern(fromAddress, "*" + domainPattern))
            {
                return false;
            }
        }

        // Check subject contains
        if (!string.IsNullOrEmpty(rule.SubjectContains))
        {
            if (string.IsNullOrEmpty(subject) || 
                !subject.Contains(rule.SubjectContains, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // If no patterns specified, rule doesn't match
        if (string.IsNullOrEmpty(rule.FromAddressPattern) && 
            string.IsNullOrEmpty(rule.DomainPattern) && 
            string.IsNullOrEmpty(rule.SubjectContains))
        {
            return false;
        }

        return true;
    }
}

