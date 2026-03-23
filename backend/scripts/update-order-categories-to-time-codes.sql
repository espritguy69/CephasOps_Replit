-- Update Order Category codes/names to TIME-FTTH, TIME-FTTR, TIME-FTTC (per Order hierarchy spec).
-- Run on existing DBs that have FTTH/FTTR/FTTC. Preserves Ids so existing orders/rates are unchanged.

UPDATE "OrderCategories" SET "Code" = 'TIME-FTTH', "Name" = 'TIME-FTTH', "UpdatedAt" = NOW() WHERE "Code" = 'FTTH';
UPDATE "OrderCategories" SET "Code" = 'TIME-FTTR', "Name" = 'TIME-FTTR', "UpdatedAt" = NOW() WHERE "Code" = 'FTTR';
UPDATE "OrderCategories" SET "Code" = 'TIME-FTTC', "Name" = 'TIME-FTTC', "Description" = 'Fibre to the Charge', "UpdatedAt" = NOW() WHERE "Code" = 'FTTC';
-- Ensure TIME-FTTC description is correct (for already-migrated or newly migrated rows)
UPDATE "OrderCategories" SET "Description" = 'Fibre to the Charge', "UpdatedAt" = NOW() WHERE "Code" = 'TIME-FTTC';

-- Optional: uncomment if you use FTTO and want TIME-FTTO
-- UPDATE "OrderCategories" SET "Code" = 'TIME-FTTO', "Name" = 'TIME-FTTO', "UpdatedAt" = NOW() WHERE "Code" = 'FTTO';
