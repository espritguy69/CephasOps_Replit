using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    CephasOps.TenantSafetyAnalyzers.CephasOpsTenantSafetyAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace CephasOps.TenantSafetyAnalyzers.Tests;

public class Cephas003FixupRiskTests
{
    [Fact]
    public async Task NoDiagnostic_WhenNavigationClearedInElse()
    {
        const string code = @"
class C
{
    public void M(Disposal disposal, Repo repo)
    {
        var a = repo.FirstOrDefaultAsync(disposal.AssetId, disposal.CompanyId);
        if (a != null)
            disposal.Asset = a;
        else
            disposal.Asset = null;
        if (disposal.Asset != null)
            disposal.Asset.Status = 0;
    }
}

class Repo { public Asset FirstOrDefaultAsync(System.Guid id, System.Guid companyId) => null; }
class Disposal { public System.Guid AssetId { get; set; } public System.Guid CompanyId { get; set; } public Asset Asset { get; set; } }
class Asset { public int Status { get; set; } }
";
        await Verify.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task CEPHAS003_WhenNavigationMutatedWithoutNullClearing()
    {
        const string code = @"
class C
{
    public void M(Disposal disposal, Repo repo)
    {
        var asset = repo.FirstOrDefaultAsync(disposal.AssetId, disposal.CompanyId);
        if (asset != null)
            disposal.Asset = asset;
        disposal.Asset.Status = 0;
    }
}

class Repo { public Asset FirstOrDefaultAsync(System.Guid id, System.Guid companyId) => null; }
class Disposal { public System.Guid AssetId { get; set; } public System.Guid CompanyId { get; set; } public Asset Asset { get; set; } }
class Asset { public int Status { get; set; } }
";
        var expected = Verify.Diagnostic(CephasOps.TenantSafetyAnalyzers.CephasOpsTenantSafetyAnalyzer.Cephas003Id)
            .WithSpan(9, 9, 9, 30)
            .WithArguments("disposal", "Asset", "Status")
            .WithMessage("Navigation property is mutated (e.g. disposal.Asset.Status = ...) and a guarded lookup exists, but no obvious null-clearing of the navigation. If the guarded lookup returns null, set the navigation to null to avoid updating a fixup-attached entity from another tenant. See EFCORE_RELATIONSHIP_FIXUP_RISK.md.")
            .WithSeverity(DiagnosticSeverity.Warning);
        await Verify.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task NoDiagnostic_WhenNoGuardedLookupInMethod()
    {
        const string code = @"
class C
{
    void M(Disposal d)
    {
        d.Asset.Status = 0;
    }
}

class Disposal { public Asset Asset { get; set; } }
class Asset { public int Status { get; set; } }
";
        await Verify.VerifyAnalyzerAsync(code);
    }
}
