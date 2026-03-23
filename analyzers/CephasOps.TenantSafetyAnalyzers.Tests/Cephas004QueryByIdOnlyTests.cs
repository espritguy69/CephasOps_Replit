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

public class Cephas004QueryByIdOnlyTests
{
    [Fact]
    public async Task CEPHAS004_WhenTenantScopedSetQueriedByIdOnly()
    {
        const string code = @"
using System.Linq;

class C
{
    void M(Db ctx, System.Guid id)
    {
        var o = ctx.Orders.FirstOrDefaultAsync(o => o.Id == id);
    }
}

class Db { public IQueryable<Order> Orders => null; }
class Order { public System.Guid Id { get; set; } public System.Guid CompanyId { get; set; } }
static class Ex { public static Order FirstOrDefaultAsync(this IQueryable<Order> q, System.Func<Order, bool> p) => null; }
";
        var expected = Verify.Diagnostic(CephasOps.TenantSafetyAnalyzers.CephasOpsTenantSafetyAnalyzer.Cephas004Id)
            .WithSpan(8, 17, 8, 64)
            .WithArguments("Orders")
            .WithSeverity(DiagnosticSeverity.Warning);
        await Verify.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task NoDiagnostic_WhenTenantScopedSetQueriedByIdAndCompanyId()
    {
        const string code = @"
using System.Linq;

class C
{
    void M(Db ctx, System.Guid id, System.Guid companyId)
    {
        var o = ctx.Orders.FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == companyId);
    }
}

class Db { public IQueryable<Order> Orders => null; }
class Order { public System.Guid Id { get; set; } public System.Guid CompanyId { get; set; } }
static class Ex { public static Order FirstOrDefaultAsync(this IQueryable<Order> q, System.Func<Order, bool> p) => null; }
";
        await Verify.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task NoDiagnostic_WhenNotTenantScopedSet()
    {
        const string code = @"
using System.Linq;

class C
{
    void M(Db ctx, System.Guid id)
    {
        var x = ctx.OtherSet.FirstOrDefaultAsync(x => x.Id == id);
    }
}

class Db { public IQueryable<Other> OtherSet => null; }
class Other { public System.Guid Id { get; set; } }
static class Ex { public static Other FirstOrDefaultAsync(this IQueryable<Other> q, System.Func<Other, bool> p) => null; }
";
        await Verify.VerifyAnalyzerAsync(code);
    }

    [Fact]
    public async Task CEPHAS004_WhenTenantScopedSetFindAsyncByIdOnly()
    {
        const string code = @"
using System.Threading.Tasks;

class C
{
    async System.Threading.Tasks.Task M(Db ctx, System.Guid id)
    {
        var o = await ctx.Orders.FindAsync(id);
    }
}

class Db { public DbSet<Order> Orders => null; }
class Order { public System.Guid Id { get; set; } public System.Guid CompanyId { get; set; } }
class DbSet<T> { public System.Threading.Tasks.Task<T> FindAsync(object key) => null; }
";
        var expected = Verify.Diagnostic(CephasOps.TenantSafetyAnalyzers.CephasOpsTenantSafetyAnalyzer.Cephas004Id)
            .WithSpan(8, 23, 8, 47)
            .WithArguments("Orders")
            .WithSeverity(DiagnosticSeverity.Warning);
        await Verify.VerifyAnalyzerAsync(code, expected);
    }

    [Fact]
    public async Task NoDiagnostic_WhenFindAsyncWithCompanyIdInBlock()
    {
        const string code = @"
using System.Threading.Tasks;

class C
{
    async System.Threading.Tasks.Task M(Db ctx, System.Guid id, System.Guid companyId)
    {
        var o = await ctx.Orders.FindAsync(id);
        if (o != null && o.CompanyId != companyId) return;
    }
}

class Db { public DbSet<Order> Orders => null; }
class Order { public System.Guid Id { get; set; } public System.Guid CompanyId { get; set; } }
class DbSet<T> { public System.Threading.Tasks.Task<T> FindAsync(object key) => null; }
";
        await Verify.VerifyAnalyzerAsync(code);
    }
}
