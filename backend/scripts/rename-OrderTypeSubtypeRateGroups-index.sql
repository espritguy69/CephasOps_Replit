-- Rename truncated index to short name (optional cleanup after apply-AddBaseWorkRates.sql)
ALTER INDEX IF EXISTS "IX_OrderTypeSubtypeRateGroups_CompanyId_OrderTypeId_OrderSubtyp"
RENAME TO "IX_OrderTypeSubtypeRateGroups_Company_Type_Subtype";
