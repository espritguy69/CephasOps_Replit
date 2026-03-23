using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CephasOps.TenantSafetyAnalyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CephasOpsTenantSafetyAnalyzer : DiagnosticAnalyzer
{
    public const string Cephas001Id = "CEPHAS001";
    public const string Cephas002Id = "CEPHAS002";
    public const string Cephas003Id = "CEPHAS003";

    /// <summary>Help link base for analyzer docs (repo-specific; update if org/repo differs).</summary>
    private const string HelpLinkBase = "https://github.com/CephasOps/CephasOps/blob/main/backend/docs/operations/TENANT_SAFETY_ANALYZER.md";

    private static readonly LocalizableString Cephas001Title = "Unsafe IgnoreQueryFilters on tenant-scoped entity";
    private static readonly LocalizableString Cephas001MessageFormat = "IgnoreQueryFilters is used on a tenant-scoped set without an explicit CompanyId/TenantId constraint. Add a Where clause that constrains by company or tenant.";
    private static readonly LocalizableString Cephas002Title = "IgnoreQueryFilters with ambiguous tenant scoping";
    private static readonly LocalizableString Cephas002MessageFormat = "IgnoreQueryFilters is used but company/tenant scoping is not obvious in this query chain. Ensure the query is constrained by CompanyId or document why it is safe.";
    private static readonly LocalizableString Cephas003Title = "Possible EF Core relationship fixup risk";
    private static readonly LocalizableString Cephas003MessageFormat = "Navigation property is mutated (e.g. {0}.{1}.{2} = ...) and a guarded lookup exists, but no obvious null-clearing of the navigation. If the guarded lookup returns null, set the navigation to null to avoid updating a fixup-attached entity from another tenant. See EFCORE_RELATIONSHIP_FIXUP_RISK.md.";

    private static readonly string Category = "CephasOps.TenantSafety";

    private static readonly DiagnosticDescriptor Cephas001 = new DiagnosticDescriptor(
        Cephas001Id,
        Cephas001Title,
        Cephas001MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Unsafe IgnoreQueryFilters usage on tenant-scoped entity.",
        helpLinkUri: HelpLinkBase + "#cephas001");

    private static readonly DiagnosticDescriptor Cephas002 = new DiagnosticDescriptor(
        Cephas002Id,
        Cephas002Title,
        Cephas002MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "IgnoreQueryFilters usage with ambiguous or non-obvious tenant scoping.",
        helpLinkUri: HelpLinkBase + "#cephas002");

    private static readonly DiagnosticDescriptor Cephas003 = new DiagnosticDescriptor(
        Cephas003Id,
        Cephas003Title,
        Cephas003MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Possible EF Core relationship fixup risk when mutating a navigation target without clearing invalid navigation state.",
        helpLinkUri: HelpLinkBase + "#cephas003");

    public const string Cephas004Id = "CEPHAS004";

    private static readonly LocalizableString Cephas004Title = "Tenant-scoped entity queried by Id only without explicit company scoping";
    private static readonly LocalizableString Cephas004MessageFormat = "Tenant-scoped set '{0}' is queried by Id (or key) only without an explicit CompanyId/TenantId constraint. Add a predicate that constrains by company or tenant to avoid cross-tenant reads.";

    private static readonly DiagnosticDescriptor Cephas004 = new DiagnosticDescriptor(
        Cephas004Id,
        Cephas004Title,
        Cephas004MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Tenant-scoped entity queried by Id only without explicit company scoping.",
        helpLinkUri: HelpLinkBase + "#cephas004");

    /// <summary>Query method names that take a predicate (e.g. FirstOrDefaultAsync(x => x.Id == id)). Used for CEPHAS004.</summary>
    private static readonly ImmutableHashSet<string> QueryPredicateMethodNames = ImmutableHashSet.Create(
        "FirstOrDefaultAsync", "FirstAsync", "SingleOrDefaultAsync", "SingleAsync");

    /// <summary>Query method names that take a key only (e.g. FindAsync(id), Find(id)). Used for CEPHAS004.</summary>
    private static readonly ImmutableHashSet<string> QueryByKeyMethodNames = ImmutableHashSet.Create(
        "FindAsync", "Find");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Cephas001, Cephas002, Cephas003, Cephas004);

    /// <summary>Known tenant-scoped entity/set names (DbSet or type names). Used for CEPHAS001.</summary>
    private static readonly ImmutableHashSet<string> TenantScopedSetNames = ImmutableHashSet.Create(
        "Orders", "Order", "Assets", "Asset", "AssetDisposals", "AssetDisposal",
        "BillingRatecards", "BillingRatecard", "ServiceInstallers", "ServiceInstaller",
        "OrderCategories", "OrderCategory", "BuildingTypes", "BuildingType",
        "Materials", "Material", "MaterialCategories", "MaterialCategory",
        "Partners", "Partner", "Invoices", "Invoice", "Departments", "Department",
        "DepartmentMemberships", "DepartmentMembership", "Buildings", "Building",
        "GponSiJobRates", "GponSiJobRate", "OrderPayoutSnapshots", "OrderPayoutSnapshot"
    );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
    }

    private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var name = memberAccess.Name.Identifier.ValueText;

        // CEPHAS004: tenant-scoped set queried by Id only (predicate-based or key-based)
        if (QueryPredicateMethodNames.Contains(name))
        {
            TryReportCephas004Predicate(context, invocation, memberAccess);
            return;
        }
        if (QueryByKeyMethodNames.Contains(name))
        {
            TryReportCephas004ByKey(context, invocation, memberAccess);
            return;
        }

        if (name != "IgnoreQueryFilters")
            return;

        // Skip generated and migration files
        var location = invocation.GetLocation();
        if (IsInGeneratedOrMigrationFile(context, location))
            return;

        // Find the chain: we need the receiver of IgnoreQueryFilters (e.g. _context.Orders)
        // and then the rest of the chain (Where, FirstOrDefaultAsync, etc.)
        var receiver = memberAccess.Expression;
        string? setOrTypeName = null;

        if (receiver is MemberAccessExpressionSyntax receiverMember)
        {
            setOrTypeName = receiverMember.Name.Identifier.ValueText;
        }
        else if (receiver is IdentifierNameSyntax id)
        {
            setOrTypeName = id.Identifier.ValueText;
        }

        if (string.IsNullOrEmpty(setOrTypeName))
            return;

        var chainHasCompanyScoping = QueryChainHasCompanyOrTenantScoping(invocation, semanticModel);

        if (TenantScopedSetNames.Contains(setOrTypeName!))
        {
            if (!chainHasCompanyScoping)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Cephas001,
                    location,
                    "Tenant-scoped set without CompanyId/TenantId constraint"));
            }
            return;
        }

        // Not a known tenant-scoped set; if no scoping, report CEPHAS002 (review)
        if (!chainHasCompanyScoping)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Cephas002,
                location,
                "Company/tenant scoping not obvious"));
        }
    }

    private void TryReportCephas004Predicate(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax memberAccess)
    {
        var location = invocation.GetLocation();
        if (IsInGeneratedOrMigrationFile(context, location))
            return;

        var receiver = memberAccess.Expression;
        string? setOrTypeName = GetSetOrTypeName(receiver);
        if (string.IsNullOrEmpty(setOrTypeName) || !TenantScopedSetNames.Contains(setOrTypeName!))
            return;

        if (invocation.ArgumentList.Arguments.Count == 0)
            return;
        var firstArg = invocation.ArgumentList.Arguments[0].Expression;
        var predicateText = firstArg.ToString();
        if (predicateText.IndexOf("CompanyId", System.StringComparison.Ordinal) >= 0)
            return;
        if (predicateText.IndexOf("TenantId", System.StringComparison.Ordinal) >= 0)
            return;
        if (predicateText.IndexOf("CurrentTenantId", System.StringComparison.Ordinal) >= 0)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Cephas004,
            location,
            setOrTypeName));
    }

    private void TryReportCephas004ByKey(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax memberAccess)
    {
        var location = invocation.GetLocation();
        if (IsInGeneratedOrMigrationFile(context, location))
            return;

        string? setOrTypeName = GetSetOrTypeName(memberAccess.Expression);
        if (string.IsNullOrEmpty(setOrTypeName) || !TenantScopedSetNames.Contains(setOrTypeName!))
            return;

        var statement = invocation.FirstAncestorOrSelf<StatementSyntax>();
        var block = statement?.FirstAncestorOrSelf<BlockSyntax>();
        var text = (block ?? statement)?.ToString() ?? invocation.ToString();
        if (text.IndexOf("CompanyId", System.StringComparison.Ordinal) >= 0)
            return;
        if (text.IndexOf("TenantId", System.StringComparison.Ordinal) >= 0)
            return;
        if (text.IndexOf("CurrentTenantId", System.StringComparison.Ordinal) >= 0)
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            Cephas004,
            location,
            setOrTypeName));
    }

    private static string? GetSetOrTypeName(SyntaxNode receiver)
    {
        if (receiver is MemberAccessExpressionSyntax receiverMember)
            return receiverMember.Name.Identifier.ValueText;
        if (receiver is IdentifierNameSyntax id)
            return id.Identifier.ValueText;
        return null;
    }

    private static bool QueryChainHasCompanyOrTenantScoping(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        // Walk up and down the chain from this invocation
        var node = invocation.Parent;
        var startInvocation = invocation;

        // Look at the entire statement / chain: same statement can have Where(x => x.CompanyId == ...)
        var statement = invocation.FirstAncestorOrSelf<StatementSyntax>();
        if (statement == null)
            return false;

        var block = statement.FirstAncestorOrSelf<BlockSyntax>();
        if (block == null)
            return false;

        // Look at the invocation and following sibling invocations (chained calls)
        var text = block.ToString();
        if (text.IndexOf("CompanyId", System.StringComparison.Ordinal) >= 0) return true;
        if (text.IndexOf("TenantId", System.StringComparison.Ordinal) >= 0) return true;
        if (text.IndexOf("CurrentTenantId", System.StringComparison.Ordinal) >= 0) return true;
        if (text.IndexOf("disposal.CompanyId", System.StringComparison.Ordinal) >= 0) return true;
        if (text.IndexOf("order.CompanyId", System.StringComparison.Ordinal) >= 0) return true;

        // Also check the immediate chain: invocation might be _context.Orders.IgnoreQueryFilters().Where(...)
        var chainRoot = GetChainRoot(startInvocation);
        var chainText = GetInvocationChainText(chainRoot);
        if (chainText.IndexOf("CompanyId", System.StringComparison.Ordinal) >= 0) return true;
        if (chainText.IndexOf("TenantId", System.StringComparison.Ordinal) >= 0) return true;
        if (chainText.IndexOf("CurrentTenantId", System.StringComparison.Ordinal) >= 0) return true;

        return false;
    }

    private static SyntaxNode? GetChainRoot(SyntaxNode node)
    {
        var current = node;
        while (current?.Parent is InvocationExpressionSyntax inv)
        {
            current = inv;
        }
        while (current?.Parent is MemberAccessExpressionSyntax)
        {
            current = current.Parent;
        }
        return current;
    }

    private static string GetInvocationChainText(SyntaxNode? node)
    {
        if (node == null) return "";
        var n = node;
        var sb = new System.Text.StringBuilder();
        while (n != null)
        {
            sb.Append(n.ToString());
            n = n.Parent;
            if (n is BlockSyntax or MethodDeclarationSyntax)
                break;
        }
        return sb.ToString();
    }

    private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Left side must be of form X.Nav.Prop (three parts)
        if (assignment.Left is not MemberAccessExpressionSyntax outer)
            return;

        if (outer.Expression is not MemberAccessExpressionSyntax inner)
            return;

        var propName = outer.Name.Identifier.ValueText;
        var navName = inner.Name.Identifier.ValueText;

        // We care about mutations like Status, UpdatedAt, IsDeleted
        if (propName != "Status" && propName != "UpdatedAt" && propName != "IsDeleted")
            return;

        var rootName = inner.Expression switch
        {
            IdentifierNameSyntax id => id.Identifier.ValueText,
            MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
            _ => null
        };
        if (string.IsNullOrEmpty(rootName))
            return;

        if (IsInGeneratedOrMigrationFile(context, assignment.GetLocation()))
            return;

        var method = assignment.FirstAncestorOrSelf<MethodDeclarationSyntax>();
        if (method == null)
            return;

        var methodBlock = method.Body ?? (SyntaxNode?)method.ExpressionBody;
        if (methodBlock == null)
            return;

        var blockText = methodBlock.ToString();
        var hasGuardedLookup = blockText.IndexOf("FirstOrDefaultAsync", System.StringComparison.Ordinal) >= 0
            || blockText.IndexOf("FirstAsync", System.StringComparison.Ordinal) >= 0;
        if (!hasGuardedLookup)
            return;

        var hasCompanyInBlock = blockText.IndexOf("CompanyId", System.StringComparison.Ordinal) >= 0;
        if (!hasCompanyInBlock)
            return;

        // Check for null-clearing: disposal.Asset = null or similar
        var nullAssignPattern = $".{navName}\\s*=\\s*null";
        var hasNullClearing = System.Text.RegularExpressions.Regex.IsMatch(blockText, nullAssignPattern);

        if (hasGuardedLookup && hasCompanyInBlock && !hasNullClearing)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Cephas003,
                assignment.Left.GetLocation(),
                rootName,
                navName,
                propName));
        }
    }

    private static bool IsInGeneratedOrMigrationFile(SyntaxNodeAnalysisContext context, Location location)
    {
        var filePath = location.SourceTree?.FilePath ?? "";
        if (filePath.IndexOf(".Designer.", System.StringComparison.Ordinal) >= 0) return true;
        if (filePath.IndexOf(".g.", System.StringComparison.Ordinal) >= 0) return true;
        if (filePath.IndexOf("Migrations", System.StringComparison.Ordinal) >= 0 && filePath.EndsWith(".cs", System.StringComparison.Ordinal)) return true;
        return false;
    }
}
