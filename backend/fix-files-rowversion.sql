-- Add RowVersion to Files table
ALTER TABLE "Files" ADD COLUMN IF NOT EXISTS "RowVersion" bytea;

