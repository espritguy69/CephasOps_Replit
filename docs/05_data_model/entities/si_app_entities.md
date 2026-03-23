\# Service Installer App Entities  

CephasOps – SI App Data Model  

Version 1.0



These entities represent the \*\*field-side\*\* data captured through the SI mobile app:



\- SiJobSession

\- SiJobEvent

\- SiPhoto

\- SiDeviceScan

\- SiLocationPing



All are \*\*company-scoped\*\* via `companyId`.



---



\## 1. SiJobSession



Represents a SI’s interaction with a specific Order on a specific day.



\### 1.1 Table: `SiJobSessions`



| Field             | Type     | Required | Description                                       |

|-------------------|----------|----------|---------------------------------------------------|

| id                | uuid     | yes      | Primary key.                                      |

| companyId         | uuid     | yes      | FK → Companies.id.                                |

| orderId           | uuid     | yes      | FK → Orders.id.                                   |

| serviceInstallerId| uuid     | yes      | FK → ServiceInstallers.id.                        |

| appVersion        | string   | no       | SI app version string.                            |

| deviceInfo        | string   | no       | Device model info.                                |

| startedAt         | datetime | yes      | When SI started job in app (`OTW` pressed).       |

| completedAt       | datetime | no       | When SI marked job complete.                      |

| lastSyncAt        | datetime | no       | Last time this session synced to backend.         |

| status            | enum     | yes      | `InProgress`, `Completed`, `Abandoned`.           |

| createdAt         | datetime | yes      | Created timestamp.                                |



---



\## 2. SiJobEvent



Detailed timeline of what SI did in the app.



\### 2.1 Table: `SiJobEvents`



| Field             | Type     | Required | Description                                             |

|-------------------|----------|----------|---------------------------------------------------------|

| id                | uuid     | yes      | Primary key.                                            |

| companyId         | uuid     | yes      | FK → Companies.id.                                      |

| jobSessionId      | uuid     | yes      | FK → SiJobSessions.id.                                  |

| eventType         | string   | yes      | `OTW`, `Arrived`, `MetCustomer`, `Testing`, `Completed`, etc. |

| eventStatus       | string   | no       | Optional finer detail.                                  |

| notes             | text     | no       | Any remarks written by SI.                              |

| latitude          | decimal  | no       | GPS lat at event time.                                  |

| longitude         | decimal  | no       | GPS lng at event time.                                  |

| createdAt         | datetime | yes      | Time event recorded (device timestamp).                 |

| syncedAt          | datetime | no       | When event was received by backend.                     |



---



\## 3. SiPhoto



Photos captured by SI during job.



\### 3.1 Table: `SiPhotos`



| Field        | Type     | Required | Description                                 |

|--------------|----------|----------|---------------------------------------------|

| id           | uuid     | yes      | Primary key.                                |

| companyId    | uuid     | yes      | FK → Companies.id.                          |

| jobSessionId | uuid     | yes      | FK → SiJobSessions.id.                      |

| orderId      | uuid     | yes      | FK → Orders.id.                             |

| type         | string   | yes      | `Before`, `After`, `Splitter`, `Modem`, etc.|

| fileId       | uuid     | yes      | FK → Files.id (image).                      |

| latitude     | decimal  | no       | GPS lat.                                    |

| longitude    | decimal  | no       | GPS lng.                                    |

| createdAt    | datetime | yes      | Captured time (device).                     |

| syncedAt     | datetime | no       | When uploaded.                              |



> Backend uses these to enforce “minimum required photo types” for order completion.



---



\## 4. SiDeviceScan



Serial and device information scanned during the job.



\### 4.1 Table: `SiDeviceScans`



| Field             | Type     | Required | Description                                      |

|-------------------|----------|----------|--------------------------------------------------|

| id                | uuid     | yes      | Primary key.                                     |

| companyId         | uuid     | yes      | FK → Companies.id.                               |

| jobSessionId      | uuid     | yes      | FK → SiJobSessions.id.                           |

| orderId           | uuid     | yes      | FK → Orders.id.                                  |

| scanType          | string   | yes      | `ONU`, `Router`, `SplitterPort`, `Faceplate`, etc. |

| scannedValue      | string   | yes      | Barcode/QR/manual entry.                         |

| serialisedItemId  | uuid     | no       | FK → SerialisedItems.id if matched.              |

| isFinalBinding    | boolean  | yes      | True if this is the final associated device.     |

| createdAt         | datetime | yes      | Captured time.                                   |

| syncedAt          | datetime | no       | When synced.                                     |



---



\## 5. SiLocationPing



Background GPS pings (optional, for routing/field visibility).



\### 5.1 Table: `SiLocationPings`



| Field             | Type     | Required | Description                    |

|-------------------|----------|----------|--------------------------------|

| id                | uuid     | yes      | Primary key.                   |

| companyId         | uuid     | yes      | FK → Companies.id.             |

| serviceInstallerId| uuid     | yes      | FK → ServiceInstallers.id.     |

| latitude          | decimal  | yes      | GPS lat.                       |

| longitude         | decimal  | yes      | GPS lng.                       |

| recordedAt        | datetime | yes      | When captured.                 |

| source            | string   | yes      | `SIAppForeground`, `Background`.|



---



\## 6. Cross-Module Links



\- `SiJobSession.orderId` ↔ Orders  

\- `SiPhoto` \& `SiDeviceScan` feed:

&nbsp; - `Orders.photosUploaded`

&nbsp; - `Orders.serialsValidated`

&nbsp; - Inventory \& RMA flows  



---



\# End of SI App Entities



