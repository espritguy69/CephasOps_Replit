using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    CephasOps.TenantSafetyAnalyzers.CephasOpsTenantSafetyAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace CephasOps.TenantSafetyAnalyzers.Tests;

public class Cephas001IgnoreQueryFiltersTests
{
    [Fact]
    public async Task NoDiagnostic_WhenExplicitCompanyScoping()
    {
        const string code = @"
using System.Linq;

class C
{
    void M(Db ctx, System.Guid id, System.Guid companyId)
    {
        var o = ctx.Orders.IgnoreQueryFilters().Where(x => x.Id == id && x.CompanyId == companyId).FirstOrDefault();
    }
}

class Db { public IQueryable<Order> Orders => null; }
class Order { public System.Guid Id { get; set; } public System.Guid CompanyId { get; set; } }
static class EfExtensions { public static IQueryable<T> IgnoreQueryFilters<T>(this IQueryable<T> q) => q; }
";
        await Verify.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task CEPHAS001_WhenTenantScopedSetWithoutCompanyScoping()
    {
        const string code = @"
using System.Linq;

class C
{
    void M(Db ctx, System.Guid id)
    {
        var o = ctx.Orders.IgnoreQueryFilters().Where(x => x.Id == id).FirstOrDefault();
    }
}

class Db { public IQueryable<Order> Orders => null; }
class Order { public System.Guid Id { get; set; } public System.Guid CompanyId { get; set; } }
static class EfExtensions { public static IQueryable<T> IgnoreQueryFilters<T>(this IQueryable<T> q) => q; }
";
        var expected = Verify.Diagnostic(CephasOps.TenantSafetyAnalyzers.CephasOpsTenantSafetyAnalyzer.Cephas001Id).WithSpan(8, 17, 8, 48).WithSeverity(DiagnosticSeverity.Warning);
        await Verify.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task CEPHAS002_WhenNotTenantScopedSetButNoScoping()
    {
        const string code = @"
using System.Linq;

class C
{
    void M(Db ctx, System.Guid id)
    {
        var x = ctx.OtherSet.IgnoreQueryFilters().Where(x => x.Id == id).FirstOrDefault();
    }
}

class Db { public IQueryable<Other> OtherSet => null; }
class Other { public System.Guid Id { get; set; } }
static class EfExtensions { public static IQueryable<T> IgnoreQueryFilters<T>(this IQueryable<T> q) => q; }
";
        var expected = Verify.Diagnostic(CephasOps.TenantSafetyAnalyzers.CephasOpsTenantSafetyAnalyzer.Cephas002Id).WithSpan(8, 17, 8, 50).WithSeverity(DiagnosticSeverity.Warning);
        await Verify.VerifyAnalyzerAsync(code, expected);
    }
}
