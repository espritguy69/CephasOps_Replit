using CephasOps.Application.Parser.Services;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Domain.Parser.Enums;
using FluentAssertions;
using Xunit;

namespace CephasOps.Application.Tests.Parser.Services;

public class EmailRuleEvaluationServiceTests
{
    private readonly EmailRuleEvaluationService _service;

    public EmailRuleEvaluationServiceTests()
    {
        _service = new EmailRuleEvaluationService();
    }

    #region Pattern Matching Tests

    [Theory]
    [InlineData("ceo@company.com", "ceo@company.com", true)]
    [InlineData("CEO@COMPANY.COM", "ceo@company.com", true)]
    [InlineData("ceo@company.com", "CEO@COMPANY.COM", true)]
    [InlineData("ceo@company.com", "director@company.com", false)]
    public void MatchesPattern_ExactMatch_ReturnsExpectedResult(string email, string pattern, bool expected)
    {
        // Act
        var result = _service.MatchesPattern(email, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("ceo@company.com", "*@company.com", true)]
    [InlineData("director@company.com", "*@company.com", true)]
    [InlineData("anyone@company.com", "*@company.com", true)]
    [InlineData("ceo@other.com", "*@company.com", false)]
    public void MatchesPattern_WildcardStar_MatchesAnyCharacters(string email, string pattern, bool expected)
    {
        // Act
        var result = _service.MatchesPattern(email, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("director1@company.com", "director?@company.com", true)]
    [InlineData("director2@company.com", "director?@company.com", true)]
    [InlineData("directorA@company.com", "director?@company.com", true)]
    [InlineData("director@company.com", "director?@company.com", false)]
    [InlineData("director12@company.com", "director?@company.com", false)]
    public void MatchesPattern_WildcardQuestion_MatchesSingleCharacter(string email, string pattern, bool expected)
    {
        // Act
        var result = _service.MatchesPattern(email, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("noreply@time.com.my", "@time.com.my", true)]
    [InlineData("activation@time.com.my", "@time.com.my", true)]
    [InlineData("anyone@time.com.my", "@time.com.my", true)]
    [InlineData("noreply@digi.com.my", "@time.com.my", false)]
    [InlineData("noreply@company.com", "@time.com.my", false)]
    public void MatchesPattern_DomainPattern_MatchesAnyUserAtDomain(string email, string pattern, bool expected)
    {
        // Act
        var result = _service.MatchesPattern(email, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("director@company.com", "director*@*", true)]
    [InlineData("director1@company.com", "director*@*", true)]
    [InlineData("directors@other.com", "director*@*", true)]
    [InlineData("ceo@company.com", "director*@*", false)]
    public void MatchesPattern_ComplexWildcard_MatchesCorrectly(string email, string pattern, bool expected)
    {
        // Act
        var result = _service.MatchesPattern(email, pattern);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("", "pattern", false)]
    [InlineData("email@test.com", "", false)]
    [InlineData(null, "pattern", false)]
    [InlineData("email@test.com", null, false)]
    public void MatchesPattern_NullOrEmpty_ReturnsFalse(string? email, string? pattern, bool expected)
    {
        // Act
        var result = _service.MatchesPattern(email!, pattern!);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region VIP Email Detection Tests

    [Fact]
    public void Evaluate_VipEmailExactMatch_ReturnsIsVipTrue()
    {
        // Arrange
        var vipEmails = new List<VipEmail>
        {
            CreateVipEmail("ceo@company.com", notifyUserId: Guid.NewGuid())
        };

        // Act
        var result = _service.Evaluate("ceo@company.com", "Test Subject", vipEmails, Enumerable.Empty<ParserRule>());

        // Assert
        result.IsVip.Should().BeTrue();
        result.MatchedVipEmail.Should().NotBeNull();
        result.MatchedVipEmail!.EmailAddress.Should().Be("ceo@company.com");
    }

    [Fact]
    public void Evaluate_VipEmailCaseInsensitive_MatchesCorrectly()
    {
        // Arrange
        var vipEmails = new List<VipEmail>
        {
            CreateVipEmail("CEO@COMPANY.COM")
        };

        // Act
        var result = _service.Evaluate("ceo@company.com", "Test Subject", vipEmails, Enumerable.Empty<ParserRule>());

        // Assert
        result.IsVip.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_VipEmailNotInList_ReturnsIsVipFalse()
    {
        // Arrange
        var vipEmails = new List<VipEmail>
        {
            CreateVipEmail("ceo@company.com")
        };

        // Act
        var result = _service.Evaluate("random@other.com", "Test Subject", vipEmails, Enumerable.Empty<ParserRule>());

        // Assert
        result.IsVip.Should().BeFalse();
        result.MatchedVipEmail.Should().BeNull();
    }

    [Fact]
    public void Evaluate_InactiveVipEmail_IsNotMatched()
    {
        // Arrange
        var vipEmails = new List<VipEmail>
        {
            CreateVipEmail("ceo@company.com", isActive: false)
        };

        // Act
        var result = _service.Evaluate("ceo@company.com", "Test Subject", vipEmails, Enumerable.Empty<ParserRule>());

        // Assert
        result.IsVip.Should().BeFalse();
        result.MatchedVipEmail.Should().BeNull();
    }

    [Fact]
    public void Evaluate_VipEmailWithNotifyUser_ReturnsNotifyUserId()
    {
        // Arrange
        var notifyUserId = Guid.NewGuid();
        var vipEmails = new List<VipEmail>
        {
            CreateVipEmail("ceo@company.com", notifyUserId: notifyUserId)
        };

        // Act
        var result = _service.Evaluate("ceo@company.com", "Test Subject", vipEmails, Enumerable.Empty<ParserRule>());

        // Assert
        result.NotifyUserId.Should().Be(notifyUserId);
    }

    [Fact]
    public void Evaluate_VipEmailWithNotifyRole_ReturnsNotifyRole()
    {
        // Arrange
        var vipEmails = new List<VipEmail>
        {
            CreateVipEmail("ceo@company.com", notifyRole: "Admin")
        };

        // Act
        var result = _service.Evaluate("ceo@company.com", "Test Subject", vipEmails, Enumerable.Empty<ParserRule>());

        // Assert
        result.NotifyRole.Should().Be("Admin");
    }

    #endregion

    #region Rule Evaluation Tests

    [Fact]
    public void Evaluate_RuleWithFromAddressPattern_MatchesCorrectly()
    {
        // Arrange
        var rules = new List<ParserRule>
        {
            CreateRule(fromPattern: "*@time.com.my", actionType: "MarkVipOnly")
        };

        // Act
        var result = _service.Evaluate("noreply@time.com.my", "Test Subject", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.IsVip.Should().BeTrue();
        result.MatchedRule.Should().NotBeNull();
    }

    [Fact]
    public void Evaluate_RuleWithDomainPattern_MatchesCorrectly()
    {
        // Arrange
        var rules = new List<ParserRule>
        {
            CreateRule(domainPattern: "time.com.my", actionType: "MarkVipOnly")
        };

        // Act
        var result = _service.Evaluate("noreply@time.com.my", "Test Subject", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.IsVip.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_RuleWithSubjectContains_MatchesCorrectly()
    {
        // Arrange
        var rules = new List<ParserRule>
        {
            CreateRule(subjectContains: "URGENT", actionType: "MarkVipOnly")
        };

        // Act
        var result = _service.Evaluate("anyone@test.com", "URGENT: Please respond", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.IsVip.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_RuleWithSubjectContains_CaseInsensitive()
    {
        // Arrange
        var rules = new List<ParserRule>
        {
            CreateRule(subjectContains: "urgent", actionType: "MarkVipOnly")
        };

        // Act
        var result = _service.Evaluate("anyone@test.com", "URGENT: Please respond", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.IsVip.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_InactiveRule_IsNotEvaluated()
    {
        // Arrange
        var rules = new List<ParserRule>
        {
            CreateRule(fromPattern: "*@time.com.my", actionType: "MarkVipOnly", isActive: false)
        };

        // Act
        var result = _service.Evaluate("noreply@time.com.my", "Test Subject", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.IsVip.Should().BeFalse();
        result.MatchedRule.Should().BeNull();
    }

    [Fact]
    public void Evaluate_MultipleRules_HighestPriorityWins()
    {
        // Arrange
        var lowPriorityDeptId = Guid.NewGuid();
        var highPriorityUserId = Guid.NewGuid();

        var rules = new List<ParserRule>
        {
            CreateRule(fromPattern: "*@time.com.my", actionType: "RouteToDepartment", priority: 10, targetDepartmentId: lowPriorityDeptId),
            CreateRule(fromPattern: "*@time.com.my", actionType: "RouteToUser", priority: 100, targetUserId: highPriorityUserId)
        };

        // Act
        var result = _service.Evaluate("noreply@time.com.my", "Test Subject", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.TargetUserId.Should().Be(highPriorityUserId);
        result.TargetDepartmentId.Should().BeNull();
    }

    #endregion

    #region Action Type Tests

    [Fact]
    public void Evaluate_IgnoreAction_SetsShoudIgnoreTrue()
    {
        // Arrange
        var rules = new List<ParserRule>
        {
            CreateRule(fromPattern: "*@spam.com", actionType: "Ignore")
        };

        // Act
        var result = _service.Evaluate("spammer@spam.com", "Buy now!", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.ShouldIgnore.Should().BeTrue();
        result.Action.Should().Be(EmailRuleActionType.Ignore);
    }

    [Fact]
    public void Evaluate_MarkVipOnlyAction_SetsIsVipTrue()
    {
        // Arrange
        var rules = new List<ParserRule>
        {
            CreateRule(fromPattern: "*@vip.com", actionType: "MarkVipOnly")
        };

        // Act
        var result = _service.Evaluate("important@vip.com", "Test", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.IsVip.Should().BeTrue();
        result.Action.Should().Be(EmailRuleActionType.MarkVipOnly);
    }

    [Fact]
    public void Evaluate_RouteToDepartmentAction_SetsTargetDepartmentId()
    {
        // Arrange
        var deptId = Guid.NewGuid();
        var rules = new List<ParserRule>
        {
            CreateRule(fromPattern: "*@partner.com", actionType: "RouteToDepartment", targetDepartmentId: deptId)
        };

        // Act
        var result = _service.Evaluate("contact@partner.com", "Test", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.TargetDepartmentId.Should().Be(deptId);
        result.Action.Should().Be(EmailRuleActionType.RouteToDepartment);
    }

    [Fact]
    public void Evaluate_RouteToUserAction_SetsTargetUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var rules = new List<ParserRule>
        {
            CreateRule(fromPattern: "*@partner.com", actionType: "RouteToUser", targetUserId: userId)
        };

        // Act
        var result = _service.Evaluate("contact@partner.com", "Test", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.TargetUserId.Should().Be(userId);
        result.Action.Should().Be(EmailRuleActionType.RouteToUser);
    }

    [Fact]
    public void Evaluate_MarkVipAndRouteToDepartment_SetsBothFields()
    {
        // Arrange
        var deptId = Guid.NewGuid();
        var rules = new List<ParserRule>
        {
            CreateRule(fromPattern: "*@vip-partner.com", actionType: "MarkVipAndRouteToDepartment", targetDepartmentId: deptId)
        };

        // Act
        var result = _service.Evaluate("ceo@vip-partner.com", "Test", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.IsVip.Should().BeTrue();
        result.TargetDepartmentId.Should().Be(deptId);
        result.Action.Should().Be(EmailRuleActionType.MarkVipAndRouteToDepartment);
    }

    [Fact]
    public void Evaluate_MarkVipAndRouteToUser_SetsBothFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var rules = new List<ParserRule>
        {
            CreateRule(fromPattern: "*@vip-partner.com", actionType: "MarkVipAndRouteToUser", targetUserId: userId)
        };

        // Act
        var result = _service.Evaluate("ceo@vip-partner.com", "Test", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.IsVip.Should().BeTrue();
        result.TargetUserId.Should().Be(userId);
        result.Action.Should().Be(EmailRuleActionType.MarkVipAndRouteToUser);
    }

    [Fact]
    public void Evaluate_RuleWithIsVipFlag_SetsIsVipTrue()
    {
        // Arrange
        var rules = new List<ParserRule>
        {
            CreateRule(fromPattern: "*@important.com", actionType: "RouteToDepartment", isVip: true, targetDepartmentId: Guid.NewGuid())
        };

        // Act
        var result = _service.Evaluate("anyone@important.com", "Test", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.IsVip.Should().BeTrue();
    }

    #endregion

    #region Combined VIP and Rule Tests

    [Fact]
    public void Evaluate_BothVipEmailAndRule_BothAreRecorded()
    {
        // Arrange
        var vipEmails = new List<VipEmail>
        {
            CreateVipEmail("ceo@company.com", notifyUserId: Guid.NewGuid())
        };
        var rules = new List<ParserRule>
        {
            CreateRule(fromPattern: "*@company.com", actionType: "RouteToDepartment", targetDepartmentId: Guid.NewGuid())
        };

        // Act
        var result = _service.Evaluate("ceo@company.com", "Test", vipEmails, rules);

        // Assert
        result.IsVip.Should().BeTrue();
        result.MatchedVipEmail.Should().NotBeNull();
        result.MatchedRule.Should().NotBeNull();
    }

    [Fact]
    public void Evaluate_IgnoreRuleStopsEvaluation_NoFurtherRulesProcessed()
    {
        // Arrange
        var rules = new List<ParserRule>
        {
            CreateRule(fromPattern: "*@spam.com", actionType: "Ignore", priority: 100),
            CreateRule(fromPattern: "*@spam.com", actionType: "MarkVipOnly", priority: 50) // Lower priority
        };

        // Act
        var result = _service.Evaluate("spammer@spam.com", "Test", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.ShouldIgnore.Should().BeTrue();
        result.IsVip.Should().BeFalse(); // MarkVipOnly rule was not evaluated
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Evaluate_EmptyFromAddress_ReturnsEmptyResult()
    {
        // Act
        var result = _service.Evaluate("", "Test Subject", Enumerable.Empty<VipEmail>(), Enumerable.Empty<ParserRule>());

        // Assert
        result.IsVip.Should().BeFalse();
        result.MatchedVipEmail.Should().BeNull();
        result.MatchedRule.Should().BeNull();
    }

    [Fact]
    public void Evaluate_NoVipEmailsOrRules_ReturnsEmptyResult()
    {
        // Act
        var result = _service.Evaluate("anyone@test.com", "Test Subject", Enumerable.Empty<VipEmail>(), Enumerable.Empty<ParserRule>());

        // Assert
        result.IsVip.Should().BeFalse();
        result.ShouldIgnore.Should().BeFalse();
        result.MatchedVipEmail.Should().BeNull();
        result.MatchedRule.Should().BeNull();
    }

    [Fact]
    public void Evaluate_RuleWithNoPatterns_DoesNotMatch()
    {
        // Arrange - Rule with no patterns specified
        var rules = new List<ParserRule>
        {
            CreateRule(actionType: "MarkVipOnly") // No from, domain, or subject pattern
        };

        // Act
        var result = _service.Evaluate("anyone@test.com", "Test Subject", Enumerable.Empty<VipEmail>(), rules);

        // Assert
        result.IsVip.Should().BeFalse();
        result.MatchedRule.Should().BeNull();
    }

    [Fact]
    public void Evaluate_WhitespaceInEmail_IsTrimmed()
    {
        // Arrange
        var vipEmails = new List<VipEmail>
        {
            CreateVipEmail("ceo@company.com")
        };

        // Act
        var result = _service.Evaluate("  ceo@company.com  ", "Test Subject", vipEmails, Enumerable.Empty<ParserRule>());

        // Assert
        result.IsVip.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static VipEmail CreateVipEmail(
        string emailAddress,
        Guid? notifyUserId = null,
        string? notifyRole = null,
        bool isActive = true)
    {
        return new VipEmail
        {
            Id = Guid.NewGuid(),
            EmailAddress = emailAddress,
            NotifyUserId = notifyUserId,
            NotifyRole = notifyRole,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = Guid.NewGuid()
        };
    }

    private static ParserRule CreateRule(
        string? fromPattern = null,
        string? domainPattern = null,
        string? subjectContains = null,
        string actionType = "MarkVipOnly",
        int priority = 100,
        bool isActive = true,
        bool isVip = false,
        Guid? targetDepartmentId = null,
        Guid? targetUserId = null)
    {
        return new ParserRule
        {
            Id = Guid.NewGuid(),
            FromAddressPattern = fromPattern,
            DomainPattern = domainPattern,
            SubjectContains = subjectContains,
            ActionType = actionType,
            Priority = priority,
            IsActive = isActive,
            IsVip = isVip,
            TargetDepartmentId = targetDepartmentId,
            TargetUserId = targetUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = Guid.NewGuid()
        };
    }

    #endregion
}

