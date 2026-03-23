# Files – System Workflow Diagram

**Date:** December 12, 2025  
**Purpose:** End-to-end workflow representation for the Files module, covering file upload, storage, download, and OneDrive synchronization

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         FILES MODULE SYSTEM                              │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   FILE UPLOAD          │      │   FILE STORAGE         │
        │  (Upload, Validation) │      │  (Local, OneDrive)     │
        ├───────────────────────┤      ├───────────────────────┤
        │ • Upload File          │      │ • Local Storage         │
        │ • Validate File         │      │ • OneDrive Sync        │
        │ • Generate Path         │      │ • Checksum Verify      │
        │ • Calculate Checksum    │      │ • File Metadata        │
        └───────────────────────┘      └───────────────────────┘
                    │                               │
                    └───────────────┬───────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    │                               │
                    ▼                               ▼
        ┌───────────────────────┐      ┌───────────────────────┐
        │   FILE DOWNLOAD        │      │   FILE MANAGEMENT      │
        │  (Retrieve, Stream)    │      │  (Delete, Metadata)   │
        └───────────────────────┘      └───────────────────────┘
```

---

## Complete Workflow: File Upload to Download

```
[STEP 1: FILE UPLOAD REQUEST]
         |
         v
┌────────────────────────────────────────┐
│ UPLOAD FILE                               │
│ POST /api/files/upload                    │
└────────────────────────────────────────┘
         |
         v
[Multipart Form Data]
  File: [binary data]
  Module: "Orders"
  EntityId: "order-456"
  EntityType: "Order"
         |
         v
┌────────────────────────────────────────┐
│ VALIDATE FILE                            │
│ FileService.UploadFileAsync()            │
└────────────────────────────────────────┘
         |
    ┌────┴────┐
    |         |
    v         v
[VALID] [INVALID]
   |         |
   |         v
   |    [Return 400 Bad Request]
   |    "File is required and cannot be empty"
   |
   v
Checks:
  ✓ File not null
  ✓ File length > 0
  ✓ File size within limits (if configured)
  ✓ File type allowed (if configured)
         |
         v
[STEP 2: GENERATE STORAGE PATH]
         |
         v
┌────────────────────────────────────────┐
│ BUILD FILE PATH                           │
└────────────────────────────────────────┘
         |
         v
[Generate Path Components]
  companyId: Cephas
  module: "Orders"
  year: 2025
  month: 12
  fileId: Guid.NewGuid()
  extension: ".pdf"
         |
         v
Storage Path:
  "files/{companyId}/{module}/{year}/{month}/{fileId}.{ext}"
  Example: "files/cephas/Orders/2025/12/abc-123-def.pdf"
         |
         v
[STEP 3: SAVE FILE TO DISK]
         |
         v
┌────────────────────────────────────────┐
│ CREATE DIRECTORY STRUCTURE                │
└────────────────────────────────────────┘
         |
         v
Base Path: AppDomain.CurrentDomain.BaseDirectory
Full Path: basePath + "uploads" + storagePath
         |
         v
[Ensure Directory Exists]
  Directory.CreateDirectory(directory)
         |
         v
┌────────────────────────────────────────┐
│ WRITE FILE TO DISK                        │
└────────────────────────────────────────┘
         |
         v
[Save File]
  using (var fileStream = new FileStream(fullPath, FileMode.Create))
  {
    await file.CopyToAsync(fileStream)
  }
         |
         v
[STEP 4: CALCULATE CHECKSUM]
         |
         v
┌────────────────────────────────────────┐
│ CALCULATE FILE CHECKSUM                   │
│ FileService.CalculateChecksumAsync()      │
└────────────────────────────────────────┘
         |
         v
[Read File Bytes]
  fileBytes = await File.ReadAllBytesAsync(fullPath)
         |
         v
[Calculate Hash]
  checksum = SHA256(fileBytes)
         |
         v
[STEP 5: CREATE FILE RECORD]
         |
         v
┌────────────────────────────────────────┐
│ CREATE FILE ENTITY                         │
└────────────────────────────────────────┘
         |
         v
File {
  Id: fileId
  CompanyId: Cephas
  FileName: "invoice_12345.pdf"
  StoragePath: "files/cephas/Orders/2025/12/abc-123-def.pdf"
  ContentType: "application/pdf"
  SizeBytes: 245760
  Checksum: "a1b2c3d4e5f6..."
  Module: "Orders"
  EntityId: "order-456"
  EntityType: "Order"
  CreatedById: "user-123"
  CreatedAt: 2025-12-12 10:00:00
}
         |
         v
[Save to Database]
  _context.Set<File>().Add(file)
  await _context.SaveChangesAsync()
         |
         v
[STEP 6: ONEDRIVE SYNC (Background)]
         |
         v
┌────────────────────────────────────────┐
│ TRIGGER ONEDRIVE SYNC                     │
│ OneDriveSyncService.SyncFileAsync()        │
└────────────────────────────────────────┘
         |
         v
[Fire and Forget Task]
  Task.Run(async () => {
    await _oneDriveSyncService.SyncFileAsync(
      fileId,
      fullPath,
      fileName,
      module
    )
  })
         |
         v
[OneDrive Upload]
  - Upload file to OneDrive
  - Store OneDrive file ID
  - Update File entity with OneDriveFileId
         |
         v
[STEP 7: RETURN FILE DTO]
         |
         v
┌────────────────────────────────────────┐
│ FILE UPLOAD RESPONSE                      │
└────────────────────────────────────────┘
         |
         v
FileDto {
  Id: fileId
  CompanyId: Cephas
  FileName: "invoice_12345.pdf"
  ContentType: "application/pdf"
  SizeBytes: 245760
  Checksum: "a1b2c3d4e5f6..."
  Module: "Orders"
  EntityId: "order-456"
  EntityType: "Order"
  CreatedById: "user-123"
  CreatedAt: 2025-12-12 10:00:00
}
         |
         v
[STEP 8: FILE DOWNLOAD]
         |
         v
[Client Requests File]
  GET /api/files/{fileId}/download
         |
         v
┌────────────────────────────────────────┐
│ GET FILE METADATA                         │
│ FileService.DownloadFileAsync()          │
└────────────────────────────────────────┘
         |
         v
[Query File]
  File.find(
    Id = fileId
    CompanyId = Cephas
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Return 404 Not Found]
   |
   v
[Check File Exists on Disk]
  System.IO.File.Exists(fullPath)
         |
    ┌────┴────┐
    |         |
    v         v
[EXISTS] [NOT EXISTS]
   |            |
   |            v
   |       [Return 404 Not Found]
   |       "File not found on disk"
   |
   v
┌────────────────────────────────────────┐
│ OPEN FILE STREAM                          │
└────────────────────────────────────────┘
         |
         v
[Create File Stream]
  fileStream = new FileStream(
    fullPath,
    FileMode.Open,
    FileAccess.Read,
    FileShare.Read
  )
         |
         v
[Return File Stream]
  return (
    FileStream: fileStream,
    FileName: file.FileName,
    ContentType: file.ContentType
  )
         |
         v
[Client Downloads File]
```

---

## OneDrive Synchronization Workflow

```
[File Uploaded to Local Storage]
  File {
    Id: fileId
    StoragePath: "files/cephas/Orders/2025/12/abc-123-def.pdf"
  }
         |
         v
┌────────────────────────────────────────┐
│ ONEDRIVE SYNC TRIGGERED                   │
│ (Background Task)                         │
└────────────────────────────────────────┘
         |
         v
[Check OneDrive Configuration]
  OneDriveSettings {
    IsEnabled: true
    ClientId: "..."
    ClientSecret: "..."
    TenantId: "..."
  }
         |
    ┌────┴────┐
    |         |
    v         v
[ENABLED] [DISABLED]
   |            |
   |            v
   |       [Skip Sync]
   |
   v
┌────────────────────────────────────────┐
│ AUTHENTICATE WITH ONEDRIVE                │
│ OneDriveSyncService.Authenticate()        │
└────────────────────────────────────────┘
         |
         v
[Get Access Token]
  OAuth2 Token Request
  → Access Token
         |
         v
┌────────────────────────────────────────┐
│ UPLOAD FILE TO ONEDRIVE                   │
│ OneDriveSyncService.UploadFile()          │
└────────────────────────────────────────┘
         |
         v
[Read File from Local Storage]
  fileBytes = await File.ReadAllBytesAsync(localPath)
         |
         v
[Upload to OneDrive]
  POST https://graph.microsoft.com/v1.0/me/drive/root:/{path}:/content
  Headers: {
    Authorization: "Bearer {accessToken}"
    Content-Type: file.ContentType
  }
  Body: fileBytes
         |
         v
[OneDrive Response]
  {
    id: "onedrive-file-id-123"
    name: "invoice_12345.pdf"
    webUrl: "https://..."
  }
         |
         v
┌────────────────────────────────────────┐
│ UPDATE FILE ENTITY                         │
└────────────────────────────────────────┘
         |
         v
[Update File Record]
  File {
    OneDriveFileId: "onedrive-file-id-123"
    OneDriveWebUrl: "https://..."
    OneDriveSyncedAt: DateTime.UtcNow
  }
         |
         v
[Save Changes]
  await _context.SaveChangesAsync()
         |
         v
[Sync Complete]
```

---

## File Deletion Workflow

```
[Delete File Request]
  DELETE /api/files/{fileId}
         |
         v
┌────────────────────────────────────────┐
│ GET FILE RECORD                           │
│ FileService.DeleteFileAsync()            │
└────────────────────────────────────────┘
         |
         v
[Query File]
  File.find(
    Id = fileId
    CompanyId = Cephas
  )
         |
    ┌────┴────┐
    |         |
    v         v
[FOUND] [NOT FOUND]
   |            |
   |            v
   |       [Return 404 Not Found]
   |
   v
┌────────────────────────────────────────┐
│ DELETE PHYSICAL FILE                       │
└────────────────────────────────────────┘
         |
         v
[Check File Exists]
  System.IO.File.Exists(fullPath)
         |
         v
[Delete File]
  System.IO.File.Delete(fullPath)
         |
         v
┌────────────────────────────────────────┐
│ DELETE FROM ONEDRIVE (if synced)          │
└────────────────────────────────────────┘
         |
         v
[If OneDriveFileId exists]
  DELETE https://graph.microsoft.com/v1.0/me/drive/items/{oneDriveFileId}
         |
         v
┌────────────────────────────────────────┐
│ DELETE DATABASE RECORD                    │
└────────────────────────────────────────┘
         |
         v
[Remove File Entity]
  _context.Set<File>().Remove(file)
  await _context.SaveChangesAsync()
         |
         v
[Deletion Complete]
```

---

## File Metadata Retrieval

```
[Get File Metadata]
  GET /api/files/{fileId}
         |
         v
┌────────────────────────────────────────┐
│ GET FILE METADATA                         │
│ FileService.GetFileMetadataAsync()       │
└────────────────────────────────────────┘
         |
         v
[Query File]
  File.find(
    Id = fileId
    CompanyId = Cephas
  )
         |
         v
[Return File DTO]
  FileDto {
    Id: fileId
    FileName: "invoice_12345.pdf"
    ContentType: "application/pdf"
    SizeBytes: 245760
    Checksum: "a1b2c3d4e5f6..."
    Module: "Orders"
    EntityId: "order-456"
    EntityType: "Order"
    CreatedAt: 2025-12-12 10:00:00
    OneDriveFileId: "onedrive-file-id-123" (if synced)
    OneDriveWebUrl: "https://..." (if synced)
  }
```

---

## Entities Involved

### File Entity
```
File
├── Id (Guid)
├── CompanyId (Guid)
├── FileName (string)
├── StoragePath (string)
├── ContentType (string)
├── SizeBytes (long)
├── Checksum (string?)
├── Module (string?)
├── EntityId (Guid?)
├── EntityType (string?)
├── OneDriveFileId (string?)
├── OneDriveWebUrl (string?)
├── OneDriveSyncedAt (DateTime?)
├── CreatedById (Guid)
└── CreatedAt (DateTime)
```

---

## API Endpoints Involved

### File Management
- `POST /api/files/upload` - Upload file
  - Request: `MultipartFormData { File, Module?, EntityId?, EntityType? }`
  - Response: `FileDto`

- `GET /api/files/{id}/download` - Download file
  - Response: `FileStream` with `Content-Type` header

- `GET /api/files/{id}` - Get file metadata
  - Response: `FileDto`

- `DELETE /api/files/{id}` - Delete file
  - Response: 204 No Content

---

## Module Rules & Validations

### Upload Rules
- File is required and cannot be empty
- File size limits (if configured)
- Allowed file types (if configured)
- Storage path generated automatically
- Checksum calculated for integrity

### Storage Rules
- Files stored in: `uploads/files/{companyId}/{module}/{year}/{month}/{fileId}.{ext}`
- Directory structure created automatically
- File names sanitized to prevent path traversal
- Company isolation enforced

### OneDrive Sync Rules
- Sync runs in background (fire and forget)
- Sync errors logged but don't fail upload
- OneDrive credentials from configuration
- Sync status tracked in File entity

### Access Rules
- Files scoped by CompanyId
- Only file owner or Admin can delete
- File download requires authentication
- Entity association optional (for linking to orders, invoices, etc.)

---

## Integration Points

### Orders Module
- Files attached to orders (photos, dockets, evidence)
- File metadata stored with order
- File deletion when order deleted (optional)

### Billing Module
- Invoice PDFs stored as files
- Docket attachments
- Payment receipts

### Email Parser Module
- Email attachments saved as files
- MRA PDFs stored
- Excel files for parsing

### Notifications Module
- File upload completion notifications (optional)
- File access notifications (optional)

---

**Last Updated:** December 12, 2025  
**Related Documents:**
- `docs/02_modules/files/` - Files module documentation

