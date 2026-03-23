using CephasOps.Api;
using CephasOps.Application.Admin.Services;
using CephasOps.Application.Auth.Services;
using CephasOps.Application.Billing.Services;
using CephasOps.Application.Companies.Services; // PartnerService still needed
using CephasOps.Application.Buildings.Services;
using CephasOps.Application.ServiceInstallers.Services;
using CephasOps.Application.Departments.Services;
using CephasOps.Application.Files.Services;
using CephasOps.Application.Inventory.Services;
using CephasOps.Application.Notifications.Services;
using CephasOps.Application.Orders.Services;
using CephasOps.Application.Parser.DTOs;
using CephasOps.Application.Parser.Services;
using CephasOps.Application.Agent.Services;
using CephasOps.Application.Payroll.Services;
using CephasOps.Application.Pnl.Services;
using CephasOps.Application.Assets.Services;
using CephasOps.Application.RMA.Services;
using CephasOps.Application.Settings.Services;
using CephasOps.Application.Documents;
using CephasOps.Application.Scheduler.Services;
using CephasOps.Application.Common.Services;
using CephasOps.Application.Tasks.Services;
using CephasOps.Application.Commands;
using CephasOps.Application.Commands.Pipeline;
using CephasOps.Application.Integration;
using CephasOps.Application.Workflow.Services;
using CephasOps.Api.Integration;
using CephasOps.Application.Rates.Services;
using CephasOps.Application.Authorization;
using CephasOps.Application.Common.Interfaces;
using CephasOps.Application.Settings;
using CephasOps.Api.Authorization;
using CephasOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using CephasOps.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using CephasOps.Api.ExceptionHandling;
using CephasOps.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Linq;
using Syncfusion.Licensing;
using Serilog;
using Serilog.Events;
using OpenTelemetry.Metrics;
using OpenTelemetry.Instrumentation.AspNetCore;

// CRITICAL: Register CodePages encoding provider for .xls file support
// This is required for Syncfusion to properly read Excel 97-2003 (.xls) files
// Without this, .xls files may fail with encoding-related exceptions
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Configure Serilog for file and console logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .MinimumLevel.Override("CephasOps.Application.Parser.Services.SyncfusionExcelParserService", LogEventLevel.Verbose)
    .MinimumLevel.Override("CephasOps.Application.Parser.Services.SyncfusionExcelToPdfService", LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/cephasops-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Use Serilog for logging
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new CephasOps.Api.Converters.NullableGuidJsonConverter());
        // Configure JsonStringEnumConverter to use exact enum names (PascalCase)
        // This allows "InHouse" and "Subcontractor" to be converted correctly
        // Using null naming policy means exact enum names are used (case-sensitive)
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
    });
builder.Services.AddEndpointsApiExplorer();

// Memory Cache (for settings and other cached data)
builder.Services.AddMemoryCache();

// OpenTelemetry metrics (Prometheus scrape at /metrics when enabled)
if (builder.Configuration.GetValue("OpenTelemetry:Metrics:Enabled", true))
{
    builder.Services.AddOpenTelemetry()
        .WithMetrics(m => m
            .AddAspNetCoreInstrumentation()
            .AddPrometheusExporter()
            .AddMeter(CephasOps.Infrastructure.Metrics.TenantSafetyMetrics.MeterName)
            .AddMeter(CephasOps.Infrastructure.Metrics.TenantOperationalMetrics.MeterName));
}

// Swagger Configuration
builder.Services.AddSwaggerConfiguration();

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:5173", "http://localhost:3000", "http://localhost:5174" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// JWT Authentication Configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSecretKeyForJwtTokenGenerationThatShouldBeAtLeast32Characters";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CephasOps";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CephasOps";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero // Remove delay of token when expire
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }
    };
});

// Authorization Policies (minimal module policies per RBAC report)
builder.Services.AddAuthorization(options =>
{
    // No fallback policy - only endpoints with [Authorize] require authentication
    options.AddPolicy("Orders", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("Inventory", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("Reports", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("Jobs", policy => policy.RequireRole("SuperAdmin", "Admin"));
    options.AddPolicy("Settings", policy => policy.RequireRole("SuperAdmin", "Admin", "Director", "HeadOfDepartment", "Supervisor"));
});
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Global exception handler (RFC 7807 Problem Details + correlation ID)
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Database
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

if (builder.Environment.EnvironmentName == "Testing")
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseInMemoryDatabase("CephasOpsIntegrationTests");
        // Same as Npgsql path: tenant + soft-delete global filters on required ends of relationships trigger
        // CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning; mitigated by optional FKs and explicit IgnoreQueryFilters where needed.
        options.ConfigureWarnings(w =>
        {
            w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning);
        });
    });
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' not found. Set it via: (1) appsettings.Development.json \"ConnectionStrings\":{\"DefaultConnection\":\"...\"}, " +
            "(2) environment variable ConnectionStrings__DefaultConnection, or (3) user secrets: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"Host=...;Database=cephasops;...\"");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
            npgsqlOptions.CommandTimeout(30);
        });
        options.ConfigureWarnings(w =>
        {
            w.Ignore(RelationalEventId.PendingModelChangesWarning);
            // Global tenant/soft-delete filters on CompanyScopedEntity + some required navigations: EF warns about
            // filtered principal/dependent interaction. Building* optional FKs and targeted IgnoreQueryFilters address real cases.
            w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning);
        });
        if (builder.Environment.IsDevelopment())
            options.EnableSensitiveDataLogging();
    });
}

// File Upload Configuration
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 52428800; // 50 MB
});
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50 MB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// HTTP Context Accessor (for ICurrentUserService)
builder.Services.AddHttpContextAccessor();

// Application Services
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<IOneDriveSyncService, OneDriveSyncService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IOperationsOverviewService, OperationsOverviewService>();
builder.Services.AddScoped<ISiOperationalInsightsService, SiOperationalInsightsService>();
builder.Services.AddScoped<CephasOps.Api.Services.ExcelParseReportService>();
builder.Services.AddScoped<CephasOps.Application.Audit.Services.IAuditLogService, CephasOps.Application.Audit.Services.AuditLogService>();
builder.Services.AddScoped<CephasOps.Application.Auth.Services.ISecurityAnomalyDetectionService, CephasOps.Application.Auth.Services.SecurityAnomalyDetectionService>();
builder.Services.AddScoped<CephasOps.Application.Auth.Services.IUserSessionService, CephasOps.Application.Auth.Services.UserSessionService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.Configure<CephasOps.Application.Common.TenantOptions>(builder.Configuration.GetSection(CephasOps.Application.Common.TenantOptions.SectionName));
builder.Services.Configure<CephasOps.Api.Options.TenantRateLimitOptions>(builder.Configuration.GetSection(CephasOps.Api.Options.TenantRateLimitOptions.SectionName));
var redisConnection = builder.Configuration["ConnectionStrings:Redis"];
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ => StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnection));
    builder.Services.AddSingleton<CephasOps.Api.Services.RateLimit.IRateLimitStore, CephasOps.Api.Services.RateLimit.RedisRateLimitStore>();
}
else
    builder.Services.AddSingleton<CephasOps.Api.Services.RateLimit.IRateLimitStore, CephasOps.Api.Services.RateLimit.InMemoryRateLimitStore>();
builder.Services.AddScoped<CephasOps.Application.Common.Interfaces.IUserCompanyFromDepartmentResolver, CephasOps.Application.Common.Services.UserCompanyFromDepartmentResolver>();
builder.Services.AddScoped<CephasOps.Application.Common.Interfaces.ITenantProvider, CephasOps.Api.Services.TenantProvider>();
builder.Services.AddScoped<ITenantContext, CephasOps.Api.Services.TenantContextService>();
builder.Services.AddScoped<IDepartmentRequestContext, DepartmentRequestContext>();
builder.Services.AddScoped<CephasOps.Application.Common.Interfaces.IPasswordHasher, CephasOps.Application.Common.Services.CompatibilityPasswordHasher>();
builder.Services.Configure<CephasOps.Application.Auth.LockoutOptions>(builder.Configuration.GetSection(CephasOps.Application.Auth.LockoutOptions.SectionName));
builder.Services.Configure<CephasOps.Application.Auth.PasswordResetOptions>(builder.Configuration.GetSection(CephasOps.Application.Auth.PasswordResetOptions.SectionName));
builder.Services.AddScoped<IAuthService, AuthService>();
if (builder.Environment.EnvironmentName == "Testing")
    builder.Services.AddScoped<IUserPermissionProvider, CephasOps.Api.Authorization.TestUserPermissionProvider>();
else
    builder.Services.AddScoped<IUserPermissionProvider, UserPermissionProvider>();
builder.Services.AddScoped<CephasOps.Application.Authorization.IFieldLevelSecurityFilter, CephasOps.Api.Authorization.FieldLevelSecurityFilter>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ICompanyDeploymentService, CompanyDeploymentService>();
builder.Services.AddScoped<CephasOps.Application.Tenants.Services.ITenantService, CephasOps.Application.Tenants.Services.TenantService>();
builder.Services.AddScoped<CephasOps.Application.Provisioning.ICompanyProvisioningService, CephasOps.Application.Provisioning.CompanyProvisioningService>();
builder.Services.AddScoped<CephasOps.Application.Provisioning.ISignupService, CephasOps.Application.Provisioning.SignupService>();
builder.Services.AddScoped<CephasOps.Application.Onboarding.IOnboardingProgressService, CephasOps.Application.Onboarding.OnboardingProgressService>();
builder.Services.AddScoped<CephasOps.Application.Billing.BillingProvider.IBillingProviderService, CephasOps.Application.Billing.BillingProvider.StubBillingProviderService>();
builder.Services.AddScoped<CephasOps.Application.Platform.IPlatformAdminService, CephasOps.Application.Platform.PlatformAdminService>();
builder.Services.AddScoped<CephasOps.Application.Platform.IPlatformAnalyticsService, CephasOps.Application.Platform.PlatformAnalyticsService>();
builder.Services.AddScoped<CephasOps.Application.Insights.IOperationalInsightsService, CephasOps.Application.Insights.OperationalInsightsService>();
builder.Services.Configure<CephasOps.Application.Insights.OperationalIntelligenceOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Insights.OperationalIntelligenceOptions.SectionName));
builder.Services.AddScoped<CephasOps.Application.Insights.IOperationalIntelligenceService, CephasOps.Application.Insights.OperationalIntelligenceService>();
builder.Services.Configure<CephasOps.Application.Insights.OperationalSlaOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Insights.OperationalSlaOptions.SectionName));
builder.Services.AddScoped<CephasOps.Application.Insights.ISlaBreachService, CephasOps.Application.Insights.SlaBreachService>();
builder.Services.Configure<CephasOps.Application.Platform.Guardian.TenantAnomalyDetectionOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Platform.Guardian.TenantAnomalyDetectionOptions.SectionName));
builder.Services.AddScoped<CephasOps.Application.Platform.Guardian.ITenantAnomalyDetectionService, CephasOps.Application.Platform.Guardian.TenantAnomalyDetectionService>();
builder.Services.Configure<CephasOps.Application.Platform.Guardian.PlatformDriftDetectionOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Platform.Guardian.PlatformDriftDetectionOptions.SectionName));
builder.Services.AddScoped<CephasOps.Application.Platform.Guardian.IPlatformDriftDetectionService, CephasOps.Application.Platform.Guardian.PlatformDriftDetectionService>();
builder.Services.AddScoped<CephasOps.Application.Platform.Guardian.IPerformanceWatchdogService, CephasOps.Application.Platform.Guardian.PerformanceWatchdogService>();
builder.Services.AddScoped<CephasOps.Application.Platform.Guardian.IPlatformHealthService, CephasOps.Application.Platform.Guardian.PlatformHealthService>();
builder.Services.AddScoped<CephasOps.Application.Platform.FeatureFlags.IFeatureFlagService, CephasOps.Application.Platform.FeatureFlags.FeatureFlagService>();
builder.Services.AddScoped<CephasOps.Application.Platform.TenantHealth.ITenantHealthScoringService, CephasOps.Application.Platform.TenantHealth.TenantHealthScoringService>();
builder.Services.AddScoped<CephasOps.Application.Audit.ITenantActivityService, CephasOps.Application.Audit.TenantActivityService>();
builder.Services.Configure<CephasOps.Application.Platform.Guardian.PlatformGuardianOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Platform.Guardian.PlatformGuardianOptions.SectionName));
builder.Services.Configure<CephasOps.Api.Options.ProductionRolesOptions>(
    builder.Configuration.GetSection(CephasOps.Api.Options.ProductionRolesOptions.SectionName));
if (builder.Configuration.GetValue("ProductionRoles:RunGuardian", true))
    builder.Services.AddHostedService<CephasOps.Application.Platform.Guardian.PlatformGuardianHostedService>();
builder.Services.AddScoped<CephasOps.Application.Platform.TenantMetricsAggregationJob>();
builder.Services.AddScoped<CephasOps.Application.Billing.Abstractions.IPaymentProvider, CephasOps.Application.Billing.Abstractions.NoOpPaymentProvider>();
builder.Services.AddScoped<CephasOps.Application.Billing.Subscription.Services.IBillingPlanService, CephasOps.Application.Billing.Subscription.Services.BillingPlanService>();
builder.Services.AddScoped<CephasOps.Application.Billing.Subscription.Services.ITenantSubscriptionService, CephasOps.Application.Billing.Subscription.Services.TenantSubscriptionService>();
builder.Services.AddScoped<CephasOps.Application.Billing.Subscription.Services.ISubscriptionEnforcementService, CephasOps.Application.Billing.Subscription.Services.SubscriptionEnforcementService>();
builder.Services.AddScoped<CephasOps.Application.Billing.Usage.ITenantUsageService, CephasOps.Application.Billing.Usage.TenantUsageService>();
builder.Services.AddScoped<CephasOps.Application.Billing.Usage.ITenantUsageQueryService, CephasOps.Application.Billing.Usage.TenantUsageQueryService>();
builder.Services.AddScoped<CephasOps.Application.Subscription.ISubscriptionAccessService, CephasOps.Application.Subscription.SubscriptionAccessService>();
builder.Services.AddScoped<IPartnerService, PartnerService>();
builder.Services.AddScoped<IPartnerGroupService, PartnerGroupService>();
// builder.Services.AddScoped<ICompanyDocumentService, CompanyDocumentService>();
builder.Services.AddScoped<IVerticalService, VerticalService>();
builder.Services.AddScoped<IBuildingService, BuildingService>();
builder.Services.AddScoped<IBuildingMatchingService, BuildingMatchingService>();
builder.Services.AddScoped<IBuildingDefaultMaterialService, BuildingDefaultMaterialService>();
builder.Services.AddScoped<IInfrastructureService, InfrastructureService>();
builder.Services.AddScoped<IBuildingTypeService, BuildingTypeService>();
builder.Services.AddScoped<IInstallationMethodService, InstallationMethodService>();
builder.Services.AddScoped<ISplitterService, SplitterService>();
builder.Services.AddScoped<ISplitterTypeService, SplitterTypeService>();
builder.Services.AddScoped<IServiceInstallerService, ServiceInstallerService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IBlockerValidationService, BlockerValidationService>();
// Encryption Service for sensitive data
builder.Services.AddScoped<CephasOps.Domain.Common.Services.IEncryptionService, CephasOps.Infrastructure.Security.EncryptionService>();

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderTypeService, OrderTypeService>();
builder.Services.AddScoped<IOrderCategoryService, OrderCategoryService>();
builder.Services.AddScoped<IOrderStatusChecklistService, OrderStatusChecklistService>();
// builder.Services.AddScoped<IVerticalService, VerticalService>(); // Removed with company features
builder.Services.AddScoped<ISchedulerService, SchedulerService>();
builder.Services.AddScoped<ITaskService, TaskService>();
// ✅ Using Syncfusion XlsIO for Excel parsing (licensed, superior performance)
builder.Services.AddScoped<ITimeExcelParserService, SyncfusionExcelParserService>();
builder.Services.AddScoped<IParsedOrderDraftEnrichmentService, ParsedOrderDraftEnrichmentService>();

// ✅ Resilient Excel to PDF conversion with automatic fallback
// Register converters
builder.Services.AddScoped<CephasOps.Application.Parser.Services.Converters.SyncfusionExcelToPdfConverter>();
builder.Services.AddScoped<CephasOps.Application.Parser.Services.Converters.ExcelDataReaderToPdfConverter>();
// Register Excel format converter (.xls to .xlsx)
builder.Services.AddScoped<CephasOps.Application.Parser.Services.Converters.ExcelFormatConverter>();
// Register resilient service (tries Syncfusion first, falls back to ExcelDataReader for corrupted files)
builder.Services.AddScoped<IExcelToPdfService, ResilientExcelToPdfService>();
builder.Services.AddScoped<IPdfTextExtractionService, PdfTextExtractionService>();
builder.Services.AddScoped<IPdfOrderParserService, PdfOrderParserService>();
builder.Services.AddScoped<IParserService, ParserService>();
builder.Services.AddScoped<IEmailRuleService, EmailRuleService>();
builder.Services.AddScoped<IVipEmailService, VipEmailService>();
builder.Services.AddScoped<IVipGroupService, VipGroupService>();
builder.Services.AddScoped<IParserTemplateService, ParserTemplateService>();
builder.Services.AddScoped<ITemplateProfileService, TemplateProfileService>();
builder.Services.AddScoped<IDriftBaselineProvider, DriftBaselineProvider>();
builder.Services.AddScoped<IParserReplayService, ParserReplayService>();
builder.Services.AddScoped<IParsedMaterialAliasService, ParsedMaterialAliasService>();
builder.Services.AddScoped<IDriftReportService, DriftReportService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IEmailSendingService, EmailSendingService>();
builder.Services.AddScoped<IEmailIngestionService, EmailIngestionService>();
// Break ParserService <-> EmailIngestionService circular dependency: resolve IEmailIngestionService only when used
builder.Services.AddTransient(sp => new Lazy<IEmailIngestionService>(() => sp.GetRequiredService<IEmailIngestionService>()));
builder.Services.AddScoped<IAgentModeService, AgentModeService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IStockLedgerService, StockLedgerService>();
builder.Services.AddScoped<IMaterialCategoryService, MaterialCategoryService>();
builder.Services.AddScoped<IMovementValidationService, MovementValidationService>();
builder.Services.AddScoped<ILocationAutoCreateService, LocationAutoCreateService>();
builder.Services.AddScoped<IRMAService, RMAService>();
builder.Services.AddScoped<IBillingService, BillingService>();
builder.Services.AddScoped<CephasOps.Application.SaaS.IFeatureFlagService, CephasOps.Application.SaaS.PlanBasedFeatureFlagService>();
builder.Services.AddScoped<IBillingRatecardService, BillingRatecardService>();
builder.Services.AddScoped<IInvoiceSubmissionService, InvoiceSubmissionService>();
builder.Services.AddScoped<EInvoiceProviderFactory>();
builder.Services.AddScoped<ICurrencyExchangeService, CurrencyExchangeService>();
builder.Services.AddScoped<IPayrollService, PayrollService>();

// Rate Engine Service
builder.Services.AddScoped<IRateEngineService, RateEngineService>();
builder.Services.AddScoped<IRateGroupService, RateGroupService>();
builder.Services.AddScoped<IBaseWorkRateService, BaseWorkRateService>();
builder.Services.AddScoped<IServiceProfileService, ServiceProfileService>();
builder.Services.AddScoped<IOrderCategoryServiceProfileService, OrderCategoryServiceProfileService>();

// CSV Import/Export Service
builder.Services.AddSingleton<ICsvService, CsvService>();
builder.Services.AddSingleton<CephasOps.Domain.PlatformSafety.IGuardViolationBuffer, CephasOps.Infrastructure.PlatformSafety.GuardViolationBuffer>();
// Report export formats (Excel, PDF) for Reports Hub
builder.Services.AddScoped<IReportExportFormatService, ReportExportFormatService>();
builder.Services.AddScoped<IPnlService, PnlService>();
builder.Services.AddScoped<IOrderProfitabilityService, OrderProfitabilityService>();
builder.Services.AddScoped<IOrderPayoutSnapshotService, OrderPayoutSnapshotService>();
builder.Services.AddScoped<CephasOps.Application.Rates.Services.IPayoutHealthDashboardService, CephasOps.Application.Rates.Services.PayoutHealthDashboardService>();
builder.Services.AddScoped<CephasOps.Application.Rates.Services.IMissingPayoutSnapshotRepairService, CephasOps.Application.Rates.Services.MissingPayoutSnapshotRepairService>();
builder.Services.AddScoped<CephasOps.Application.Rates.Services.IPayoutAnomalyService, CephasOps.Application.Rates.Services.PayoutAnomalyService>();
builder.Services.AddScoped<CephasOps.Application.Rates.Services.IPayoutAnomalyReviewService, CephasOps.Application.Rates.Services.PayoutAnomalyReviewService>();
builder.Services.AddScoped<CephasOps.Application.Rates.Services.IPayoutAnomalyAlertSender, CephasOps.Application.Rates.Services.EmailPayoutAnomalyAlertSender>();
builder.Services.AddScoped<CephasOps.Application.Rates.Services.IPayoutAnomalyAlertService, CephasOps.Application.Rates.Services.PayoutAnomalyAlertService>();
builder.Services.AddScoped<CephasOps.Application.Rates.Services.IAlertRunHistoryService, CephasOps.Application.Rates.Services.AlertRunHistoryService>();
builder.Services.AddScoped<CephasOps.Application.Rates.Services.IPayoutAnomalyResponseTrackingService, CephasOps.Application.Rates.Services.PayoutAnomalyResponseTrackingService>();
builder.Services.AddScoped<IOrderFinancialAlertNotifier, NoOpOrderFinancialAlertNotifier>();
builder.Services.AddScoped<IOrderProfitAlertService, OrderProfitAlertService>();
builder.Services.AddScoped<IPnlTypeService, PnlTypeService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IDepartmentDeploymentService, DepartmentDeploymentService>();
builder.Services.AddScoped<IDepartmentAccessService, DepartmentAccessService>();

// Asset Management Services
builder.Services.AddScoped<IAssetTypeService, AssetTypeService>();
builder.Services.AddScoped<IAssetService, AssetService>();
builder.Services.AddScoped<IDepreciationService, DepreciationService>();

// Accounting Services
builder.Services.AddScoped<ISupplierInvoiceService, SupplierInvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Settings Services (Phase 5)
builder.Services.AddScoped<IGlobalSettingsService, GlobalSettingsService>();
// Register IGlobalSettingsReader as the same instance (IGlobalSettingsService extends IGlobalSettingsReader)
builder.Services.AddScoped<CephasOps.Domain.Settings.IGlobalSettingsReader>(sp => sp.GetRequiredService<IGlobalSettingsService>());
builder.Services.AddScoped<IMaterialTemplateService, MaterialTemplateService>();
builder.Services.AddScoped<IDocumentTemplateService, DocumentTemplateService>();
builder.Services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();
builder.Services.AddScoped<ISyncfusionBoqGenerator, SyncfusionBoqGenerator>();
builder.Services.AddScoped<ISyncfusionInvoiceGenerator, SyncfusionInvoiceGenerator>();
builder.Services.AddScoped<IKpiProfileService, KpiProfileService>();
builder.Services.AddScoped<ISlaProfileService, SlaProfileService>();
builder.Services.AddScoped<IAutomationRuleService, AutomationRuleService>();
builder.Services.AddScoped<IApprovalWorkflowService, ApprovalWorkflowService>();
builder.Services.AddScoped<IBusinessHoursService, BusinessHoursService>();
builder.Services.AddScoped<IPaymentTermService, PaymentTermService>();
// Reference-data CRUD services (controllers inject these; parallel to PaymentTerms / GenericSettingsService<Brand> pattern)
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<ITeamService, TeamService>();
builder.Services.AddScoped<IServicePlanService, ServicePlanService>();
builder.Services.AddScoped<IProductTypeService, ProductTypeService>();
builder.Services.AddScoped<IReportDefinitionService, ReportDefinitionService>();
builder.Services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
builder.Services.AddScoped<ITaxCodeService, TaxCodeService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IEscalationRuleService, EscalationRuleService>();
builder.Services.AddScoped<IEmailAccountService, EmailAccountService>();
builder.Services.AddScoped<ITimeSlotService, TimeSlotService>();
builder.Services.AddScoped<ISmsTemplateService, SmsTemplateService>();
builder.Services.AddScoped<IWhatsAppTemplateService, WhatsAppTemplateService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();

// SMS Gateway Service
builder.Services.AddScoped<CephasOps.Application.Settings.Services.ISmsGatewayService, CephasOps.Application.Settings.Services.SmsGatewayService>();

// SMS/WhatsApp Notification Providers
builder.Services.AddScoped<CephasOps.Infrastructure.Services.External.TwilioSmsProvider>();
builder.Services.AddScoped<CephasOps.Infrastructure.Services.External.TwilioWhatsAppProvider>();
builder.Services.AddScoped<CephasOps.Infrastructure.Services.External.NullSmsProvider>();
builder.Services.AddScoped<CephasOps.Infrastructure.Services.External.NullWhatsAppProvider>();
builder.Services.AddHttpClient<CephasOps.Infrastructure.Services.External.SmsGatewaySender>();
// Changed to Scoped to match IGlobalSettingsService lifetime
builder.Services.AddScoped<CephasOps.Application.Notifications.Services.SmsProviderFactory>();
builder.Services.AddScoped<CephasOps.Application.Notifications.Services.WhatsAppProviderFactory>();

// SMS and WhatsApp messaging services
builder.Services.AddScoped<CephasOps.Application.Notifications.Services.ISmsMessagingService, CephasOps.Application.Notifications.Services.SmsMessagingService>();
builder.Services.AddScoped<CephasOps.Application.Notifications.Services.IWhatsAppMessagingService, CephasOps.Application.Notifications.Services.WhatsAppMessagingService>();

// Note: ISmsProvider and IWhatsAppProvider are resolved via factories at runtime
// No direct registration needed - CustomerNotificationService uses factories

// SMS and WhatsApp messaging services
builder.Services.AddScoped<CephasOps.Application.Notifications.Services.ISmsMessagingService, CephasOps.Application.Notifications.Services.SmsMessagingService>();

// Integration Settings Service
builder.Services.AddScoped<CephasOps.Application.Settings.Services.IIntegrationSettingsService, CephasOps.Application.Settings.Services.IntegrationSettingsService>();

// Notification Services
builder.Services.AddScoped<CephasOps.Application.Notifications.Services.CustomerNotificationService>();
builder.Services.AddScoped<CephasOps.Application.Notifications.Handlers.OrderStatusChangedNotificationHandler>();
// Phase 2 Notifications extraction: dispatch store, request service, delivery sender, event handler, worker
builder.Services.AddScoped<CephasOps.Domain.Notifications.INotificationDispatchStore, CephasOps.Infrastructure.Persistence.NotificationDispatchStore>();
builder.Services.AddScoped<CephasOps.Application.Notifications.INotificationDispatchRequestService, CephasOps.Application.Notifications.Services.NotificationDispatchRequestService>();
builder.Services.AddScoped<CephasOps.Application.Notifications.INotificationDeliverySender, CephasOps.Application.Notifications.Services.NotificationDeliverySender>();
// Phase 6: default email account for notification email dispatch
builder.Services.AddScoped<CephasOps.Application.Notifications.IDefaultEmailAccountIdProvider, CephasOps.Application.Notifications.Services.DefaultEmailAccountIdProvider>();
builder.Services.Configure<CephasOps.Application.Notifications.NotificationDispatchWorkerOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Notifications.NotificationDispatchWorkerOptions.SectionName));
if (builder.Configuration.GetValue("ProductionRoles:RunNotificationWorkers", true))
    builder.Services.AddHostedService<CephasOps.Application.Notifications.NotificationDispatchWorkerHostedService>();

// Phase 7 Notification retention (replaces legacy notificationretention BackgroundJob)
builder.Services.AddScoped<CephasOps.Application.Notifications.INotificationRetentionService, CephasOps.Application.Notifications.Services.NotificationRetentionService>();
builder.Services.Configure<CephasOps.Application.Notifications.NotificationRetentionOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Notifications.NotificationRetentionOptions.SectionName));
if (builder.Configuration.GetValue("ProductionRoles:RunNotificationWorkers", true))
    builder.Services.AddHostedService<CephasOps.Application.Notifications.NotificationRetentionHostedService>();

// Phase 3/4 Job Orchestration: JobExecution store, enqueuer, executors, worker, query
builder.Services.AddScoped<CephasOps.Domain.Workflow.IJobExecutionStore, CephasOps.Infrastructure.Persistence.JobExecutionStore>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutionEnqueuer, CephasOps.Application.Workflow.JobOrchestration.JobExecutionEnqueuer>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutor, CephasOps.Application.Workflow.JobOrchestration.Executors.PnlRebuildJobExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutor, CephasOps.Application.Workflow.JobOrchestration.Executors.ReconcileLedgerBalanceCacheJobExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutor, CephasOps.Application.Workflow.JobOrchestration.Executors.SlaEvaluationJobExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutor, CephasOps.Application.Workflow.JobOrchestration.Executors.PopulateStockByLocationSnapshotsJobExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutor, CephasOps.Application.Workflow.JobOrchestration.Executors.DocumentGenerationJobExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutor, CephasOps.Application.Workflow.JobOrchestration.Executors.EmailIngestJobExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutor, CephasOps.Application.Workflow.JobOrchestration.Executors.MyInvoisStatusPollJobExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutor, CephasOps.Application.Workflow.JobOrchestration.Executors.InventoryReportExportJobExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutor, CephasOps.Application.Workflow.JobOrchestration.Executors.EventHandlingAsyncJobExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutor, CephasOps.Application.Workflow.JobOrchestration.Executors.OperationalReplayJobExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutor, CephasOps.Application.Workflow.JobOrchestration.Executors.OperationalRebuildJobExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutorRegistry, CephasOps.Application.Workflow.JobOrchestration.JobExecutorRegistry>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IDocumentGenerationJobEnqueuer, CephasOps.Application.Workflow.JobOrchestration.DocumentGenerationJobEnqueuer>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobOrchestration.IJobExecutionQueryService, CephasOps.Application.Workflow.JobOrchestration.JobExecutionQueryService>();
builder.Services.Configure<CephasOps.Application.Workflow.JobOrchestration.JobExecutionWorkerOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Workflow.JobOrchestration.JobExecutionWorkerOptions.SectionName));
if (builder.Configuration.GetValue("ProductionRoles:RunJobWorkers", true))
{
    builder.Services.AddHostedService<CephasOps.Application.Workflow.JobOrchestration.JobExecutionWorkerHostedService>();
    if (builder.Configuration.GetValue("ProductionRoles:RunWatchdog", true))
        builder.Services.AddHostedService<CephasOps.Application.Workflow.JobOrchestration.JobExecutionWatchdogService>();
}
builder.Services.Configure<CephasOps.Application.Files.StorageLifecycleOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Files.StorageLifecycleOptions.SectionName));
if (builder.Configuration.GetValue("ProductionRoles:RunStorageLifecycle", true))
    builder.Services.AddHostedService<CephasOps.Application.Files.StorageLifecycleService>();

// WhatsApp Cloud API Provider and Messaging Service
builder.Services.AddHttpClient<CephasOps.Infrastructure.Services.External.WhatsAppCloudApiProvider>();
builder.Services.AddScoped<CephasOps.Application.Notifications.Services.IWhatsAppMessagingService, CephasOps.Application.Notifications.Services.WhatsAppMessagingService>();

// Unified Messaging Service (SMS + WhatsApp routing)
builder.Services.AddScoped<CephasOps.Application.Notifications.Services.IUnifiedMessagingService, CephasOps.Application.Notifications.Services.UnifiedMessagingService>();

// E-Invoice Provider (MyInvois)
builder.Services.AddHttpClient("MyInvois", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<CephasOps.Domain.Billing.IEInvoiceProvider, CephasOps.Infrastructure.Services.External.NullEInvoiceProvider>();
builder.Services.AddScoped<CephasOps.Infrastructure.Services.External.MyInvoisApiProvider>(); // Default to Null, can be switched to MyInvoisApiProvider when configured
// Settings Services (using existing implementations)
builder.Services.AddScoped<IBinService, BinService>();
builder.Services.AddScoped(sp => new GenericSettingsService<CephasOps.Domain.Settings.Entities.Bin>(sp.GetRequiredService<ApplicationDbContext>(), sp.GetRequiredService<ILogger<GenericSettingsService<CephasOps.Domain.Settings.Entities.Bin>>>()));
builder.Services.AddScoped(sp => new GenericSettingsService<CephasOps.Domain.Settings.Entities.Brand>(sp.GetRequiredService<ApplicationDbContext>(), sp.GetRequiredService<ILogger<GenericSettingsService<CephasOps.Domain.Settings.Entities.Brand>>>()));
builder.Services.AddScoped(sp => new GenericSettingsService<CephasOps.Domain.Settings.Entities.ServicePlan>(sp.GetRequiredService<ApplicationDbContext>(), sp.GetRequiredService<ILogger<GenericSettingsService<CephasOps.Domain.Settings.Entities.ServicePlan>>>()));
builder.Services.AddScoped(sp => new GenericSettingsService<CephasOps.Domain.Settings.Entities.ProductType>(sp.GetRequiredService<ApplicationDbContext>(), sp.GetRequiredService<ILogger<GenericSettingsService<CephasOps.Domain.Settings.Entities.ProductType>>>()));
builder.Services.AddScoped(sp => new GenericSettingsService<CephasOps.Domain.Settings.Entities.Team>(sp.GetRequiredService<ApplicationDbContext>(), sp.GetRequiredService<ILogger<GenericSettingsService<CephasOps.Domain.Settings.Entities.Team>>>()));
builder.Services.AddScoped(sp => new GenericSettingsService<CephasOps.Domain.Companies.Entities.CostCentre>(sp.GetRequiredService<ApplicationDbContext>(), sp.GetRequiredService<ILogger<GenericSettingsService<CephasOps.Domain.Companies.Entities.CostCentre>>>()));

// Carbone Document Engine (optional - disabled by default)
builder.Services.Configure<CarboneSettings>(builder.Configuration.GetSection(CarboneSettings.SectionName));
builder.Services.Configure<CephasOps.Application.Parser.Settings.MailSettings>(
    builder.Configuration.GetSection(CephasOps.Application.Parser.Settings.MailSettings.SectionName));
builder.Services.Configure<CephasOps.Application.Pnl.ProfitabilityAlertsOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Pnl.ProfitabilityAlertsOptions.SectionName));
builder.Services.AddHttpClient<ICarboneRenderer, CarboneRenderer>();

// Workflow Engine Services (Phase 6)
builder.Services.AddScoped<IWorkflowDefinitionsService, WorkflowDefinitionsService>();

// Register Guard Condition Validators (settings-driven, no hardcoding)
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.IGuardConditionValidator, CephasOps.Application.Workflow.Validators.PhotosRequiredValidator>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.IGuardConditionValidator, CephasOps.Application.Workflow.Validators.DocketUploadedValidator>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.IGuardConditionValidator, CephasOps.Application.Workflow.Validators.SplitterAssignedValidator>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.IGuardConditionValidator, CephasOps.Application.Workflow.Validators.SerialsValidatedValidator>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.IGuardConditionValidator, CephasOps.Application.Workflow.Validators.MaterialsSpecifiedValidator>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.IGuardConditionValidator, CephasOps.Application.Workflow.Validators.SiAssignedValidator>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.IGuardConditionValidator, CephasOps.Application.Workflow.Validators.AppointmentDateSetValidator>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.IGuardConditionValidator, CephasOps.Application.Workflow.Validators.BuildingSelectedValidator>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.IGuardConditionValidator, CephasOps.Application.Workflow.Validators.CustomerContactProvidedValidator>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.IGuardConditionValidator, CephasOps.Application.Workflow.Validators.NoActiveBlockersValidator>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.IGuardConditionValidator, CephasOps.Application.Workflow.Validators.ChecklistCompletedValidator>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.IGuardConditionValidator, CephasOps.Application.Workflow.Validators.AssuranceReplacementValidator>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.IGuardConditionValidator, CephasOps.Application.Workflow.Validators.NoSchedulingConflictsValidator>();

// Register Material Collection Service
builder.Services.AddScoped<CephasOps.Application.Orders.Services.MaterialCollectionService>();
builder.Services.AddScoped<CephasOps.Application.Orders.Services.IMaterialPackProvider>(sp => sp.GetRequiredService<CephasOps.Application.Orders.Services.MaterialCollectionService>());
builder.Services.AddScoped<CephasOps.Application.Orders.Services.OrderMaterialUsageService>();

// Register SI App Services
builder.Services.AddScoped<CephasOps.Application.SIApp.Services.SiAppMaterialService>();

// Register Side Effect Executors (settings-driven, no hardcoding)
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.ISideEffectExecutor, CephasOps.Application.Workflow.Executors.NotifySideEffectExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.ISideEffectExecutor, CephasOps.Application.Workflow.Executors.CreateStockMovementSideEffectExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.ISideEffectExecutor, CephasOps.Application.Workflow.Executors.CreateOrderStatusLogSideEffectExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.ISideEffectExecutor, CephasOps.Application.Workflow.Executors.UpdateOrderFlagsSideEffectExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.ISideEffectExecutor, CephasOps.Application.Workflow.Executors.TriggerInvoiceEligibilitySideEffectExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.ISideEffectExecutor, CephasOps.Application.Workflow.Executors.CheckMaterialCollectionSideEffectExecutor>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Interfaces.ISideEffectExecutor, CephasOps.Application.Workflow.Executors.CreateInstallerTaskSideEffectExecutor>();

// Register Registries (must be after validators/executors)
builder.Services.AddScoped<CephasOps.Application.Workflow.Services.GuardConditionValidatorRegistry>();
builder.Services.AddScoped<CephasOps.Application.Workflow.Services.SideEffectExecutorRegistry>();

// Shared order scope resolver (PartnerId, DepartmentId, OrderTypeCode from Order)
builder.Services.AddScoped<IEffectiveScopeResolver, EffectiveScopeResolver>();

// Shared order pricing context resolver (full pricing-driving fields from Order)
builder.Services.AddScoped<IOrderPricingContextResolver, OrderPricingContextResolver>();

// Event Bus (Phase 1–8: Correlation, emission, durable outbox, dispatcher worker, platform envelope, partitioning)
builder.Services.Configure<CephasOps.Application.Events.EventBusDispatcherOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Events.EventBusDispatcherOptions.SectionName));
builder.Services.Configure<CephasOps.Application.Events.PlatformEventEnvelopeOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Events.PlatformEventEnvelopeOptions.SectionName));
builder.Services.Configure<CephasOps.Application.Events.Backpressure.BackpressureOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Events.Backpressure.BackpressureOptions.SectionName));
builder.Services.AddSingleton<CephasOps.Application.Events.Partitioning.IPartitionKeyResolver, CephasOps.Application.Events.Partitioning.DefaultPartitionKeyResolver>();
builder.Services.AddScoped<CephasOps.Application.Events.IPlatformEventEnvelopeBuilder, CephasOps.Application.Events.PlatformEventEnvelopeBuilder>();
builder.Services.AddSingleton<CephasOps.Application.Events.Backpressure.IEventBusBackpressureService, CephasOps.Application.Events.Backpressure.EventBusBackpressureService>();
builder.Services.AddScoped<CephasOps.Application.Common.Interfaces.ICorrelationIdProvider, CephasOps.Api.Services.CorrelationIdProvider>();
builder.Services.AddScoped<CephasOps.Domain.Events.IEventStore, CephasOps.Infrastructure.Persistence.EventStoreRepository>();
builder.Services.AddScoped<CephasOps.Application.Events.IJobRunRecorderForEvents, CephasOps.Application.Events.JobRunRecorderForEvents>();
builder.Services.AddSingleton<CephasOps.Application.Events.EventBusMetricsSnapshot>();
builder.Services.AddSingleton<CephasOps.Application.Events.EventBusDispatcherMetrics>();
builder.Services.AddSingleton<CephasOps.Application.Events.EventStoreDispatcherState>();
builder.Services.AddSingleton<CephasOps.Application.Events.IEventStoreDispatcherState>(sp => sp.GetRequiredService<CephasOps.Application.Events.EventStoreDispatcherState>());
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventDispatcher, CephasOps.Application.Events.DomainEventDispatcher>();
builder.Services.AddScoped<CephasOps.Application.Events.IEventBus, CephasOps.Application.Events.EventBus>();
builder.Services.AddScoped<CephasOps.Application.Events.IEventProcessingLogStore, CephasOps.Application.Events.EventProcessingLogStore>();
builder.Services.AddScoped<CephasOps.Application.Events.IAsyncEventEnqueuer, CephasOps.Application.Events.AsyncEventEnqueuer>();
if (builder.Configuration.GetValue("ProductionRoles:RunEventDispatcher", true))
{
    builder.Services.AddHostedService<CephasOps.Application.Events.EventStoreDispatcherHostedService>();
    builder.Services.AddHostedService<CephasOps.Application.Events.EventBusMetricsCollectorHostedService>();
}
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.WorkflowTransitionCompletedEvent>, CephasOps.Application.Events.WorkflowTransitionCompletedEventHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.WorkflowTransitionCompletedEvent>, CephasOps.Application.Events.WorkflowTransitionHistoryProjectionHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.WorkflowTransitionCompletedEvent>, CephasOps.Application.Events.Ledger.WorkflowTransitionLedgerHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.OrderStatusChangedEvent>, CephasOps.Application.Events.Ledger.OrderLifecycleLedgerHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.OrderStatusChangedEvent>, CephasOps.Application.Notifications.Handlers.OrderStatusNotificationDispatchHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.OrderAssignedEvent>, CephasOps.Application.Events.OrderAssignedOperationsHandler>();
// Tenant activity timeline: record selected domain events to tenant activity timeline (tenant-scoped only)
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.OrderCreatedEvent>, CephasOps.Application.Audit.TenantActivityTimelineFromEventsHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.OrderCompletedEvent>, CephasOps.Application.Audit.TenantActivityTimelineFromEventsHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.OrderAssignedEvent>, CephasOps.Application.Audit.TenantActivityTimelineFromEventsHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.OrderStatusChangedEvent>, CephasOps.Application.Audit.TenantActivityTimelineFromEventsHandler>();
// Event Platform: forward selected domain events to outbound integration bus
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.WorkflowTransitionCompletedEvent>, CephasOps.Application.Integration.IntegrationEventForwardingHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.OrderStatusChangedEvent>, CephasOps.Application.Integration.IntegrationEventForwardingHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.OrderAssignedEvent>, CephasOps.Application.Integration.IntegrationEventForwardingHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.OrderCreatedEvent>, CephasOps.Application.Integration.IntegrationEventForwardingHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.OrderCompletedEvent>, CephasOps.Application.Integration.IntegrationEventForwardingHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.OrderCompletedEvent>, CephasOps.Application.Automation.Handlers.OrderCompletedAutomationHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.OrderCompletedEvent>, CephasOps.Application.Insights.Handlers.OrderCompletedInsightHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.InvoiceGeneratedEvent>, CephasOps.Application.Integration.IntegrationEventForwardingHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.MaterialIssuedEvent>, CephasOps.Application.Integration.IntegrationEventForwardingHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.MaterialReturnedEvent>, CephasOps.Application.Integration.IntegrationEventForwardingHandler>();
builder.Services.AddScoped<CephasOps.Application.Events.IDomainEventHandler<CephasOps.Application.Events.PayrollCalculatedEvent>, CephasOps.Application.Integration.IntegrationEventForwardingHandler>();
builder.Services.Configure<CephasOps.Application.Events.Ledger.LedgerOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Events.Ledger.LedgerOptions.SectionName));
builder.Services.AddSingleton<CephasOps.Application.Events.Ledger.ILedgerPayloadValidator, CephasOps.Application.Events.Ledger.LedgerPayloadValidator>();
builder.Services.AddScoped<CephasOps.Application.Events.Ledger.ILedgerWriter, CephasOps.Application.Events.Ledger.LedgerWriter>();
builder.Services.AddScoped<CephasOps.Application.Events.Ledger.ILedgerQueryService, CephasOps.Application.Events.Ledger.LedgerQueryService>();
builder.Services.AddSingleton<CephasOps.Application.Events.Ledger.ILedgerFamilyRegistry, CephasOps.Application.Events.Ledger.LedgerFamilyRegistry>();
builder.Services.AddScoped<CephasOps.Application.Events.Ledger.IWorkflowTransitionTimelineFromLedger, CephasOps.Application.Events.Ledger.WorkflowTransitionTimelineFromLedger>();
builder.Services.AddScoped<CephasOps.Application.Events.Ledger.IOrderTimelineFromLedger, CephasOps.Application.Events.Ledger.OrderTimelineFromLedger>();
builder.Services.AddScoped<CephasOps.Application.Events.Ledger.IUnifiedOrderHistoryFromLedger, CephasOps.Application.Events.Ledger.UnifiedOrderHistoryFromLedger>();
builder.Services.AddScoped<CephasOps.Application.Events.IEventStoreQueryService, CephasOps.Application.Events.EventStoreQueryService>();
builder.Services.AddScoped<CephasOps.Application.Events.IEventBusObservabilityService, CephasOps.Application.Events.EventBusObservabilityService>();
builder.Services.AddSingleton<CephasOps.Application.Events.Replay.IEventReplayPolicy, CephasOps.Application.Events.Replay.EventReplayPolicy>();
builder.Services.AddSingleton<CephasOps.Application.Events.Replay.IOperationalReplayPolicy, CephasOps.Application.Events.Replay.OperationalReplayPolicy>();
builder.Services.AddSingleton<CephasOps.Application.Events.Replay.IEventTypeRegistry, CephasOps.Application.Events.Replay.EventTypeRegistry>();
builder.Services.AddSingleton<CephasOps.Application.Events.Replay.IReplayExecutionContextAccessor, CephasOps.Application.Events.Replay.ReplayExecutionContextAccessor>();
builder.Services.AddScoped<CephasOps.Application.Events.Replay.IEventReplayService, CephasOps.Application.Events.Replay.EventReplayService>();
builder.Services.AddScoped<CephasOps.Application.Events.Replay.IEventBulkReplayService, CephasOps.Application.Events.Replay.EventBulkReplayService>();
builder.Services.AddSingleton<CephasOps.Application.Events.IFailureClassifier, CephasOps.Application.Events.FailureClassifier>();
builder.Services.AddScoped<CephasOps.Domain.Events.IEventStoreAttemptHistoryStore, CephasOps.Infrastructure.Persistence.EventStoreAttemptHistoryStore>();
builder.Services.AddSingleton<CephasOps.Application.Events.Replay.ReplayMetrics>();
        builder.Services.AddSingleton<CephasOps.Application.Events.Replay.IReplayTargetRegistry, CephasOps.Application.Events.Replay.ReplayTargetRegistry>();
        builder.Services.AddScoped<CephasOps.Application.Events.Replay.IReplayPreviewService, CephasOps.Application.Events.Replay.ReplayPreviewService>();
        builder.Services.AddScoped<CephasOps.Application.Events.Replay.IReplayExecutionLockStore, CephasOps.Application.Events.Replay.ReplayExecutionLockStore>();
        builder.Services.AddScoped<CephasOps.Application.Events.Replay.IOperationalReplayExecutionService, CephasOps.Application.Events.Replay.OperationalReplayExecutionService>();
        builder.Services.AddScoped<CephasOps.Application.Events.Replay.IReplayJobEnqueuer, CephasOps.Application.Events.Replay.ReplayJobEnqueuer>();
        builder.Services.AddScoped<CephasOps.Application.Events.Replay.IReplayOperationQueryService, CephasOps.Application.Events.Replay.ReplayOperationQueryService>();
builder.Services.AddScoped<CephasOps.Application.Events.Lineage.IEventLineageService, CephasOps.Application.Events.Lineage.EventLineageService>();

// Operational State Rebuilder (Phase 1 + Phase 2)
builder.Services.AddSingleton<CephasOps.Application.Rebuild.IRebuildTargetRegistry, CephasOps.Application.Rebuild.RebuildTargetRegistry>();
builder.Services.AddScoped<CephasOps.Application.Rebuild.IRebuildRunner, CephasOps.Application.Rebuild.WorkflowTransitionHistoryFromEventStoreRebuildRunner>();
builder.Services.AddScoped<CephasOps.Application.Rebuild.IRebuildRunner, CephasOps.Application.Rebuild.WorkflowTransitionHistoryFromLedgerRebuildRunner>();
builder.Services.AddScoped<CephasOps.Application.Rebuild.IRebuildExecutionLockStore, CephasOps.Application.Rebuild.RebuildExecutionLockStore>();
builder.Services.AddScoped<CephasOps.Application.Rebuild.IRebuildJobEnqueuer, CephasOps.Application.Rebuild.RebuildJobEnqueuer>();
builder.Services.AddScoped<CephasOps.Application.Rebuild.IOperationalRebuildService, CephasOps.Application.Rebuild.OperationalRebuildService>();

// Health checks (Event Bus, Database, Redis, Guardian, Job Backlog - Phase 13 + launch readiness)
var healthChecksBuilder = builder.Services.AddHealthChecks()
    .AddCheck<CephasOps.Api.Health.DatabaseHealthCheck>("database", tags: new[] { "ready", "database", "platform", "startup" })
    .AddCheck<CephasOps.Api.Health.EventBusHealthCheck>("eventbus", tags: new[] { "ready", "eventbus", "platform" })
    .AddCheck<CephasOps.Api.Health.GuardianHealthCheck>("guardian", tags: new[] { "platform" })
    .AddCheck<CephasOps.Api.Health.JobBacklogHealthCheck>("jobbacklog", tags: new[] { "platform" });
if (!string.IsNullOrWhiteSpace(redisConnection))
    healthChecksBuilder.AddCheck<CephasOps.Api.Health.RedisHealthCheck>("redis", tags: new[] { "ready", "platform", "cache", "startup" });
builder.Services.Configure<CephasOps.Api.Health.JobBacklogHealthCheckOptions>(
    builder.Configuration.GetSection(CephasOps.Api.Health.JobBacklogHealthCheckOptions.SectionName));

// Trace Explorer (operational trace timeline)
builder.Services.AddScoped<CephasOps.Application.Trace.ITraceQueryService, CephasOps.Application.Trace.TraceQueryService>();
builder.Services.AddScoped<CephasOps.Application.Sla.ISlaEvaluationService, CephasOps.Application.Sla.SlaEvaluationService>();
builder.Services.Configure<CephasOps.Application.Sla.SlaAlertOptions>(builder.Configuration.GetSection(CephasOps.Application.Sla.SlaAlertOptions.SectionName));
builder.Services.AddHttpClient<CephasOps.Application.Sla.ISlaAlertSender, CephasOps.Application.Sla.SlaAlertSender>();

// Register Workflow Engine Service (depends on registries and scope/pricing context resolvers)
builder.Services.AddScoped<IWorkflowEngineService, WorkflowEngineService>();

// Phase 9: Command Bus and Workflow Orchestrator
builder.Services.AddScoped<ICommandBus, CommandBus>();
builder.Services.AddScoped<ICommandProcessingLogStore, CommandProcessingLogStore>();
builder.Services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));
builder.Services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(ICommandPipelineBehavior<,>), typeof(RetryBehavior<,>));
builder.Services.AddScoped<ICommandHandler<ExecuteWorkflowTransitionCommand, CephasOps.Application.Workflow.DTOs.WorkflowJobDto>, ExecuteWorkflowTransitionHandler>();
builder.Services.AddScoped<IWorkflowOrchestrator, WorkflowOrchestratorService>();
builder.Services.AddScoped<ICommandDiagnosticsQueryService, CommandDiagnosticsQueryService>();

// Phase 10: External Integration Bus and Webhook Runtime
builder.Services.AddScoped<IConnectorRegistry, ConnectorRegistry>();
builder.Services.AddScoped<IOutboundDeliveryStore, OutboundDeliveryStore>();
builder.Services.AddScoped<IInboundWebhookReceiptStore, InboundWebhookReceiptStore>();
builder.Services.AddScoped<IExternalIdempotencyStore, ExternalIdempotencyStore>();
builder.Services.AddScoped<IOutboundIntegrationBus, OutboundIntegrationBus>();
builder.Services.AddScoped<IDomainEventToPlatformEnvelopeBuilder, DomainEventToPlatformEnvelopeBuilder>();
builder.Services.AddScoped<IInboundWebhookRuntime, InboundWebhookRuntime>();
builder.Services.AddScoped<IInboundReceiptReplayService, InboundReceiptReplayService>();
builder.Services.AddSingleton<IOutboundHttpDispatcher, OutboundHttpDispatcher>();
builder.Services.AddHttpClient("IntegrationOutbound", _ => { });
builder.Services.AddSingleton<IIntegrationPayloadMapper, DefaultIntegrationPayloadMapper>();
builder.Services.AddSingleton<IOutboundSigner, NoOpOutboundSigner>();
builder.Services.AddSingleton<IInboundWebhookVerifier, NoOpInboundWebhookVerifier>();
builder.Services.AddSingleton<IInboundWebhookHandler, NoOpInboundWebhookHandler>();
builder.Services.Configure<OutboundIntegrationRetryWorkerOptions>(builder.Configuration.GetSection(OutboundIntegrationRetryWorkerOptions.SectionName));
if (builder.Configuration.GetValue("ProductionRoles:RunIntegrationWorkers", true))
    builder.Services.AddHostedService<OutboundIntegrationRetryWorkerHostedService>();
builder.Services.Configure<EventPlatformRetentionOptions>(builder.Configuration.GetSection(EventPlatformRetentionOptions.SectionName));
builder.Services.AddScoped<IEventPlatformRetentionService, EventPlatformRetentionService>();
if (builder.Configuration.GetValue("ProductionRoles:RunIntegrationWorkers", true))
    builder.Services.AddHostedService<EventPlatformRetentionWorkerHostedService>();

// Register Guard Condition and Side Effect Definition Services
builder.Services.AddScoped<IGuardConditionDefinitionsService, GuardConditionDefinitionsService>();
builder.Services.AddScoped<ISideEffectDefinitionsService, SideEffectDefinitionsService>();

// Background job stale-running thresholds (reaper + scheduler guard)
builder.Services.Configure<CephasOps.Application.Workflow.BackgroundJobStaleOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Workflow.BackgroundJobStaleOptions.SectionName));
builder.Services.Configure<CephasOps.Application.Workflow.BackgroundJobFairnessOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Workflow.BackgroundJobFairnessOptions.SectionName));
// Tenant operational observability: guard (detection-only alerting signals)
builder.Services.Configure<CephasOps.Infrastructure.Operational.TenantOperationsGuardOptions>(
    builder.Configuration.GetSection(CephasOps.Infrastructure.Operational.TenantOperationsGuardOptions.SectionName));
builder.Services.AddSingleton<CephasOps.Infrastructure.Operational.ITenantOperationsGuard, CephasOps.Infrastructure.Operational.TenantOperationsGuard>();
builder.Services.Configure<CephasOps.Application.Rates.PayoutAnomalyAlertOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Rates.PayoutAnomalyAlertOptions.SectionName));

// Distributed worker coordination (Phase 1: identity, heartbeat, job ownership)
builder.Services.Configure<CephasOps.Application.Workers.WorkerOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Workers.WorkerOptions.SectionName));
builder.Services.AddSingleton<CephasOps.Application.Workers.WorkerIdentityHolder>();
builder.Services.AddSingleton<CephasOps.Application.Workers.IWorkerIdentity>(sp => sp.GetRequiredService<CephasOps.Application.Workers.WorkerIdentityHolder>());
builder.Services.AddScoped<CephasOps.Application.Workers.IWorkerCoordinator, CephasOps.Application.Workers.WorkerCoordinatorService>();
if (builder.Configuration.GetValue("ProductionRoles:RunJobWorkers", true) || builder.Configuration.GetValue("ProductionRoles:RunSchedulers", true))
    builder.Services.AddHostedService<CephasOps.Application.Workers.WorkerHeartbeatHostedService>();

// Distributed job scheduler (Phase 1: polling coordinator, safe claiming)
builder.Services.Configure<CephasOps.Application.Scheduler.SchedulerOptions>(
    builder.Configuration.GetSection(CephasOps.Application.Scheduler.SchedulerOptions.SectionName));
builder.Services.AddSingleton<CephasOps.Application.Scheduler.SchedulerDiagnostics>();
if (builder.Configuration.GetValue("ProductionRoles:RunSchedulers", true))
    builder.Services.AddHostedService<CephasOps.Application.Scheduler.JobPollingCoordinatorService>();

// Job observability (JobRun recording)
builder.Services.AddScoped<CephasOps.Application.Workflow.JobObservability.IJobRunRecorder, CephasOps.Application.Workflow.JobObservability.JobRunRecorder>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobObservability.IJobDefinitionProvider, CephasOps.Application.Workflow.JobObservability.JobDefinitionProvider>();
builder.Services.AddScoped<CephasOps.Application.Workflow.JobObservability.IJobRunRetentionService, CephasOps.Application.Workflow.JobObservability.JobRunRetentionService>();
// Background Job Processor (Phase 6)
if (builder.Configuration.GetValue("ProductionRoles:RunJobWorkers", true))
    builder.Services.AddHostedService<BackgroundJobProcessorService>();

// Email Ingestion Scheduler (Phase 6)
if (builder.Configuration.GetValue("ProductionRoles:RunSchedulers", true))
    builder.Services.AddHostedService<CephasOps.Application.Workflow.Services.EmailIngestionSchedulerService>();

// Stock-by-location snapshot scheduler (Phase 2.2.2) – daily snapshots for reporting
if (builder.Configuration.GetValue("ProductionRoles:RunSchedulers", true))
{
    builder.Services.AddHostedService<CephasOps.Application.Workflow.Services.StockSnapshotSchedulerService>();
    builder.Services.AddHostedService<CephasOps.Application.Workflow.Services.LedgerReconciliationSchedulerService>();
    builder.Services.AddHostedService<CephasOps.Application.Workflow.Services.PnlRebuildSchedulerService>();
    builder.Services.AddHostedService<CephasOps.Application.Workflow.Services.SlaEvaluationSchedulerService>();
    builder.Services.AddHostedService<CephasOps.Application.Rates.Services.MissingPayoutSnapshotSchedulerService>();
    builder.Services.AddHostedService<CephasOps.Application.Rates.Services.PayoutAnomalyAlertSchedulerService>();
}
// Email Cleanup Service (48-hour TTL for mail viewer)
builder.Services.AddScoped<IEmailCleanupService, EmailCleanupService>();
if (builder.Configuration.GetValue("ProductionRoles:RunEmailCleanup", true))
    builder.Services.AddHostedService<EmailCleanupService>();
if (builder.Configuration.GetValue("ProductionRoles:RunMetricsAggregation", true))
    builder.Services.AddHostedService<CephasOps.Api.HostedServices.TenantMetricsAggregationHostedService>();

// Testing Services
builder.Services.AddScoped<CephasOps.Application.Testing.Services.ITestRunnerService, CephasOps.Application.Testing.Services.TestRunnerService>();

// --- Phase 10: Drift Report CLI (deterministic exit codes, clean config error handling) ---
var cmdArgs = Environment.GetCommandLineArgs();
var driftReportCmd = cmdArgs.Length >= 2 && cmdArgs[1] == "drift-report";

WebApplication app;
try
{
    app = builder.Build();

    // Log BackgroundJobs:StaleRunning options once at startup (no secrets)
    using (var scope = app.Services.CreateScope())
    {
        var staleOpts = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CephasOps.Application.Workflow.BackgroundJobStaleOptions>>().Value;
        var startupLog = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        startupLog.LogInformation(
            "BackgroundJobs:StaleRunning loaded: EmailIngestMinutes={EmailIngestMinutes}, DefaultMinutes={DefaultMinutes}",
            staleOpts.EmailIngestMinutes, staleOpts.DefaultMinutes);
        // Initialize platform guard violation logging (category: PlatformGuardViolation) and optional in-memory buffer for operations overview
        var guardLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(CephasOps.Infrastructure.PlatformGuardLogger.CategoryName);
        var guardViolationBuffer = scope.ServiceProvider.GetService<CephasOps.Domain.PlatformSafety.IGuardViolationBuffer>();
        CephasOps.Infrastructure.PlatformGuardLogger.Initialize(guardLogger, guardViolationBuffer);

        // Tenant safety: fail fast if critical services are not registered
        if (builder.Environment.EnvironmentName != "Testing")
            CephasOps.Api.Production.TenantSafetyStartupValidator.Validate(scope.ServiceProvider);
    }

    // Startup schema guard: fail fast if critical tables are missing (prevents runtime "relation does not exist" after drift)
    if (builder.Environment.EnvironmentName != "Testing")
    {
        using (var guardScope = app.Services.CreateScope())
        {
            var guardContext = guardScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var guardLogger = guardScope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            await CephasOps.Api.Startup.StartupSchemaGuard.EnsureCriticalTablesExistAsync(guardContext, guardLogger);
        }
    }

    // Production config validation: fail fast when ASPNETCORE_ENVIRONMENT=Production and required config is missing
    CephasOps.Api.Production.ProductionStartupValidator.Validate(app.Configuration);

    // Production startup connectivity: fail fast if database (and Redis when configured) are unreachable
    await CephasOps.Api.Production.StartupConnectivityValidator.RunAsync(app);
}
catch (Exception ex)
{
    if (driftReportCmd)
    {
        Console.Error.WriteLine("drift-report failed (startup): " + ex.Message);
        Environment.Exit(1);
    }
    throw;
}

if (driftReportCmd)
{
    string? GetArg(string name)
    {
        for (var i = 2; i < cmdArgs.Length - 1; i++)
            if (cmdArgs[i] == name) return cmdArgs[i + 1];
        return null;
    }
    var daysStr = GetArg("--days") ?? "7";
    if (!int.TryParse(daysStr, out var reportDays) || reportDays < 1) reportDays = 7;
    Guid? profileIdFilter = null;
    var profileIdStr = GetArg("--profileId");
    if (!string.IsNullOrEmpty(profileIdStr) && Guid.TryParse(profileIdStr, out var pid)) profileIdFilter = pid;
    var format = (GetArg("--format") ?? "console").Trim().ToLowerInvariant();
    if (format != "console" && format != "markdown") format = "console";
    var outPath = GetArg("--out");
    var includeReplay = cmdArgs.Any(a => a == "--includeReplay");
    var dryRun = cmdArgs.Any(a => a == "--dry-run");
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var driftService = scope.ServiceProvider.GetRequiredService<IDriftReportService>();
            var result = await driftService.BuildReportAsync(reportDays, profileIdFilter, includeReplay);
            var output = format == "markdown" ? DriftReportFormatters.FormatMarkdown(result) : DriftReportFormatters.FormatConsole(result);
            var writeToFile = format == "markdown" && !string.IsNullOrEmpty(outPath) && !dryRun;
            if (writeToFile)
            {
                await File.WriteAllTextAsync(outPath!, output);
                Console.WriteLine("Wrote markdown report to " + outPath);
            }
            else
                Console.WriteLine(output);
            if (dryRun && !string.IsNullOrEmpty(outPath))
                Console.Error.WriteLine("Dry run: report not written to " + outPath);
        }
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine("drift-report failed: " + ex.Message);
        Environment.Exit(1);
    }
    Environment.Exit(0);
}

// --- Parser Replay CLI (Phase 6/9): run replay and exit without starting Kestrel ---
var replayCmd = cmdArgs.Length >= 2 ? cmdArgs[1] : null;
var isReplayCli = replayCmd == "replay" || replayCmd == "replay-pack" || replayCmd == "replay-profile-pack" || replayCmd == "replay-profiles" || replayCmd == "replay-all-profile-packs";
if (isReplayCli)
{
    string? GetArg(string name)
    {
        for (var i = 2; i < cmdArgs.Length - 1; i++)
            if (cmdArgs[i] == name) return cmdArgs[i + 1];
        return null;
    }
    bool CiMode() => cmdArgs.Any(a => a == "--ci-mode");
    using (var scope = app.Services.CreateScope())
    {
        var replay = scope.ServiceProvider.GetRequiredService<IParserReplayService>();
        var profileService = scope.ServiceProvider.GetRequiredService<ITemplateProfileService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        try
        {
            if (replayCmd == "replay-profile-pack")
            {
                var profileIdStr = GetArg("--profileId");
                if (string.IsNullOrEmpty(profileIdStr))
                {
                    Console.WriteLine("Usage: dotnet run -- replay-profile-pack --profileId <guid>");
                    Environment.Exit(1);
                }
                if (!Guid.TryParse(profileIdStr, out var profileIdGuid))
                {
                    Console.WriteLine("Invalid --profileId (must be a GUID).");
                    Environment.Exit(1);
                }
                var profileOpt = await profileService.GetProfileConfigByIdAsync(profileIdGuid);
                if (profileOpt == null)
                {
                    Console.WriteLine($"Profile not found or no PROFILE_JSON for template id: {profileIdGuid}");
                    Environment.Exit(1);
                }
                var (config, _) = profileOpt.Value;
                var pack = config.Pack;
                if (pack == null || ((pack.AttachmentIds == null || pack.AttachmentIds.Count == 0) && (pack.ParseSessionIds == null || pack.ParseSessionIds.Count == 0)))
                {
                    Console.WriteLine($"Profile {config.ProfileName} has no pack (attachmentIds or parseSessionIds).");
                    Environment.Exit(1);
                }
                var attachmentIds = await profileService.ResolvePackAttachmentIdsAsync(pack);
                if (attachmentIds.Count == 0)
                {
                    Console.WriteLine("Pack resolved to zero attachments.");
                    Environment.Exit(1);
                }
                const string triggeredBy = "ProfilePack";
                var lifecycle = new ProfileLifecycleContext
                {
                    ProfileId = config.ProfileId,
                    ProfileName = config.ProfileName,
                    ProfileVersion = config.ProfileVersion,
                    EffectiveFrom = config.EffectiveFrom,
                    Owner = config.Owner,
                    PackName = pack.PackName,
                    ProfileChangeNotes = config.ChangeNotes
                };
                var total = attachmentIds.Count;
                var regressions = 0;
                var improvements = 0;
                var noChange = 0;
                var errors = 0;
                var results = new List<ParserReplayResult>();
                foreach (var aid in attachmentIds)
                {
                    var r = await replay.ReplayByAttachmentIdAsync(aid, triggeredBy, lifecycle);
                    results.Add(r);
                    if (r.Error != null) errors++;
                    else if (r.RegressionDetected) regressions++;
                    else if (r.ImprovementDetected) improvements++;
                    else noChange++;
                }
                var ciMode = CiMode();
                Console.WriteLine("replay-profile-pack: Profile=" + config.ProfileName + " (" + (config.ProfileVersion ?? "n/a") + "), Pack=" + (pack.PackName ?? "n/a"));
                Console.WriteLine("Total=" + total + ", Regressions=" + regressions + ", Improvements=" + improvements + ", NoChange=" + noChange + ", Errors=" + errors);
                if (!ciMode)
                {
                    foreach (var r in results.Take(50))
                        Console.WriteLine("  " + r.AttachmentId + ": " + r.FileName + " | " + r.OldParseStatus + " (" + r.OldConfidence.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ") -> " + r.NewParseStatus + " (" + r.NewConfidence.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ") | Reg=" + r.RegressionDetected + " Imp=" + r.ImprovementDetected);
                }
                if (regressions > 0)
                {
                    Console.WriteLine("--- NO-GO: Regressions detected ---");
                    foreach (var r in results.Where(x => x.RegressionDetected))
                        Console.WriteLine("  AttachmentId=" + r.AttachmentId + ", OldStatus=" + r.OldParseStatus + ", NewStatus=" + r.NewParseStatus + ", OldCategory=" + r.OldParseFailureCategory + ", NewCategory=" + r.NewParseFailureCategory + ", DriftSignature=" + r.NewDriftSignature + ", Reason=" + r.ReasonForChange);
                    Console.WriteLine("Guidance: Revert profileVersion / restore previous PROFILE_JSON in ParserTemplates.Description.");
                    Environment.Exit(1);
                }
                Console.WriteLine("PASS");
                Environment.Exit(0);
            }
            else if (replayCmd == "replay-all-profile-packs")
            {
                var ciMode = CiMode();
                const string triggeredBy = "ReplayAllProfilePacks";
                var profiles = await profileService.GetAllProfileConfigsAsync(true);
                var overallRegressions = 0;
                var profilesWithPacks = 0;
                foreach (var (config, _) in profiles)
                {
                    var pack = config.Pack;
                    if (pack == null || ((pack.AttachmentIds == null || pack.AttachmentIds.Count == 0) && (pack.ParseSessionIds == null || pack.ParseSessionIds.Count == 0))) continue;
                    var attachmentIds = await profileService.ResolvePackAttachmentIdsAsync(pack);
                    if (attachmentIds.Count == 0) continue;
                    profilesWithPacks++;
                    var lifecycle = new ProfileLifecycleContext
                    {
                        ProfileId = config.ProfileId,
                        ProfileName = config.ProfileName,
                        ProfileVersion = config.ProfileVersion,
                        EffectiveFrom = config.EffectiveFrom,
                        Owner = config.Owner,
                        PackName = pack.PackName,
                        ProfileChangeNotes = config.ChangeNotes
                    };
                    var regressions = 0;
                    var improvements = 0;
                    var noChange = 0;
                    var errors = 0;
                    foreach (var aid in attachmentIds)
                    {
                        var r = await replay.ReplayByAttachmentIdAsync(aid, triggeredBy, lifecycle);
                        if (r.Error != null) errors++;
                        else if (r.RegressionDetected) regressions++;
                        else if (r.ImprovementDetected) improvements++;
                        else noChange++;
                    }
                    overallRegressions += regressions;
                    if (ciMode)
                        Console.WriteLine("Profile " + config.ProfileName + " (" + config.ProfileId + "): Total=" + attachmentIds.Count + ", Regressions=" + regressions + " " + (regressions > 0 ? "NO-GO" : "PASS"));
                    else
                        Console.WriteLine("Profile " + config.ProfileName + " (" + (config.ProfileVersion ?? "n/a") + "): Total=" + attachmentIds.Count + ", Regressions=" + regressions + ", Improvements=" + improvements + ", NoChange=" + noChange + ", Errors=" + errors);
                }
                Console.WriteLine("replay-all-profile-packs: Profiles with packs=" + profilesWithPacks + ", Overall Regressions=" + overallRegressions);
                if (overallRegressions > 0)
                {
                    Console.WriteLine("FAIL: Regressions detected. Exit code 1.");
                    Environment.Exit(1);
                }
                Console.WriteLine("PASS");
                Environment.Exit(0);
            }
            else if (replayCmd == "replay-profiles")
            {
                var daysStr = GetArg("--days") ?? "30";
                if (!int.TryParse(daysStr, out var days)) days = 30;
                var profiles = await profileService.GetAllProfileConfigsAsync(true);
                var overallRegressions = 0;
                var overallTotal = 0;
                foreach (var (config, templateId) in profiles)
                {
                    var attachmentIds = await replay.GetAttachmentIdsForProfileAsync(config.ProfileId, days);
                    if (attachmentIds.Count == 0) continue;
                    const string triggeredBy = "ReplayProfiles";
                    var lifecycle = new ProfileLifecycleContext
                    {
                        ProfileId = config.ProfileId,
                        ProfileName = config.ProfileName,
                        ProfileVersion = config.ProfileVersion,
                        EffectiveFrom = config.EffectiveFrom,
                        Owner = config.Owner
                    };
                    var regressions = 0;
                    var improvements = 0;
                    var noChange = 0;
                    var errors = 0;
                    foreach (var aid in attachmentIds)
                    {
                        var r = await replay.ReplayByAttachmentIdAsync(aid, triggeredBy, lifecycle);
                        if (r.Error != null) errors++;
                        else if (r.RegressionDetected) regressions++;
                        else if (r.ImprovementDetected) improvements++;
                        else noChange++;
                    }
                    overallTotal += attachmentIds.Count;
                    overallRegressions += regressions;
                    Console.WriteLine($"Profile {config.ProfileName} ({config.ProfileVersion ?? "n/a"}): Total={attachmentIds.Count}, Regressions={regressions}, Improvements={improvements}, NoChange={noChange}, Errors={errors}");
                }
                Console.WriteLine($"replay-profiles (last {days} days): Profiles={profiles.Count}, Overall Total={overallTotal}, Overall Regressions={overallRegressions}");
                if (overallRegressions > 0)
                {
                    Console.WriteLine("FAIL: Regressions detected. Exit code 1.");
                    Environment.Exit(1);
                }
                Environment.Exit(0);
            }
            else if (replayCmd == "replay-pack")
            {
                var triggeredBy = "CLI";
                var daysStr = GetArg("--days") ?? "30";
                if (!int.TryParse(daysStr, out var days)) days = 30;
                logger.LogInformation("Replay-pack: last {Days} days (failed or low-confidence)...", days);
                var pack = await replay.ReplayPackAsync(days, triggeredBy);
                Console.WriteLine($"Replay-pack: Total={pack.Total}, Regressions={pack.Regressions}, Improvements={pack.Improvements}, NoChange={pack.NoChange}, Errors={pack.Errors}");
                foreach (var r in pack.Results.Take(20))
                    Console.WriteLine($"  {r.FileName}: {r.OldParseStatus} ({r.OldConfidence:F2}) -> {r.NewParseStatus} ({r.NewConfidence:F2}) | Reg={r.RegressionDetected} Imp={r.ImprovementDetected}");
                if (pack.Regressions > 0)
                {
                    Console.WriteLine("FAIL: Regressions detected. Exit code 1.");
                    Environment.Exit(1);
                }
                Environment.Exit(0);
            }
            else
            {
                var triggeredBy = "CLI";
                var sessionIdStr = GetArg("--parseSessionId");
                var attachmentIdStr = GetArg("--attachmentId");
                if (attachmentIdStr != null && Guid.TryParse(attachmentIdStr, out var attachmentId))
                {
                    var result = await replay.ReplayByAttachmentIdAsync(attachmentId, triggeredBy);
                    if (result.Error != null) { Console.WriteLine($"Error: {result.Error}"); Environment.Exit(1); }
                    Console.WriteLine($"Old Status: {result.OldParseStatus} -> New: {result.NewParseStatus}");
                    Console.WriteLine($"Old Confidence: {result.OldConfidence:F2} -> New: {result.NewConfidence:F2}");
                    Console.WriteLine($"Missing fields: Old=[{string.Join(",", result.OldMissingFields ?? Array.Empty<string>())}] New=[{string.Join(",", result.NewMissingFields ?? Array.Empty<string>())}]");
                    Console.WriteLine($"Sheet: {result.OldSheetName} -> {result.NewSheetName} | HeaderRow: {result.OldHeaderRow} -> {result.NewHeaderRow}");
                    Console.WriteLine($"Regression={result.RegressionDetected} Improvement={result.ImprovementDetected}");
                    Environment.Exit(result.RegressionDetected ? 1 : 0);
                }
                else if (sessionIdStr != null && Guid.TryParse(sessionIdStr, out var sessionId))
                {
                    var results = await replay.ReplayByParseSessionIdAsync(sessionId, triggeredBy);
                    Console.WriteLine($"Replayed {results.Count} attachment(s) from session {sessionId}");
                    foreach (var r in results)
                    {
                        Console.WriteLine($"  {r.FileName}: {r.OldParseStatus} ({r.OldConfidence:F2}) -> {r.NewParseStatus} ({r.NewConfidence:F2}) | Reg={r.RegressionDetected} Imp={r.ImprovementDetected}");
                    }
                    var regressions = results.Count(x => x.RegressionDetected);
                    Environment.Exit(regressions > 0 ? 1 : 0);
                }
                else
                {
                    Console.WriteLine("Usage: dotnet run -- replay --attachmentId <guid> | --parseSessionId <guid>");
                    Environment.Exit(1);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Replay failed");
            Console.WriteLine($"Replay failed: {ex.Message}");
            Environment.Exit(1);
        }
    }
    return;
}

// Global exception handler must be first so unhandled exceptions return Problem Details
// Empty options satisfy middleware; IExceptionHandler (GlobalExceptionHandler) is invoked by the pipeline
app.UseExceptionHandler(_ => { });

// Register Syncfusion license from environment variable or fallback
var syncfusionLicenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY") 
    ?? "Ngo9BigBOggjHTQxAR8/V1JFaF5cXGRCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdmWH1edHVUR2BcVkVzWEBWYEg=";
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionLicenseKey);

// Configure the HTTP request pipeline
// CORS must be FIRST, before any other middleware
app.UseCors();
app.UseMiddleware<CorrelationIdMiddleware>();

// Swagger is enabled (UseSwaggerConfiguration handles environment checks internally)
app.UseSwaggerConfiguration();

// Only use HTTPS redirection in production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Routing must run before TenantGuard so endpoint metadata (AllowNoTenant, AllowAnonymous) is available
app.UseRouting();
// Authentication must come before Authorization
app.UseAuthentication();
app.UseMiddleware<CephasOps.Api.Middleware.TenantGuardMiddleware>();
app.UseMiddleware<RequestLogContextMiddleware>();
app.UseMiddleware<CephasOps.Api.Middleware.TenantUsageRecordingMiddleware>();
app.UseMiddleware<CephasOps.Api.Middleware.SubscriptionEnforcementMiddleware>();
app.UseMiddleware<CephasOps.Api.Middleware.TenantRateLimitMiddleware>();
// Set tenant scope for global query filters (SaaS multi-tenant)
app.Use(async (context, next) =>
{
    var tenantProvider = context.RequestServices.GetService<CephasOps.Application.Common.Interfaces.ITenantProvider>();
    if (tenantProvider != null)
        CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId = tenantProvider.CurrentTenantId;
    try
    {
        await next();
    }
    finally
    {
        CephasOps.Infrastructure.Persistence.TenantScope.CurrentTenantId = null;
    }
});
app.UseAuthorization();

// Ensure CORS headers are added even on error responses (401, 403, etc.)
app.Use(async (context, next) =>
{
    await next();
    if ((context.Response.StatusCode == 401 || context.Response.StatusCode == 403) &&
        !context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"))
    {
        var origin = context.Request.Headers["Origin"].ToString();
        var allowedOrigins = new[] { "http://localhost:5173", "http://localhost:3000", "http://localhost:5174" };
        if (allowedOrigins.Contains(origin))
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", origin);
            context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS, PATCH");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With");
        }
    }
});

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });
app.MapHealthChecks("/health/platform", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions { Predicate = r => r.Tags.Contains("platform") });
if (app.Configuration.GetValue("OpenTelemetry:Metrics:Enabled", true))
    app.UseOpenTelemetryPrometheusScrapingEndpoint();

// ============================================
// Database Seeding - DISABLED
// ============================================
// All seed data is now managed via PostgreSQL SQL migrations
// PostgreSQL is the single source of truth for all reference data
// 
// Seed data is applied via migration: 20260106014834_SeedAllReferenceData.cs
// 
// The DatabaseSeeder class is kept for reference but is no longer invoked.
// DocumentPlaceholderSeeder is also disabled - placeholders should be seeded via SQL if needed.
// ============================================

// DISABLED: C# DatabaseSeeder - All seeding now in PostgreSQL migrations
/*
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<DatabaseSeeder>>();
        var programLogger = services.GetRequiredService<ILogger<Program>>();
        
        programLogger.LogInformation("Starting database seeding...");
        var seeder = new DatabaseSeeder(context, logger);
        await seeder.SeedAsync();
        programLogger.LogInformation("Database seeding completed successfully.");
        
        // Seed document placeholder definitions
        programLogger.LogInformation("Starting document placeholder seeding...");
        var placeholderLogger = services.GetRequiredService<ILogger<DocumentPlaceholderSeeder>>();
        var placeholderSeeder = new DocumentPlaceholderSeeder(context, placeholderLogger);
        await placeholderSeeder.SeedAsync();
        programLogger.LogInformation("Document placeholder seeding completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database. Application will continue but login may not work.");
        // Don't throw - allow app to start even if seeding fails
    }
}
*/

app.Run();

// Expose for WebApplicationFactory in integration tests
public partial class Program { }
