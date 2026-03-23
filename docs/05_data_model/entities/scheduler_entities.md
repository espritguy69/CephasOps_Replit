\# Scheduler \& Planning Entities  

CephasOps – Scheduler Domain Data Model  

Version 1.0



Entities:



\- CalendarDayCapacity

\- SiAvailability

\- ScheduledSlot

\- SiLeaveRequest



All \*\*company-scoped\*\*.



---



\## 1. CalendarDayCapacity



Represents total capacity per day per region/team (high-level).



\### 1.1 Table: `CalendarDayCapacities`



| Field         | Type     | Required | Description                                      |

|---------------|----------|----------|--------------------------------------------------|

| id            | uuid     | yes      | Primary key.                                     |

| companyId     | uuid     | yes      | FK → Companies.id.                               |

| date          | date     | yes      | The day.                                         |

| region        | string   | no       | Optional label (e.g. `KL`, `PJ`, `Seremban`).    |

| maxJobs       | int      | yes      | Maximum number of jobs that can be scheduled.    |

| reservedJobs  | int      | yes      | Jobs already reserved/booked.                    |

| createdAt     | datetime | yes      | Created timestamp.                               |

| updatedAt     | datetime | yes      | Last update.                                     |



---



\## 2. SiAvailability



Daily availability per SI (normalised view of shifts / working days).



\### 2.1 Table: `SiAvailabilities`



| Field            | Type     | Required | Description                                          |

|------------------|----------|----------|------------------------------------------------------|

| id               | uuid     | yes      | Primary key.                                         |

| companyId        | uuid     | yes      | FK → Companies.id.                                   |

| serviceInstallerId| uuid    | yes      | FK → ServiceInstallers.id.                           |

| date             | date     | yes      | For which day.                                       |

| isWorkingDay     | boolean  | yes      | True if SI is expected to work.                      |

| workingFrom      | time     | no       | Planned start time.                                  |

| workingTo        | time     | no       | Planned end time.                                    |

| maxJobs          | int      | yes      | Max jobs SI should handle that day.                  |

| currentJobsCount | int      | yes      | How many jobs currently assigned.                    |

| notes            | string   | no       | Special constraints (training, half-day, etc.).      |

| createdAt        | datetime | yes      | Created timestamp.                                   |

| updatedAt        | datetime | yes      | Last update.                                         |



---



\## 3. ScheduledSlot



Binding between an `Order` and a time slot for a specific SI/team.



\### 3.1 Table: `ScheduledSlots`



| Field            | Type     | Required | Description                                          |

|------------------|----------|----------|------------------------------------------------------|

| id               | uuid     | yes      | Primary key.                                         |

| companyId        | uuid     | yes      | FK → Companies.id.                                   |

| orderId          | uuid     | yes      | FK → Orders.id.                                      |

| serviceInstallerId| uuid    | yes      | FK → ServiceInstallers.id.                           |

| date             | date     | yes      | Appointment date.                                    |

| windowFrom       | time     | yes      | Start of window.                                     |

| windowTo         | time     | yes      | End of window.                                       |

| plannedTravelMin | int      | no       | Estimated travel time before this job.               |

| sequenceIndex    | int      | yes      | Job order for the SI on that day (1,2,3...).         |

| status           | enum     | yes      | `Planned`, `InProgress`, `Completed`, `Cancelled`.   |

| createdByUserId  | uuid     | yes      | Who scheduled.                                       |

| createdAt        | datetime | yes      | Created timestamp.                                   |

| updatedAt        | datetime | yes      | Last update.                                         |



> This table is effectively the “calendar” for SIs.



---



\## 4. SiLeaveRequest



Tracks leave/unavailability for SIs.



\### 4.1 Table: `SiLeaveRequests`



| Field            | Type     | Required | Description                              |

|------------------|----------|----------|------------------------------------------|

| id               | uuid     | yes      | Primary key.                             |

| companyId        | uuid     | yes      | FK → Companies.id.                       |

| serviceInstallerId| uuid    | yes      | FK → ServiceInstallers.id.               |

| dateFrom         | date     | yes      | Leave start date.                        |

| dateTo           | date     | yes      | Leave end date.                          |

| reason           | string   | yes      | Sick, holiday, personal, etc.            |

| status           | enum     | yes      | `Pending`, `Approved`, `Rejected`.       |

| approvedByUserId | uuid     | no       | Who approved.                            |

| createdAt        | datetime | yes      | Created timestamp.                       |

| updatedAt        | datetime | yes      | Last update.                             |



---



\## 5. Cross-Module Links



\- `ScheduledSlots.orderId` → Orders  

\- `ScheduledSlots.serviceInstallerId` → ServiceInstallers  

\- `SiAvailabilities` \& `SiLeaveRequests` used as constraints when generating schedules  



---



\# End of Scheduler Entities



