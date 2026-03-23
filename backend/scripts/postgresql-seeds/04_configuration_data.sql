-- ============================================
-- Configuration Data Seed Script
-- ============================================
-- Seeds: ParserTemplates, GuardConditionDefinitions, SideEffectDefinitions, GlobalSettings
-- Dependencies: Companies
-- ============================================

DO $$
DECLARE
    v_company_id UUID;
BEGIN
    -- Get company ID
    SELECT "Id" INTO v_company_id FROM "Companies" ORDER BY "CreatedAt" LIMIT 1;
    
    IF v_company_id IS NULL THEN
        RAISE WARNING 'No company found. Some configuration data requires company ID.';
    END IF;
    
    -- ============================================
    -- 1. ParserTemplates
    -- ============================================
    INSERT INTO "ParserTemplates" (
        "Id", "CompanyId", "Name", "Code", "Description", "PartnerPattern", 
        "SubjectPattern", "OrderTypeCode", "Priority", "IsActive", "AutoApprove", "CreatedAt"
    )
    SELECT gen_random_uuid(), NULL, v.name, v.code, v.description, v.partner_pattern, v.subject_pattern, v.order_type_code, v.priority, true, false, NOW()
    FROM (VALUES
        ('TIME Activation', 'TIME_ACTIVATION', 'Parses TIME FTTH/HSBB activation work orders', '*@time.com.my', '*Activation*', 'ACTIVATION', 100),
        ('TIME Modification (Indoor)', 'TIME_MOD_INDOOR', 'Parses TIME indoor modification work orders', '*@time.com.my', '*Modification*Indoor*', 'MODIFICATION_INDOOR', 95),
        ('TIME Modification (Outdoor)', 'TIME_MOD_OUTDOOR', 'Parses TIME outdoor modification work orders', '*@time.com.my', '*Modification*Outdoor*', 'MODIFICATION_OUTDOOR', 95),
        ('TIME Modification (General)', 'TIME_MODIFICATION', 'Parses TIME general modification work orders', '*@time.com.my', '*Modification*', 'MODIFICATION', 90),
        ('TIME Termination', 'TIME_TERMINATION', 'Parses TIME termination/cancellation work orders', '*@time.com.my', '*Termination*', 'TERMINATION', 80),
        ('TIME Relocation', 'TIME_RELOCATION', 'Parses TIME relocation work orders', '*@time.com.my', '*Relocation*', 'RELOCATION', 85),
        ('TIME Assurance', 'TIME_ASSURANCE', 'Parses TIME assurance/troubleshooting work orders', '*@time.com.my', '*Assurance*', 'ASSURANCE', 70),
        ('TIME General (Fallback)', 'TIME_GENERAL', 'Fallback template for TIME work orders that don''t match other patterns', '*@time.com.my', '*Work Order*', 'GENERAL', 10),
        ('Celcom HSBB', 'CELCOM_HSBB', 'Parses Celcom HSBB work orders via TIME', '*celcom*', '*HSBB*', 'ACTIVATION', 100)
    ) AS v(name, code, description, partner_pattern, subject_pattern, order_type_code, priority)
    WHERE NOT EXISTS (
        SELECT 1 FROM "ParserTemplates" pt WHERE pt."Code" = v.code
    );
    
    RAISE NOTICE 'Seeded ParserTemplates (9 records)';
    
    -- ============================================
    -- 2. GuardConditionDefinitions
    -- ============================================
    IF v_company_id IS NOT NULL THEN
        INSERT INTO "GuardConditionDefinitions" (
            "Id", "CompanyId", "Key", "Name", "Description", "EntityType", 
            "ValidatorType", "ValidatorConfigJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt", "IsDeleted"
        ) VALUES
            (gen_random_uuid(), v_company_id, 'photosRequired', 'Photos Required', 'Checks if photos are uploaded for the order', 'Order', 'PhotosRequiredValidator', '{"checkFlag":true,"checkFiles":true}', true, 1, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'docketUploaded', 'Docket Uploaded', 'Checks if docket is uploaded for the order', 'Order', 'DocketUploadedValidator', '{"checkFlag":true,"checkDockets":true}', true, 2, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'splitterAssigned', 'Splitter Assigned', 'Checks if splitter port is assigned to the order', 'Order', 'SplitterAssignedValidator', NULL, true, 3, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'serialNumbersValidated', 'Serial Numbers Validated', 'Checks if serial numbers are validated for the order', 'Order', 'SerialsValidatedValidator', NULL, true, 4, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'materialsSpecified', 'Materials Specified', 'Checks if materials are specified for the order', 'Order', 'MaterialsSpecifiedValidator', NULL, true, 5, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'siaAssigned', 'SI Assigned', 'Checks if Service Installer (SI) is assigned to the order', 'Order', 'SiAssignedValidator', NULL, true, 6, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'appointmentDateSet', 'Appointment Date Set', 'Checks if appointment date is set for the order', 'Order', 'AppointmentDateSetValidator', NULL, true, 7, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'buildingSelected', 'Building Selected', 'Checks if building is selected for the order', 'Order', 'BuildingSelectedValidator', NULL, true, 8, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'customerContactProvided', 'Customer Contact Provided', 'Checks if customer contact (phone or email) is provided for the order', 'Order', 'CustomerContactProvidedValidator', NULL, true, 9, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'noBlockersActive', 'No Active Blockers', 'Checks if there are no active blockers for the order', 'Order', 'NoActiveBlockersValidator', NULL, true, 10, NOW(), NOW(), false)
        ON CONFLICT ("CompanyId", "Key", "EntityType") WHERE "IsDeleted" = false DO NOTHING;
        
        RAISE NOTICE 'Seeded GuardConditionDefinitions (10 records)';
    END IF;
    
    -- ============================================
    -- 3. SideEffectDefinitions
    -- ============================================
    IF v_company_id IS NOT NULL THEN
        INSERT INTO "SideEffectDefinitions" (
            "Id", "CompanyId", "Key", "Name", "Description", "EntityType", 
            "ExecutorType", "ExecutorConfigJson", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt", "IsDeleted"
        ) VALUES
            (gen_random_uuid(), v_company_id, 'notify', 'Send Notification', 'Sends a notification to relevant users when workflow transition occurs', 'Order', 'NotifySideEffectExecutor', '{"template":"OrderStatusChange"}', true, 1, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'createStockMovement', 'Create Stock Movement', 'Creates stock movement records when workflow transition occurs', 'Order', 'CreateStockMovementSideEffectExecutor', NULL, true, 2, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'createOrderStatusLog', 'Create Order Status Log', 'Creates an order status log entry when workflow transition occurs', 'Order', 'CreateOrderStatusLogSideEffectExecutor', NULL, true, 3, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'updateOrderFlags', 'Update Order Flags', 'Updates order flags (DocketUploaded, PhotosUploaded, etc.) when workflow transition occurs', 'Order', 'UpdateOrderFlagsSideEffectExecutor', NULL, true, 4, NOW(), NOW(), false),
            (gen_random_uuid(), v_company_id, 'triggerInvoiceEligibility', 'Trigger Invoice Eligibility', 'Checks and updates invoice eligibility flag when workflow transition occurs', 'Order', 'TriggerInvoiceEligibilitySideEffectExecutor', '{"requireDocket":true,"requirePhotos":true,"requireSerials":true}', true, 5, NOW(), NOW(), false)
        ON CONFLICT ("CompanyId", "Key", "EntityType") WHERE "IsDeleted" = false DO NOTHING;
        
        RAISE NOTICE 'Seeded SideEffectDefinitions (5 records)';
    END IF;
    
    -- ============================================
    -- 4. GlobalSettings
    -- ============================================
    INSERT INTO "GlobalSettings" (
        "Id", "Key", "Value", "ValueType", "Description", "Module", "CreatedAt", "UpdatedAt"
    ) VALUES
        -- SMS Settings
        (gen_random_uuid(), 'SMS_Enabled', 'false', 'Bool', 'Enable SMS notifications', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'SMS_Provider', 'None', 'String', 'SMS provider (Twilio, SMS_Gateway, None)', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'SMS_Twilio_AccountSid', '', 'String', 'Twilio Account SID (encrypted)', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'SMS_Twilio_AuthToken', '', 'String', 'Twilio Auth Token (encrypted)', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'SMS_Twilio_FromNumber', '', 'String', 'Twilio From Phone Number', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'SMS_AutoSendOnStatusChange', 'false', 'Bool', 'Automatically send SMS when order status changes', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'SMS_RetryAttempts', '3', 'Int', 'Number of retry attempts for failed SMS', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'SMS_RetryDelaySeconds', '5', 'Int', 'Delay between SMS retry attempts (seconds)', 'Notifications', NOW(), NOW()),
        
        -- WhatsApp Settings
        (gen_random_uuid(), 'WhatsApp_Enabled', 'false', 'Bool', 'Enable WhatsApp notifications', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'WhatsApp_Provider', 'None', 'String', 'WhatsApp provider (Twilio, None)', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'WhatsApp_Twilio_AccountSid', '', 'String', 'Twilio Account SID for WhatsApp (encrypted)', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'WhatsApp_Twilio_AuthToken', '', 'String', 'Twilio Auth Token for WhatsApp (encrypted)', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'WhatsApp_Twilio_FromNumber', '', 'String', 'Twilio WhatsApp From Number', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'WhatsApp_AutoSendOnStatusChange', 'false', 'Bool', 'Automatically send WhatsApp when order status changes', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'WhatsApp_RetryAttempts', '3', 'Int', 'Number of retry attempts for failed WhatsApp', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'WhatsApp_RetryDelaySeconds', '5', 'Int', 'Delay between WhatsApp retry attempts (seconds)', 'Notifications', NOW(), NOW()),
        
        -- MyInvois E-Invoice Settings
        (gen_random_uuid(), 'EInvoice_Enabled', 'false', 'Bool', 'Enable e-invoice submission (MyInvois)', 'Billing', NOW(), NOW()),
        (gen_random_uuid(), 'EInvoice_Provider', 'Null', 'String', 'E-invoice provider (MyInvois, Null)', 'Billing', NOW(), NOW()),
        (gen_random_uuid(), 'MyInvois_BaseUrl', 'https://api-sandbox.myinvois.hasil.gov.my', 'String', 'MyInvois API base URL', 'Billing', NOW(), NOW()),
        (gen_random_uuid(), 'MyInvois_ClientId', '', 'String', 'MyInvois Client ID (encrypted)', 'Billing', NOW(), NOW()),
        (gen_random_uuid(), 'MyInvois_ClientSecret', '', 'String', 'MyInvois Client Secret (encrypted)', 'Billing', NOW(), NOW()),
        (gen_random_uuid(), 'MyInvois_Enabled', 'false', 'Bool', 'Enable MyInvois integration', 'Billing', NOW(), NOW()),
        
        -- Template Mapping Settings
        (gen_random_uuid(), 'Notification_Assigned_SmsTemplateCode', 'ASSIGNED', 'String', 'SMS template code for Assigned status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_Assigned_WhatsAppTemplateCode', 'ASSIGNED', 'String', 'WhatsApp template code for Assigned status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_OnTheWay_SmsTemplateCode', 'OTW', 'String', 'SMS template code for OnTheWay status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_OnTheWay_WhatsAppTemplateCode', 'OTW', 'String', 'WhatsApp template code for OnTheWay status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_MetCustomer_SmsTemplateCode', 'MET_CUSTOMER', 'String', 'SMS template code for MetCustomer status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_MetCustomer_WhatsAppTemplateCode', 'MET_CUSTOMER', 'String', 'WhatsApp template code for MetCustomer status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_OrderCompleted_SmsTemplateCode', 'IN_PROGRESS', 'String', 'SMS template code for OrderCompleted status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_OrderCompleted_WhatsAppTemplateCode', 'IN_PROGRESS', 'String', 'WhatsApp template code for OrderCompleted status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_Completed_SmsTemplateCode', 'COMPLETED', 'String', 'SMS template code for Completed status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_Completed_WhatsAppTemplateCode', 'COMPLETED', 'String', 'WhatsApp template code for Completed status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_Cancelled_SmsTemplateCode', 'CANCELLED', 'String', 'SMS template code for Cancelled status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_Cancelled_WhatsAppTemplateCode', 'CANCELLED', 'String', 'WhatsApp template code for Cancelled status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_ReschedulePendingApproval_SmsTemplateCode', 'RESCHEDULED', 'String', 'SMS template code for ReschedulePendingApproval status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_ReschedulePendingApproval_WhatsAppTemplateCode', 'RESCHEDULED', 'String', 'WhatsApp template code for ReschedulePendingApproval status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_Blocker_SmsTemplateCode', 'BLOCKER', 'String', 'SMS template code for Blocker status', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Notification_Blocker_WhatsAppTemplateCode', 'BLOCKER', 'String', 'WhatsApp template code for Blocker status', 'Notifications', NOW(), NOW()),
        
        -- Unified Messaging Routing Settings
        (gen_random_uuid(), 'Messaging_SendSmsFallback', 'true', 'Bool', 'Send SMS alongside WhatsApp for non-urgent messages (optional fallback)', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Messaging_AutoDetectWhatsApp', 'true', 'Bool', 'Automatically detect if customer uses WhatsApp by attempting to send', 'Notifications', NOW(), NOW()),
        (gen_random_uuid(), 'Messaging_WhatsAppRetryOnFailure', 'true', 'Bool', 'Retry with SMS if WhatsApp fails', 'Notifications', NOW(), NOW())
    ON CONFLICT ("Key") DO NOTHING;
    
    RAISE NOTICE 'Seeded GlobalSettings (~30+ records)';
END $$;

-- ============================================
-- Verification
-- ============================================
DO $$
DECLARE
    v_parser_templates_count INT;
    v_guard_conditions_count INT;
    v_side_effects_count INT;
    v_global_settings_count INT;
BEGIN
    SELECT COUNT(*) INTO v_parser_templates_count FROM "ParserTemplates";
    SELECT COUNT(*) INTO v_guard_conditions_count FROM "GuardConditionDefinitions";
    SELECT COUNT(*) INTO v_side_effects_count FROM "SideEffectDefinitions";
    SELECT COUNT(*) INTO v_global_settings_count FROM "GlobalSettings";
    
    RAISE NOTICE '========================================';
    RAISE NOTICE 'Configuration Data Seeding Complete';
    RAISE NOTICE '========================================';
    RAISE NOTICE 'ParserTemplates: %', v_parser_templates_count;
    RAISE NOTICE 'GuardConditionDefinitions: %', v_guard_conditions_count;
    RAISE NOTICE 'SideEffectDefinitions: %', v_side_effects_count;
    RAISE NOTICE 'GlobalSettings: %', v_global_settings_count;
    RAISE NOTICE '========================================';
END $$;

