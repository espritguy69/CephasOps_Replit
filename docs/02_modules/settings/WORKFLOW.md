# Settings – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Settings module, covering global settings, module-specific settings, and configuration management

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         SETTINGS MODULE SYSTEM                           │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   GLOBAL SETTINGS      │      │   MODULE SETTINGS     │
        │  (System-wide Config)   │      │  (Per-Module Config)   │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Key-Value Pairs      │      │ • Material Templates   │
        │ • Module Grouping      │      │ • KPI Profiles         │
        │ • Type Support         │      │ • Rate Cards           │
        │ • Value Resolution     │      │ • Workflow Definitions │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   SETTINGS RESOLUTION   │      │   SETTINGS MANAGEMENT │
        │  (Fallback Chain)       │      │  (CRUD Operations)    │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Workflow: Settings Creation to Resolution

```
[STEP 1: CREATE GLOBAL SETTING]
         |
         v
┌────────────────────────────────────────┐
│ CREATE GLOBAL SETTING                     │
│ POST /api/global-settings                │
└────────────────────────────────────────┘
         |
         v
CreateGlobalSettingDto {
  Key: "DefaultCostCenterCode"
  Value: "ISP_OPS"
  ValueType: "String"
  Description: "Default cost center code for material allocation"
  Module: "PNL"
}
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE SETTING                          │
│ GlobalSettingsService.CreateAsync()      │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Return Validation Errors]
   |
   v
Checks:
  ✓ Key not empty
  ✓ Key unique (no existing setting with same key)
  ✓ ValueType valid (String, Int, Decimal, Bool, Json)
  ✓ Value matches ValueType
         |
         v
┌────────────────────────────────────────┐
│ CREATE SETTING RECORD                     │
└────────────────────────────────────────┘
         |
         v
GlobalSetting {
  Id: Guid.NewGuid()
  Key: "DefaultCostCenterCode"
  Value: "ISP_OPS"
  ValueType: "String"
  Description: "Default cost center code for material allocation"
  Module: "PNL"
  CreatedAt: DateTime.UtcNow
  CreatedByUserId: "user-123"
  UpdatedAt: DateTime.UtcNow
  UpdatedByUserId: "user-123"
}
         |
         v
[Save to Database]
  _context.GlobalSettings.Add(setting)
  await _context.SaveChangesAsync()
         |
         v
[STEP 2: UPDATE SETTING]
         |
         v
┌────────────────────────────────────────┐
│ UPDATE GLOBAL SETTING                     │
│ PUT /api/global-settings/{key}           │
└────────────────────────────────────────┘
         |
         v
UpdateGlobalSettingDto {
  Value: "WAREHOUSE"
  Description: "Updated default cost center"
}
         |
         v
[Get Existing Setting]
  GlobalSetting.find(Key = "DefaultCostCenterCode")
         |
         v
[Update Setting]
  setting.Value = "WAREHOUSE"
  setting.Description = "Updated default cost center"
  setting.UpdatedAt = DateTime.UtcNow
  setting.UpdatedByUserId = "user-123"
         |
         v
[Save Changes]
  await _context.SaveChangesAsync()
         |
         v
[STEP 3: RESOLVE SETTING VALUE]
         |
         v
[Service Needs Setting Value]
  PnlService needs: DefaultCostCenterCode
         |
         v
┌────────────────────────────────────────┐
│ GET SETTING VALUE                         │
│ GlobalSettingsService.GetValueAsync<T>() │
└────────────────────────────────────────┘
         |
         v
[Query Setting]
  GlobalSetting.find(Key = "DefaultCostCenterCode")
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Return default(T)]
   |
   v
┌────────────────────────────────────────┐
│ PARSE VALUE BY TYPE                       │
└────────────────────────────────────────┘
         |
         v
[Parse Based on ValueType]
  ValueType = "String"
    → Return Value as string
  
  ValueType = "Int"
    → Parse Value as int
  
  ValueType = "Decimal"
    → Parse Value as decimal
  
  ValueType = "Bool"
    → Parse Value as bool
  
  ValueType = "Json"
    → Deserialize Value as T
         |
         v
[Return Parsed Value]
  return "WAREHOUSE" (as string)
```

---

## Settings Resolution Chain

```
[Service Requests Setting]
  GetValueAsync<T>("DefaultCostCenterCode")
         |
         v
┌────────────────────────────────────────┐
│ RESOLUTION CHAIN                          │
└────────────────────────────────────────┘
         |
         v
[Step 1: Check Global Settings]
  GlobalSetting.find(Key = "DefaultCostCenterCode")
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Step 2: Check Module-Specific Settings]
   |           MaterialTemplateService.GetSetting()
   |               |
   |       ┌───────┴───────┐
   |       |               |
   |       v               v
   |   [FOUND]         [NOT FOUND]
   |       |               |
   |       |               v
   |       |       [Step 3: Use Hardcoded Default]
   |       |           return "DEFAULT"
   |
   v
[Return Value]
  return setting.Value
```

---

## Module-Specific Settings Workflow

```
[Module: Material Templates]
         |
         v
┌────────────────────────────────────────┐
│ CREATE MATERIAL TEMPLATE                 │
│ POST /api/material-templates             │
└────────────────────────────────────────┘
         |
         v
CreateMaterialTemplateDto {
  Name: "TIME Prelaid High-Rise Kit"
  OrderType: "ACTIVATION"
  BuildingTypeId: HighRise
  PartnerId: TIME
  IsDefault: true
  Items: [
    { MaterialId: "ONU-HG8240H", Quantity: 1 },
    { MaterialId: "PATCHCORD-6M", Quantity: 2 }
  ]
}
         |
         v
[Create Template]
  MaterialTemplate {
    Id: Guid.NewGuid()
    CompanyId: Cephas
    Name: "TIME Prelaid High-Rise Kit"
    OrderType: "ACTIVATION"
    BuildingTypeId: HighRise
    PartnerId: TIME
    IsDefault: true
  }
         |
         v
[Create Template Items]
  For each item:
    MaterialTemplateItem {
      MaterialTemplateId: template.Id
      MaterialId: item.MaterialId
      Quantity: item.Quantity
    }
         |
         v
[Save Template]
  _context.MaterialTemplates.Add(template)
  await _context.SaveChangesAsync()
         |
         v
[Template Available for Order Creation]
```

---

## Entities Involved

### GlobalSetting Entity
```
GlobalSetting
├── Id (Guid)
├── Key (string, unique)
├── Value (string)
├── ValueType (string: String, Int, Decimal, Bool, Json)
├── Description (string?)
├── Module (string?)
├── CreatedAt (DateTime)
├── CreatedByUserId (Guid)
├── UpdatedAt (DateTime)
└── UpdatedByUserId (Guid)
```

---

## API Endpoints Involved

### Global Settings
- `GET /api/global-settings` - List all settings (optional: `?module=PNL`)
- `GET /api/global-settings/{key}` - Get setting by key
- `POST /api/global-settings` - Create setting
- `PUT /api/global-settings/{key}` - Update setting
- `DELETE /api/global-settings/{key}` - Delete setting

### Module-Specific Settings
- Various endpoints per module (Material Templates, KPI Profiles, Rate Cards, etc.)
- Each module has its own settings management endpoints

---

## Module Rules & Validations

### Global Settings Rules
- Key must be unique across all settings
- ValueType must be valid (String, Int, Decimal, Bool, Json)
- Value must be parseable according to ValueType
- Module grouping is optional but recommended

### Settings Resolution Rules
- Global settings are system-wide
- Module-specific settings take precedence (if applicable)
- Fallback to hardcoded defaults if setting not found
- Settings are cached for performance (optional)

### Value Type Rules
- String: Stored as-is
- Int: Must be parseable as integer
- Decimal: Must be parseable as decimal
- Bool: Must be "true" or "false"
- Json: Must be valid JSON, deserialized to target type

---

## Integration Points

### All Modules
- All modules can read global settings
- Settings provide configuration without code changes
- Settings changes take effect immediately (no restart needed)

### PNL Module
- Uses DefaultCostCenterCode setting
- Uses CostCenterStrictMode setting

### Billing Module
- Uses invoice numbering settings
- Uses tax calculation settings

### Workflow Module
- Workflow definitions are settings
- Guard condition definitions are settings
- Side effect definitions are settings

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/global_settings/GLOBAL_SETTINGS_MODULE.md` - Global settings specification
- `docs/02_modules/global_settings/SETTINGS_MODULE.md` - Settings module overview

