using System.Security.Claims;
using CephasOps.Api.Services;
using CephasOps.Application.Common;
using CephasOps.Application.Common.DTOs;
using CephasOps.Application.Common.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace CephasOps.Api.Tests.Services;

/// <summary>
/// Tenant resolution: header override (SuperAdmin), JWT company, department fallback, and unresolved.
/// </summary>
public class TenantProviderTests
{
    private static readonly Guid CompanyA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid CompanyB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid UserId1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static TenantProvider CreateSut(
        Guid? jwtCompanyId,
        bool isSuperAdmin,
        string? xCompanyIdHeader = null,
        Guid? userId = null,
        DepartmentCompanyResolutionResult? departmentResult = null)
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.CompanyId).Returns(jwtCompanyId);
        currentUser.Setup(c => c.IsSuperAdmin).Returns(isSuperAdmin);
        currentUser.Setup(c => c.UserId).Returns(userId);

        var context = new DefaultHttpContext();
        if (!string.IsNullOrEmpty(xCompanyIdHeader))
            context.Request.Headers["X-Company-Id"] = xCompanyIdHeader;

        // TenantProvider step 2 reads company from User.Claims (companyId/company_id), not from ICurrentUserService.
        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            claims.Add(new Claim("sub", userId.Value.ToString()));
        }
        if (jwtCompanyId.HasValue && jwtCompanyId.Value != Guid.Empty)
        {
            claims.Add(new Claim("companyId", jwtCompanyId.Value.ToString()));
            claims.Add(new Claim("company_id", jwtCompanyId.Value.ToString()));
        }
        if (isSuperAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "SuperAdmin"));
        if (claims.Count > 0)
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(a => a.HttpContext).Returns(context);

        var departmentResolver = new Mock<IUserCompanyFromDepartmentResolver>();
        departmentResolver
            .Setup(r => r.TryGetSingleCompanyFromDepartmentsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(departmentResult ?? DepartmentCompanyResolutionResult.None);

        var logger = new Mock<ILogger<TenantProvider>>();
        var options = Microsoft.Extensions.Options.Options.Create(new TenantOptions { DefaultCompanyId = null });

        return new TenantProvider(
            currentUser.Object,
            httpContextAccessor.Object,
            departmentResolver.Object,
            options,
            logger.Object);
    }

    [Fact]
    public async Task HeaderOverride_SuperAdmin_WithValidXCompanyId_Wins()
    {
        var sut = CreateSut(CompanyA, isSuperAdmin: true, xCompanyIdHeader: CompanyB.ToString());

        await sut.GetEffectiveCompanyIdAsync();
        sut.CurrentTenantId.Should().Be(CompanyB);
    }

    [Fact]
    public async Task JwtCompanyId_WinsOverDepartmentFallback()
    {
        var sut = CreateSut(
            CompanyA,
            isSuperAdmin: false,
            xCompanyIdHeader: null,
            userId: UserId1,
            departmentResult: DepartmentCompanyResolutionResult.Single(CompanyB));

        await sut.GetEffectiveCompanyIdAsync();
        sut.CurrentTenantId.Should().Be(CompanyA);
    }

    [Fact]
    public async Task DepartmentFallback_WhenJwtMissing_SingleCompany_Resolves()
    {
        var sut = CreateSut(
            null,
            isSuperAdmin: false,
            xCompanyIdHeader: null,
            userId: UserId1,
            departmentResult: DepartmentCompanyResolutionResult.Single(CompanyA));

        await sut.GetEffectiveCompanyIdAsync();
        sut.CurrentTenantId.Should().Be(CompanyA);
    }

    [Fact]
    public async Task DepartmentFallback_AmbiguousMultiCompany_Unresolved()
    {
        var sut = CreateSut(
            null,
            isSuperAdmin: false,
            xCompanyIdHeader: null,
            userId: UserId1,
            departmentResult: DepartmentCompanyResolutionResult.AmbiguousMultiCompany);

        await sut.GetEffectiveCompanyIdAsync();
        sut.CurrentTenantId.Should().BeNull();
    }

    [Fact]
    public async Task DepartmentFallback_NoDepartmentCompany_Unresolved()
    {
        var sut = CreateSut(
            null,
            isSuperAdmin: false,
            xCompanyIdHeader: null,
            userId: UserId1,
            departmentResult: DepartmentCompanyResolutionResult.None);

        await sut.GetEffectiveCompanyIdAsync();
        sut.CurrentTenantId.Should().BeNull();
    }

    [Fact]
    public async Task NormalUser_WithSpoofedXCompanyId_IgnoresHeader_UsesJwtCompanyId()
    {
        var sut = CreateSut(CompanyA, isSuperAdmin: false, xCompanyIdHeader: CompanyB.ToString());

        await sut.GetEffectiveCompanyIdAsync();
        sut.CurrentTenantId.Should().Be(CompanyA);
    }

    [Fact]
    public async Task SuperAdmin_WithNoHeader_UsesJwtCompanyId()
    {
        var sut = CreateSut(CompanyA, isSuperAdmin: true, xCompanyIdHeader: null);

        await sut.GetEffectiveCompanyIdAsync();
        sut.CurrentTenantId.Should().Be(CompanyA);
    }

    [Fact]
    public async Task NoUserId_NoJwtCompany_Unresolved()
    {
        var sut = CreateSut(null, isSuperAdmin: false, userId: null);

        await sut.GetEffectiveCompanyIdAsync();
        sut.CurrentTenantId.Should().BeNull();
    }

    [Fact]
    public async Task SuperAdmin_EmptyHeader_DoesNotUseHeader_UsesJwt()
    {
        var sut = CreateSut(CompanyA, isSuperAdmin: true, xCompanyIdHeader: "");

        await sut.GetEffectiveCompanyIdAsync();
        sut.CurrentTenantId.Should().Be(CompanyA);
    }

    [Fact]
    public async Task JwtCompanyIdEmpty_TreatedAsMissing_DepartmentFallbackUsed()
    {
        var sut = CreateSut(
            Guid.Empty,
            isSuperAdmin: false,
            userId: UserId1,
            departmentResult: DepartmentCompanyResolutionResult.Single(CompanyA));

        await sut.GetEffectiveCompanyIdAsync();
        sut.CurrentTenantId.Should().Be(CompanyA);
    }
}
