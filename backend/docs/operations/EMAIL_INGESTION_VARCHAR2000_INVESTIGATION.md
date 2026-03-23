# Email Ingestion "value too long for type character varying(2000)" Investigation

## Summary

The error occurs in `EmailIngestionService` when persisting an `EmailMessage` (around `SaveChangesAsync` after adding the entity). The failure is caused by one or more columns on table `EmailMessages` that are defined as `character varying(2000)` while the application stores full email payloads or long text.

## 1. Columns that store email body/content

| Column       | Purpose                                      | Used in ingestion |
|-------------|-----------------------------------------------|-------------------|
| **BodyText**  | Full plain-text body                          | Yes – set to full `textBody` |
| **BodyHtml**  | Full HTML body                                | Yes – set to full `htmlBody` |
| **BodyPreview** | Short preview (intended ≤500 chars)        | Yes – set to first 500 chars of body |
| **ParserError** | Parser failure message (exception/stack)   | Set when parsing fails |

## 2. Current vs intended types

| Column        | Table         | Current type (model/DB intent)     | Fails when                    |
|---------------|---------------|-----------------------------------|-------------------------------|
| **BodyText**  | EmailMessages | `text` in EF config and migration | If DB still has `varchar(2000)` (e.g. migration not applied) |
| **BodyHtml**  | EmailMessages | `text` in EF config and migration | Same as above                 |
| **BodyPreview** | EmailMessages | `character varying(2000)`       | If any code path ever sets >2000 chars |
| **ParserError** | EmailMessages | `character varying(2000)`       | Long exception messages/stacks |

- **BodyText** and **BodyHtml**: EF configuration and migration `20251216120000_AddFullEmailBodyToEmailMessages` define them as PostgreSQL `text`. If the database still has `character varying(2000)` (e.g. migration not applied or different apply path), inserts of long bodies will produce: *"value too long for type character varying(2000)"*.
- **BodyPreview**: Application caps at 500 characters in `EmailIngestionService`; DB allows 2000. Risk is low but schema can be aligned to `text` for consistency.
- **ParserError**: Can receive long exception messages or stack traces; 2000 chars is a real limit and can cause the same error when parsing fails.

## 3. Do these fields store full email payloads?

- **BodyText** and **BodyHtml**: **Yes.** They are set to the full `textBody` and `htmlBody` in `EmailIngestionService` (no truncation). Full payloads routinely exceed 2000 characters.
- **BodyPreview**: No; intentionally capped at 500 characters in ingestion.
- **ParserError**: No; stores error details, but content can exceed 2000 characters.

## 4. Recommendations (schema change, no data loss)

- **Prefer schema change over truncation:** Do not truncate email content or parser errors in application code; fix the schema so all relevant columns can store long text.
- **BodyText / BodyHtml:** Should be PostgreSQL `text`. Already defined as `text` in EF and in migration `AddFullEmailBodyToEmailMessages`. Any database where they are still `varchar(2000)` must be updated (apply the existing migration or run a corrective migration that alters these columns to `text`).
- **BodyPreview / ParserError:** Recommend changing from `character varying(2000)` to `text` so that:
  - No code path can hit the 2000 limit.
  - ParserError can hold long exception/stack text without truncation.

## 5. Column summary (deliverable format)

| Column        | Table         | Current type              | Recommended type | Migration impact |
|---------------|---------------|----------------------------|------------------|-------------------|
| BodyText      | EmailMessages | `text` (intended); may be `varchar(2000)` in some DBs | `text` | Ensure column type is `text` (idempotent alter). |
| BodyHtml      | EmailMessages | `text` (intended); may be `varchar(2000)` in some DBs | `text` | Same as above. |
| BodyPreview   | EmailMessages | `character varying(2000)`  | `text`           | One-time alter; no data loss; allows future >2000 if needed. |
| ParserError   | EmailMessages | `character varying(2000)`  | `text`           | One-time alter; prevents truncation of long errors. |

## 6. EF migration

An EF migration **EnsureEmailMessageBodyAndErrorColumnsAreText** (`20260313025530_EnsureEmailMessageBodyAndErrorColumnsAreText.cs`) is provided to:

1. Alter **BodyText** and **BodyHtml** to `text` (idempotent where already `text`).
2. Alter **BodyPreview** and **ParserError** to `text`.

PostgreSQL allows changing `character varying(n)` to `text` without data loss. No business logic or silent truncation changes are required.

## 7. Verification (optional)

To confirm actual column types in a database:

```sql
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = 'EmailMessages'
  AND column_name IN ('BodyText', 'BodyHtml', 'BodyPreview', 'ParserError')
ORDER BY column_name;
```

If `data_type` is `character varying` and `character_maximum_length` is 2000 for any of these, the migration should be applied (or the corrective migration run) so they become `text`.
