using FileEntity = CephasOps.Domain.Files.Entities.File;
// MaterialCategory moved to Inventory namespace
using CephasOps.Domain.Inventory.Entities;
using CephasOps.Domain.RMA.Entities;
using CephasOps.Domain.Billing.Entities;
using CephasOps.Domain.Payroll.Entities;
using CephasOps.Domain.Pnl.Entities;
using CephasOps.Domain.Departments.Entities;
using CephasOps.Domain.Companies.Entities;
using CephasOps.Domain.Tenants.Entities;
using CephasOps.Domain.Users.Entities;
using CephasOps.Domain.Orders.Entities;
using CephasOps.Domain.Buildings.Entities;
using CephasOps.Domain.ServiceInstallers.Entities;
using CephasOps.Domain.Tasks.Entities;
using CephasOps.Domain.Scheduler.Entities;
using CephasOps.Domain.Parser.Entities;
using CephasOps.Domain.Notifications.Entities;
using CephasOps.Domain.Settings.Entities;
using CephasOps.Domain.Workflow.Entities;
using CephasOps.Domain.Assets.Entities;
using CephasOps.Domain.Commands;
using CephasOps.Domain.Events;
using CephasOps.Domain.Integration.Entities;
using CephasOps.Domain.Insights.Entities;
using CephasOps.Domain.Rates.Entities;
using CephasOps.Domain.Workers;
using CephasOps.Domain.Sla.Entities;
using CephasOps.Domain.Audit.Entities;
using CephasOps.Domain.Operations.Entities;
using CephasOps.Domain.Procurement.Entities;
using CephasOps.Domain.Sales.Entities;
using CephasOps.Domain.Projects.Entities;
using CephasOps.Domain.PlatformGuardian;
using CephasOps.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace CephasOps.Infrastructure.Persistence;

/// <summary>
/// Application database context
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Files
    public DbSet<FileEntity> Files => Set<FileEntity>();

    // Tenants (Phase 11)
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantOnboardingProgress> TenantOnboardingProgress => Set<TenantOnboardingProgress>();
    public DbSet<TenantActivityEvent> TenantActivityEvents => Set<TenantActivityEvent>();

    // Companies & Partners
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Vertical> Verticals => Set<Vertical>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<PartnerGroup> PartnerGroups => Set<PartnerGroup>();
    // public DbSet<CostCentre> CostCentres => Set<CostCentre>();
    // public DbSet<CompanyDocument> CompanyDocuments => Set<CompanyDocument>();

    // Users & RBAC
    public DbSet<User> Users => Set<User>();
    public DbSet<CephasOps.Domain.Users.Entities.RefreshToken> RefreshTokens => Set<CephasOps.Domain.Users.Entities.RefreshToken>();
    public DbSet<CephasOps.Domain.Users.Entities.PasswordResetToken> PasswordResetTokens => Set<CephasOps.Domain.Users.Entities.PasswordResetToken>();
    // public DbSet<UserCompany> UserCompanies => Set<UserCompany>(); // Company feature removed
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    // Orders
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderType> OrderTypes => Set<OrderType>();
    public DbSet<OrderCategory> OrderCategories => Set<OrderCategory>();
    public DbSet<OrderStatusLog> OrderStatusLogs => Set<OrderStatusLog>();
    public DbSet<OrderReschedule> OrderReschedules => Set<OrderReschedule>();
    public DbSet<OrderBlocker> OrderBlockers => Set<OrderBlocker>();
    public DbSet<OrderDocket> OrderDockets => Set<OrderDocket>();
    public DbSet<OrderMaterialUsage> OrderMaterialUsage => Set<OrderMaterialUsage>();
    public DbSet<OrderMaterialReplacement> OrderMaterialReplacements => Set<OrderMaterialReplacement>();
    public DbSet<OrderNonSerialisedReplacement> OrderNonSerialisedReplacements => Set<OrderNonSerialisedReplacement>();
    public DbSet<OrderStatusChecklistItem> OrderStatusChecklistItems => Set<OrderStatusChecklistItem>();
    public DbSet<OrderStatusChecklistAnswer> OrderStatusChecklistAnswers => Set<OrderStatusChecklistAnswer>();

    // Buildings
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<BuildingType> BuildingTypes => Set<BuildingType>();
    public DbSet<InstallationMethod> InstallationMethods => Set<InstallationMethod>();
    public DbSet<BuildingContact> BuildingContacts => Set<BuildingContact>();
    public DbSet<BuildingRules> BuildingRules => Set<BuildingRules>();
    public DbSet<BuildingBlock> BuildingBlocks => Set<BuildingBlock>();
    public DbSet<BuildingSplitter> BuildingSplitters => Set<BuildingSplitter>();
    public DbSet<Street> Streets => Set<Street>();
    public DbSet<HubBox> HubBoxes => Set<HubBox>();
    public DbSet<Pole> Poles => Set<Pole>();
    public DbSet<Splitter> Splitters => Set<Splitter>();
    public DbSet<SplitterType> SplitterTypes => Set<SplitterType>();
    public DbSet<SplitterPort> SplitterPorts => Set<SplitterPort>();
    public DbSet<BuildingDefaultMaterial> BuildingDefaultMaterials => Set<BuildingDefaultMaterial>();

    // Service Installers
    public DbSet<ServiceInstaller> ServiceInstallers => Set<ServiceInstaller>();
    public DbSet<ServiceInstallerContact> ServiceInstallerContacts => Set<ServiceInstallerContact>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<ServiceInstallerSkill> ServiceInstallerSkills => Set<ServiceInstallerSkill>();

    // Tasks
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();

    // Scheduler
    public DbSet<ScheduledSlot> ScheduledSlots => Set<ScheduledSlot>();
    public DbSet<SiAvailability> SiAvailabilities => Set<SiAvailability>();
    public DbSet<SiLeaveRequest> SiLeaveRequests => Set<SiLeaveRequest>();

    // Parser
    public DbSet<ParseSession> ParseSessions => Set<ParseSession>();
    public DbSet<ParsedOrderDraft> ParsedOrderDrafts => Set<ParsedOrderDraft>();
    public DbSet<ParsedMaterialAlias> ParsedMaterialAliases => Set<ParsedMaterialAlias>();
    public DbSet<EmailMessage> EmailMessages => Set<EmailMessage>();
    public DbSet<EmailAttachment> EmailAttachments => Set<EmailAttachment>();
    public DbSet<ParserRule> ParserRules => Set<ParserRule>();
    public DbSet<EmailAccount> EmailAccounts => Set<EmailAccount>();
    public DbSet<VipEmail> VipEmails => Set<VipEmail>();
    public DbSet<VipGroup> VipGroups => Set<VipGroup>();
    public DbSet<ParserTemplate> ParserTemplates => Set<ParserTemplate>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<ParserReplayRun> ParserReplayRuns => Set<ParserReplayRun>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationDispatch> NotificationDispatches => Set<NotificationDispatch>();
    public DbSet<NotificationSetting> NotificationSettings => Set<NotificationSetting>();

    // Inventory
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialPartner> MaterialPartners => Set<MaterialPartner>();
    public DbSet<CephasOps.Domain.Inventory.Entities.MaterialCategory> MaterialCategories => Set<CephasOps.Domain.Inventory.Entities.MaterialCategory>();
    public DbSet<MaterialVertical> MaterialVerticals => Set<MaterialVertical>();
    public DbSet<MaterialTag> MaterialTags => Set<MaterialTag>();
    public DbSet<MaterialAttribute> MaterialAttributes => Set<MaterialAttribute>();
    public DbSet<StockLocation> StockLocations => Set<StockLocation>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockLedgerEntry> StockLedgerEntries => Set<StockLedgerEntry>();
    public DbSet<StockAllocation> StockAllocations => Set<StockAllocation>();
    public DbSet<LedgerBalanceCache> LedgerBalanceCaches => Set<LedgerBalanceCache>();
    public DbSet<StockByLocationSnapshot> StockByLocationSnapshots => Set<StockByLocationSnapshot>();
    public DbSet<SerialisedItem> SerialisedItems => Set<SerialisedItem>();
    public DbSet<MovementType> MovementTypes => Set<MovementType>();
    public DbSet<LocationType> LocationTypes => Set<LocationType>();

    // RMA
    public DbSet<RmaRequest> RmaRequests => Set<RmaRequest>();
    public DbSet<RmaRequestItem> RmaRequestItems => Set<RmaRequestItem>();

    // Billing
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLineItem> InvoiceLineItems => Set<InvoiceLineItem>();
    public DbSet<InvoiceSubmissionHistory> InvoiceSubmissionHistory => Set<InvoiceSubmissionHistory>();
    public DbSet<BillingRatecard> BillingRatecards => Set<BillingRatecard>();
    public DbSet<SupplierInvoice> SupplierInvoices => Set<SupplierInvoice>();
    public DbSet<SupplierInvoiceLineItem> SupplierInvoiceLineItems => Set<SupplierInvoiceLineItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    // Phase 12: SaaS subscription billing
    public DbSet<BillingPlan> BillingPlans => Set<BillingPlan>();
    public DbSet<BillingPlanFeature> BillingPlanFeatures => Set<BillingPlanFeature>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<TenantFeatureFlag> TenantFeatureFlags => Set<TenantFeatureFlag>();
    public DbSet<TenantUsageRecord> TenantUsageRecords => Set<TenantUsageRecord>();
    public DbSet<TenantMetricsDaily> TenantMetricsDaily => Set<TenantMetricsDaily>();
    public DbSet<TenantMetricsMonthly> TenantMetricsMonthly => Set<TenantMetricsMonthly>();
    public DbSet<TenantInvoice> TenantInvoices => Set<TenantInvoice>();
    public DbSet<TenantAnomalyEvent> TenantAnomalyEvents => Set<TenantAnomalyEvent>();

    // Payroll
    public DbSet<PayrollPeriod> PayrollPeriods => Set<PayrollPeriod>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollLine> PayrollLines => Set<PayrollLine>();
    public DbSet<JobEarningRecord> JobEarningRecords => Set<JobEarningRecord>();
    public DbSet<SiRatePlan> SiRatePlans => Set<SiRatePlan>();

    // Rate Engine (Universal)
    public DbSet<RateCard> RateCards => Set<RateCard>();
    public DbSet<RateCardLine> RateCardLines => Set<RateCardLine>();
    public DbSet<CustomRate> CustomRates => Set<CustomRate>();

    // Rate Engine (GPON-specific)
    public DbSet<RateGroup> RateGroups => Set<RateGroup>();
    public DbSet<OrderTypeSubtypeRateGroup> OrderTypeSubtypeRateGroups => Set<OrderTypeSubtypeRateGroup>();
    public DbSet<BaseWorkRate> BaseWorkRates => Set<BaseWorkRate>();
    public DbSet<RateModifier> RateModifiers => Set<RateModifier>();
    public DbSet<ServiceProfile> ServiceProfiles => Set<ServiceProfile>();
    public DbSet<OrderCategoryServiceProfile> OrderCategoryServiceProfiles => Set<OrderCategoryServiceProfile>();
    public DbSet<GponPartnerJobRate> GponPartnerJobRates => Set<GponPartnerJobRate>();
    public DbSet<GponSiJobRate> GponSiJobRates => Set<GponSiJobRate>();
    public DbSet<GponSiCustomRate> GponSiCustomRates => Set<GponSiCustomRate>();
    public DbSet<OrderPayoutSnapshot> OrderPayoutSnapshots => Set<OrderPayoutSnapshot>();
    public DbSet<PayoutSnapshotRepairRun> PayoutSnapshotRepairRuns => Set<PayoutSnapshotRepairRun>();
    public DbSet<PayoutAnomalyReview> PayoutAnomalyReviews => Set<PayoutAnomalyReview>();
    public DbSet<PayoutAnomalyAlert> PayoutAnomalyAlerts => Set<PayoutAnomalyAlert>();
    public DbSet<PayoutAnomalyAlertRun> PayoutAnomalyAlertRuns => Set<PayoutAnomalyAlertRun>();

    // P&L
    public DbSet<PnlPeriod> PnlPeriods => Set<PnlPeriod>();
    public DbSet<PnlFact> PnlFacts => Set<PnlFact>();
    public DbSet<PnlDetailPerOrder> PnlDetailPerOrders => Set<PnlDetailPerOrder>();
    public DbSet<OverheadEntry> OverheadEntries => Set<OverheadEntry>();
    public DbSet<PnlType> PnlTypes => Set<PnlType>();
    public DbSet<OrderFinancialAlert> OrderFinancialAlerts => Set<OrderFinancialAlert>();

    // Departments
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<MaterialAllocation> MaterialAllocations => Set<MaterialAllocation>();
    public DbSet<DepartmentMembership> DepartmentMemberships => Set<DepartmentMembership>();

    // Assets
    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetMaintenance> AssetMaintenanceRecords => Set<AssetMaintenance>();
    public DbSet<AssetDepreciation> AssetDepreciationEntries => Set<AssetDepreciation>();
    public DbSet<AssetDisposal> AssetDisposals => Set<AssetDisposal>();

    // Settings (Phase 5)
    public DbSet<GlobalSetting> GlobalSettings => Set<GlobalSetting>();
    public DbSet<MaterialTemplate> MaterialTemplates => Set<MaterialTemplate>();
    public DbSet<MaterialTemplateItem> MaterialTemplateItems => Set<MaterialTemplateItem>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<GeneratedDocument> GeneratedDocuments => Set<GeneratedDocument>();
    public DbSet<DocumentPlaceholderDefinition> DocumentPlaceholderDefinitions => Set<DocumentPlaceholderDefinition>();
    public DbSet<KpiProfile> KpiProfiles => Set<KpiProfile>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<SmsTemplate> SmsTemplates => Set<SmsTemplate>();
    public DbSet<WhatsAppTemplate> WhatsAppTemplates => Set<WhatsAppTemplate>();
    public DbSet<SmsGateway> SmsGateways => Set<SmsGateway>();
    public DbSet<CustomerPreference> CustomerPreferences => Set<CustomerPreference>();
    public DbSet<TaxCode> TaxCodes => Set<TaxCode>();
    public DbSet<PaymentTerm> PaymentTerms => Set<PaymentTerm>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Bin> Bins => Set<Bin>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ServicePlan> ServicePlans => Set<ServicePlan>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<CostCentre> CostCentres => Set<CostCentre>();
    public DbSet<SlaProfile> SlaProfiles => Set<SlaProfile>();
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
    public DbSet<ApprovalWorkflow> ApprovalWorkflows => Set<ApprovalWorkflow>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<BusinessHours> BusinessHours => Set<BusinessHours>();
    public DbSet<PublicHoliday> PublicHolidays => Set<PublicHoliday>();
    public DbSet<EscalationRule> EscalationRules => Set<EscalationRule>();
    public DbSet<GuardConditionDefinition> GuardConditionDefinitions => Set<GuardConditionDefinition>();
    public DbSet<SideEffectDefinition> SideEffectDefinitions => Set<SideEffectDefinition>();

    // Workflow Engine (Phase 6)
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();
    public DbSet<WorkflowJob> WorkflowJobs => Set<WorkflowJob>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowStepRecord> WorkflowStepRecords => Set<WorkflowStepRecord>();
    public DbSet<WorkflowTransitionHistoryEntry> WorkflowTransitionHistory => Set<WorkflowTransitionHistoryEntry>();
    public DbSet<BackgroundJob> BackgroundJobs => Set<BackgroundJob>();
    public DbSet<JobExecution> JobExecutions => Set<JobExecution>();
    public DbSet<JobDefinition> JobDefinitions => Set<JobDefinition>();
    public DbSet<JobRun> JobRuns => Set<JobRun>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
    public DbSet<EventStoreEntry> EventStore => Set<EventStoreEntry>();
    public DbSet<EventStoreAttemptHistory> EventStoreAttemptHistory => Set<EventStoreAttemptHistory>();
    public DbSet<ReplayOperation> ReplayOperations => Set<ReplayOperation>();
    public DbSet<ReplayOperationEvent> ReplayOperationEvents => Set<ReplayOperationEvent>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<EventProcessingLog> EventProcessingLog => Set<EventProcessingLog>();
    public DbSet<CommandProcessingLog> CommandProcessingLogs => Set<CommandProcessingLog>();
    public DbSet<ReplayExecutionLock> ReplayExecutionLock => Set<ReplayExecutionLock>();
    public DbSet<RebuildOperation> RebuildOperations => Set<RebuildOperation>();
    public DbSet<RebuildExecutionLock> RebuildExecutionLocks => Set<RebuildExecutionLock>();
    public DbSet<WorkerInstance> WorkerInstances => Set<WorkerInstance>();

    // External Integration (Phase 10)
    public DbSet<ConnectorDefinition> ConnectorDefinitions => Set<ConnectorDefinition>();
    public DbSet<ConnectorEndpoint> ConnectorEndpoints => Set<ConnectorEndpoint>();
    public DbSet<OutboundIntegrationDelivery> OutboundIntegrationDeliveries => Set<OutboundIntegrationDelivery>();
    public DbSet<OutboundIntegrationAttempt> OutboundIntegrationAttempts => Set<OutboundIntegrationAttempt>();
    public DbSet<InboundWebhookReceipt> InboundWebhookReceipts => Set<InboundWebhookReceipt>();
    public DbSet<ExternalIdempotencyRecord> ExternalIdempotencyRecords => Set<ExternalIdempotencyRecord>();

    // Field Ops Intelligence (Insights)
    public DbSet<OperationalInsight> OperationalInsights => Set<OperationalInsight>();

    // SLA Intelligence
    public DbSet<SlaRule> SlaRules => Set<SlaRule>();
    public DbSet<SlaBreach> SlaBreaches => Set<SlaBreach>();

    // Audit
    public DbSet<AuditOverride> AuditOverrides => Set<AuditOverride>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Operations (migration deployment audit; operator-recorded only)
    public DbSet<MigrationAudit> MigrationAudits => Set<MigrationAudit>();

    // Procurement
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();

    // Sales
    public DbSet<Quotation> Quotations => Set<Quotation>();
    public DbSet<QuotationItem> QuotationItems => Set<QuotationItem>();

    // Projects
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<BoqItem> BoqItems => Set<BoqItem>();

    // Delivery Orders (Inventory extension)
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();
    public DbSet<DeliveryOrderItem> DeliveryOrderItems => Set<DeliveryOrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // StockAllocation: three optional FKs to StockLedgerEntry (reserved/issued/returned)
        modelBuilder.Entity<StockAllocation>()
            .HasOne(a => a.LedgerEntryReserved)
            .WithMany()
            .HasForeignKey(a => a.LedgerEntryIdReserved)
            .IsRequired(false);
        modelBuilder.Entity<StockAllocation>()
            .HasOne(a => a.LedgerEntryIssued)
            .WithMany()
            .HasForeignKey(a => a.LedgerEntryIdIssued)
            .IsRequired(false);
        modelBuilder.Entity<StockAllocation>()
            .HasOne(a => a.LedgerEntryReturned)
            .WithMany()
            .HasForeignKey(a => a.LedgerEntryIdReturned)
            .IsRequired(false);
        // StockLedgerEntry -> StockAllocation (optional)
        modelBuilder.Entity<StockLedgerEntry>()
            .HasOne(e => e.Allocation)
            .WithMany()
            .HasForeignKey(e => e.AllocationId)
            .IsRequired(false);

        // StockLedgerEntry: performance indexes for ledger list, stock-summary, usage-summary, serial lifecycle (Phase 2.3.2)
        modelBuilder.Entity<StockLedgerEntry>()
            .HasIndex(e => new { e.CompanyId, e.IsDeleted, e.CreatedAt })
            .HasDatabaseName("IX_StockLedgerEntries_CompanyId_IsDeleted_CreatedAt")
            .IsDescending(false, false, true);
        modelBuilder.Entity<StockLedgerEntry>()
            .HasIndex(e => new { e.CompanyId, e.IsDeleted, e.MaterialId, e.LocationId })
            .HasDatabaseName("IX_StockLedgerEntries_CompanyId_IsDeleted_MaterialId_LocationId");
        modelBuilder.Entity<StockLedgerEntry>()
            .HasIndex(e => new { e.CompanyId, e.IsDeleted, e.OrderId })
            .HasDatabaseName("IX_StockLedgerEntries_CompanyId_IsDeleted_OrderId");
        modelBuilder.Entity<StockLedgerEntry>()
            .HasIndex(e => new { e.SerialisedItemId, e.CreatedAt })
            .HasDatabaseName("IX_StockLedgerEntries_SerialisedItemId_CreatedAt");

        // StockAllocation: performance indexes for reserved aggregate and available-qty (Phase 2.3.2)
        modelBuilder.Entity<StockAllocation>()
            .HasIndex(a => new { a.CompanyId, a.IsDeleted, a.Status })
            .HasDatabaseName("IX_StockAllocations_CompanyId_IsDeleted_Status");
        modelBuilder.Entity<StockAllocation>()
            .HasIndex(a => new { a.MaterialId, a.LocationId, a.Status })
            .HasDatabaseName("IX_StockAllocations_MaterialId_LocationId_Status");

        // LedgerBalanceCache: derived balance cache (Phase 2.3.3); one row per (CompanyId, MaterialId, LocationId)
        modelBuilder.Entity<LedgerBalanceCache>()
            .ToTable("LedgerBalanceCaches");
        modelBuilder.Entity<LedgerBalanceCache>()
            .HasIndex(c => new { c.CompanyId, c.MaterialId, c.LocationId })
            .IsUnique()
            .HasDatabaseName("IX_LedgerBalanceCaches_CompanyId_MaterialId_LocationId");
        modelBuilder.Entity<LedgerBalanceCache>()
            .HasIndex(c => new { c.CompanyId, c.DepartmentId })
            .HasDatabaseName("IX_LedgerBalanceCaches_CompanyId_DepartmentId");

        // StockByLocationSnapshot: period snapshots for stock-by-location history (Phase 2.2.2)
        modelBuilder.Entity<StockByLocationSnapshot>()
            .ToTable("StockByLocationSnapshots");
        modelBuilder.Entity<StockByLocationSnapshot>()
            .HasIndex(s => new { s.CompanyId, s.MaterialId, s.LocationId, s.PeriodStart, s.SnapshotType })
            .IsUnique()
            .HasDatabaseName("IX_StockByLocationSnapshots_CompanyId_MaterialId_LocationId_PeriodStart_Type");
        modelBuilder.Entity<StockByLocationSnapshot>()
            .HasIndex(s => new { s.CompanyId, s.DepartmentId, s.PeriodStart, s.SnapshotType })
            .HasDatabaseName("IX_StockByLocationSnapshots_CompanyId_DepartmentId_Period_Type");

        // OrderType self-reference: Parent -> Children (subtypes)
        modelBuilder.Entity<OrderType>()
            .HasOne(ot => ot.ParentOrderType)
            .WithMany(ot => ot.Children)
            .HasForeignKey(ot => ot.ParentOrderTypeId)
            .IsRequired(false);
        modelBuilder.Entity<OrderType>()
            .HasIndex(ot => ot.ParentOrderTypeId)
            .HasDatabaseName("IX_OrderTypes_ParentOrderTypeId");

        // Apply global query filter: soft delete + tenant isolation on all CompanyScopedEntity types
        var tenantScopeType = typeof(Persistence.TenantScope);
        var currentTenantIdProp = tenantScopeType.GetProperty(nameof(Persistence.TenantScope.CurrentTenantId), BindingFlags.Public | BindingFlags.Static);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(CephasOps.Domain.Common.CompanyScopedEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var isDeletedProp = Expression.Property(parameter, nameof(CephasOps.Domain.Common.CompanyScopedEntity.IsDeleted));
                var isNotDeleted = Expression.Equal(isDeletedProp, Expression.Constant(false));
                var companyIdProp = Expression.Property(parameter, nameof(CephasOps.Domain.Common.CompanyScopedEntity.CompanyId));
                var currentTenantIdAccess = Expression.MakeMemberAccess(null, currentTenantIdProp!);
                var tenantIdIsNull = Expression.Equal(currentTenantIdAccess, Expression.Constant(null, typeof(Guid?)));
                var companyIdEqualsTenant = Expression.Equal(companyIdProp, currentTenantIdAccess);
                var tenantPart = Expression.OrElse(tenantIdIsNull, companyIdEqualsTenant);
                var body = Expression.AndAlso(isNotDeleted, tenantPart);
                var lambda = Expression.Lambda(body, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        // Tenant filter for User (not CompanyScopedEntity)
        var userParam = Expression.Parameter(typeof(User), "e");
        var userCompanyIdProp = Expression.Property(userParam, nameof(User.CompanyId));
        var userCurrentIdAccess = Expression.MakeMemberAccess(null, currentTenantIdProp!);
        var userTenantIdIsNull = Expression.Equal(userCurrentIdAccess, Expression.Constant(null, typeof(Guid?)));
        var userCompanyIdEqualsTenant = Expression.Equal(userCompanyIdProp, userCurrentIdAccess);
        modelBuilder.Entity<User>().HasQueryFilter(Expression.Lambda(Expression.OrElse(userTenantIdIsNull, userCompanyIdEqualsTenant), userParam));

        // Tenant filter for BackgroundJob (not CompanyScopedEntity)
        var jobParam = Expression.Parameter(typeof(BackgroundJob), "e");
        var jobCompanyIdProp = Expression.Property(jobParam, nameof(BackgroundJob.CompanyId));
        var jobTenantIdIsNull = Expression.Equal(userCurrentIdAccess, Expression.Constant(null, typeof(Guid?)));
        var jobCompanyIdEqualsTenant = Expression.Equal(jobCompanyIdProp, userCurrentIdAccess);
        modelBuilder.Entity<BackgroundJob>().HasQueryFilter(Expression.Lambda(Expression.OrElse(jobTenantIdIsNull, jobCompanyIdEqualsTenant), jobParam));

        // Tenant filter for JobExecution (not CompanyScopedEntity; worker uses raw SQL to claim, then sets TenantScope per job)
        var jeParam = Expression.Parameter(typeof(CephasOps.Domain.Workflow.Entities.JobExecution), "e");
        var jeCompanyIdProp = Expression.Property(jeParam, nameof(CephasOps.Domain.Workflow.Entities.JobExecution.CompanyId));
        var jeTenantIdIsNull = Expression.Equal(Expression.MakeMemberAccess(null, currentTenantIdProp!), Expression.Constant(null, typeof(Guid?)));
        var jeCompanyIdEqualsTenant = Expression.Equal(jeCompanyIdProp, Expression.MakeMemberAccess(null, currentTenantIdProp!));
        modelBuilder.Entity<CephasOps.Domain.Workflow.Entities.JobExecution>().HasQueryFilter(Expression.Lambda(Expression.OrElse(jeTenantIdIsNull, jeCompanyIdEqualsTenant), jeParam));

        // Tenant filter for OrderPayoutSnapshot (not CompanyScopedEntity)
        var opsParam = Expression.Parameter(typeof(CephasOps.Domain.Rates.Entities.OrderPayoutSnapshot), "e");
        var opsCompanyIdProp = Expression.Property(opsParam, nameof(CephasOps.Domain.Rates.Entities.OrderPayoutSnapshot.CompanyId));
        var opsTenantIdIsNull = Expression.Equal(Expression.MakeMemberAccess(null, currentTenantIdProp!), Expression.Constant(null, typeof(Guid?)));
        var opsCompanyIdEqualsTenant = Expression.Equal(opsCompanyIdProp, Expression.MakeMemberAccess(null, currentTenantIdProp!));
        modelBuilder.Entity<CephasOps.Domain.Rates.Entities.OrderPayoutSnapshot>().HasQueryFilter(Expression.Lambda(Expression.OrElse(opsTenantIdIsNull, opsCompanyIdEqualsTenant), opsParam));

        // Tenant filter for InboundWebhookReceipt (not CompanyScopedEntity; platform retention uses IgnoreQueryFilters for delete)
        var iwrParam = Expression.Parameter(typeof(CephasOps.Domain.Integration.Entities.InboundWebhookReceipt), "e");
        var iwrCompanyIdProp = Expression.Property(iwrParam, nameof(CephasOps.Domain.Integration.Entities.InboundWebhookReceipt.CompanyId));
        var iwrTenantIdIsNull = Expression.Equal(Expression.MakeMemberAccess(null, currentTenantIdProp!), Expression.Constant(null, typeof(Guid?)));
        var iwrCompanyIdEqualsTenant = Expression.Equal(iwrCompanyIdProp, Expression.MakeMemberAccess(null, currentTenantIdProp!));
        modelBuilder.Entity<CephasOps.Domain.Integration.Entities.InboundWebhookReceipt>().HasQueryFilter(Expression.Lambda(Expression.OrElse(iwrTenantIdIsNull, iwrCompanyIdEqualsTenant), iwrParam));
    }

    /// <summary>
    /// Gets the CompanyId from a tenant-scoped entity via reflection (used for SaveChanges tenant-integrity validation).
    /// All types considered tenant-scoped by TenantSafetyGuard have a CompanyId property (Guid or Guid?).
    /// </summary>
    private static Guid? GetEntityCompanyId(object entity)
    {
        if (entity == null) return null;
        var prop = entity.GetType().GetProperty("CompanyId", BindingFlags.Public | BindingFlags.Instance);
        if (prop == null) return null;
        var value = prop.GetValue(entity);
        if (value == null) return null;
        if (value is Guid g) return g;
        return null;
    }

    /// <summary>
    /// Validates that all tenant-scoped changes have valid tenant context and entity CompanyId matches current tenant.
    /// Called by both SaveChanges and SaveChangesAsync. Throws if guard is violated (unless platform bypass is active).
    /// </summary>
    private void ValidateTenantScopeBeforeSave()
    {
        if (!TenantSafetyGuard.IsPlatformBypassActive)
        {
            var tenantId = TenantScope.CurrentTenantId;
            var hasTenantContext = tenantId.HasValue && tenantId.Value != Guid.Empty;
            if (!hasTenantContext)
            {
                foreach (var entry in ChangeTracker.Entries())
                {
                    if (entry.State != EntityState.Added && entry.State != EntityState.Modified && entry.State != EntityState.Deleted)
                        continue;
                    if (TenantSafetyGuard.IsTenantScopedEntityType(entry.Entity.GetType()))
                    {
                        var entityTypeName = entry.Entity.GetType().Name;
                        var detail = $"Entity: {entityTypeName}, State: {entry.State}.";
                        PlatformGuardLogger.LogViolation("TenantSafetyGuard", "SaveChanges", detail, companyId: null, entityType: entityTypeName);
                        throw new InvalidOperationException(
                            "TenantSafetyGuard: Cannot save tenant-scoped entity without tenant context. " +
                            "Set TenantScope.CurrentTenantId (e.g. by API middleware or job worker) or use TenantSafetyGuard.EnterPlatformBypass() for platform-wide operations. " +
                            detail);
                    }
                }
            }
            else
            {
                var currentTenantId = tenantId!.Value;
                foreach (var entry in ChangeTracker.Entries())
                {
                    if (entry.State != EntityState.Added && entry.State != EntityState.Modified && entry.State != EntityState.Deleted)
                        continue;
                    if (!TenantSafetyGuard.IsTenantScopedEntityType(entry.Entity.GetType()))
                        continue;
                    var entityCompanyId = GetEntityCompanyId(entry.Entity);
                    var entityTypeName = entry.Entity.GetType().Name;
                    if (entry.State == EntityState.Added)
                    {
                        if (entityCompanyId.HasValue && entityCompanyId.Value != currentTenantId)
                        {
                            var detail = $"Entity: {entityTypeName}, State: Added. Entity CompanyId={entityCompanyId}, CurrentTenantId={currentTenantId}.";
                            PlatformGuardLogger.LogViolation("TenantSafetyGuard", "SaveChangesTenantIntegrity", detail, entityType: entityTypeName);
                            throw new InvalidOperationException(
                                "TenantSafetyGuard: Tenant integrity violation. Cannot add tenant-scoped entity with CompanyId that does not match current tenant. " +
                                detail);
                        }
                    }
                    else
                    {
                        if (entityCompanyId != currentTenantId)
                        {
                            var detail = $"Entity: {entityTypeName}, State: {entry.State}. Entity CompanyId={entityCompanyId}, CurrentTenantId={currentTenantId}.";
                            PlatformGuardLogger.LogViolation("TenantSafetyGuard", "SaveChangesTenantIntegrity", detail, companyId: currentTenantId, entityType: entityTypeName);
                            throw new InvalidOperationException(
                                "TenantSafetyGuard: Tenant integrity violation. Cannot modify or delete tenant-scoped entity that does not belong to current tenant. " +
                                detail);
                        }
                    }
                }
            }
        }
    }

    public override int SaveChanges()
    {
        ValidateTenantScopeBeforeSave();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ValidateTenantScopeBeforeSave();

        // Auto-generate RowVersion for entities that don't have one (PostgreSQL workaround)
        // Also ensure all DateTime properties are explicitly UTC for PostgreSQL
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                // Handle CompanyScopedEntity DateTime properties
                if (entry.Entity is CephasOps.Domain.Common.CompanyScopedEntity companyScopedEntity)
                {
                    if (companyScopedEntity.RowVersion == null || companyScopedEntity.RowVersion.Length == 0)
                    {
                        companyScopedEntity.RowVersion = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);
                    }

                    // Ensure all DateTime properties are explicitly UTC for PostgreSQL
                    // This fixes the "Cannot write DateTime with Kind=Unspecified" error
                    if (companyScopedEntity.CreatedAt.Kind != DateTimeKind.Utc)
                    {
                        companyScopedEntity.CreatedAt = companyScopedEntity.CreatedAt.Kind == DateTimeKind.Local 
                            ? companyScopedEntity.CreatedAt.ToUniversalTime() 
                            : DateTime.SpecifyKind(companyScopedEntity.CreatedAt, DateTimeKind.Utc);
                    }

                    if (companyScopedEntity.UpdatedAt.Kind != DateTimeKind.Utc)
                    {
                        companyScopedEntity.UpdatedAt = companyScopedEntity.UpdatedAt.Kind == DateTimeKind.Local 
                            ? companyScopedEntity.UpdatedAt.ToUniversalTime() 
                            : DateTime.SpecifyKind(companyScopedEntity.UpdatedAt, DateTimeKind.Utc);
                    }

                    if (companyScopedEntity.DeletedAt.HasValue && companyScopedEntity.DeletedAt.Value.Kind != DateTimeKind.Utc)
                    {
                        companyScopedEntity.DeletedAt = companyScopedEntity.DeletedAt.Value.Kind == DateTimeKind.Local 
                            ? companyScopedEntity.DeletedAt.Value.ToUniversalTime() 
                            : DateTime.SpecifyKind(companyScopedEntity.DeletedAt.Value, DateTimeKind.Utc);
                    }
                }

                // Handle ParseSession-specific CompletedAt property
                if (entry.Entity is CephasOps.Domain.Parser.Entities.ParseSession parseSession && 
                    parseSession.CompletedAt.HasValue && 
                    parseSession.CompletedAt.Value.Kind != DateTimeKind.Utc)
                {
                    parseSession.CompletedAt = parseSession.CompletedAt.Value.Kind == DateTimeKind.Local 
                        ? parseSession.CompletedAt.Value.ToUniversalTime() 
                        : DateTime.SpecifyKind(parseSession.CompletedAt.Value, DateTimeKind.Utc);
                }

                // Handle ParsedOrderDraft-specific AppointmentDate property
                if (entry.Entity is CephasOps.Domain.Parser.Entities.ParsedOrderDraft parsedOrderDraft)
                {
                    if (parsedOrderDraft.AppointmentDate.HasValue && 
                        parsedOrderDraft.AppointmentDate.Value.Kind != DateTimeKind.Utc)
                    {
                        parsedOrderDraft.AppointmentDate = parsedOrderDraft.AppointmentDate.Value.Kind == DateTimeKind.Local 
                            ? parsedOrderDraft.AppointmentDate.Value.ToUniversalTime() 
                            : DateTime.SpecifyKind(parsedOrderDraft.AppointmentDate.Value, DateTimeKind.Utc);
                    }
                }

                // Use reflection to catch any other DateTime properties we might have missed
                var entityType = entry.Entity.GetType();
                var properties = entityType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                foreach (var property in properties)
                {
                    // Handle DateTime properties
                    if (property.PropertyType == typeof(DateTime))
                    {
                        var value = property.GetValue(entry.Entity);
                        if (value is DateTime dateTime && dateTime.Kind != DateTimeKind.Utc)
                        {
                            var utcValue = dateTime.Kind == DateTimeKind.Local 
                                ? dateTime.ToUniversalTime() 
                                : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                            property.SetValue(entry.Entity, utcValue);
                        }
                    }
                    // Handle DateTime? properties
                    else if (property.PropertyType == typeof(DateTime?))
                    {
                        var value = property.GetValue(entry.Entity);
                        if (value != null && value is DateTime nullableDateTime)
                        {
                            if (nullableDateTime.Kind != DateTimeKind.Utc)
                            {
                                var utcValue = nullableDateTime.Kind == DateTimeKind.Local 
                                    ? nullableDateTime.ToUniversalTime() 
                                    : DateTime.SpecifyKind(nullableDateTime, DateTimeKind.Utc);
                                property.SetValue(entry.Entity, utcValue);
                            }
                        }
                    }
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

