# ✅ orders.md — Full Production Version (FINAL)

# \# ORDERS.md — CephasOps Full Production Specification

# 

# This document defines the \*\*Order data model\*\*, \*\*validation rules\*\*, and \*\*API behaviour\*\* that govern how CephasOps stores, updates, and transmits job information for TIME, Digi, Celcom, CelcomDigi, U-Mobile, and future partners.

# 

# It is the \*\*single source of truth\*\* for backend, frontend, mobile apps, and email parser integration.

# 

# ---

# 

# \# 1. Order Object – Top-Level Structure

# 

# ```json

# {

# &nbsp; "orderId": "<UUID>",

# &nbsp; "uniqueId": "<ServiceID or PartnerOrderID>",

# &nbsp; "partnerGroup": "TIME | TIMEDIGI | TIMECELCOM | TIMEUMOBILE | DIRECT",

# &nbsp; "partnerOrderType": "Activation | ModificationIndoor | ModificationOutdoor | Assurance | ValueAddedService",

# &nbsp; "createdFrom": "email | manual | api",

# &nbsp; "customer": { ... },

# &nbsp; "address": { ... },

# &nbsp; "appointment": { ... },

# &nbsp; "relocation": { ... },

# &nbsp; "assurance": { ... },

# &nbsp; "materials": { ... },

# &nbsp; "splitterUsage": { ... },

# &nbsp; "billing": { ... },

# &nbsp; "status": "Pending | Assigned | ... | Completed",

# &nbsp; "statusHistory": \[],

# &nbsp; "rescheduleHistory": \[],

# &nbsp; "attachments": \[],

# &nbsp; "workOrderUrl": "",

# &nbsp; "createdAt": "",

# &nbsp; "updatedAt": ""

# }

# 

# 2\. Identification Rules

# 2.1 Service ID Types

# 

# CephasOps supports two types of Service IDs:

# 

# **TBBN (TIME Direct Customers):**

# - Format: TBBN[A-Z]?\d+[A-Z]?

# - Examples: TBBN1234567, TBBNA12345, TBBNB1234

# - Auto-detected when Service ID starts with "TBBN"

# 

# **Partner Service ID (Wholesale Partners):**

# - Digi: DIGI\d+ or DIGI00\d+ (e.g., DIGI0012345)

# - Celcom: CELCOM\d+ or CELCOM00\d+ (e.g., CELCOM0016996)

# - U Mobile: UMOBILE\d+ or UMOBILE00\d+

# - Auto-detected based on partner-specific patterns

# 

# **Auto-Selection Rules:**

# - Assurance Order Type → TIME ASSURANCE partner

# - FTTO Installation Method/Building Type → TIME FTTO partner

# - Service ID pattern matching → Corresponding partner (TIME, Digi, Celcom, U Mobile)

# 

# See [Service ID Rules](./SERVICE_ID_RULES.md) for complete specification.

# 

# 2.2 Universal Unique Identifier

# 

# The unique ID for all orders is:

# 

# TIME activation/modification:

# serviceId = TBBN[A-Z]?\d+[A-Z]? (e.g., TBBN1234567, TBBNA12345)

# 

# TIME–Digi:

# serviceId = DIGI\d+ or DIGI00\d+ (e.g., DIGI0012345)

# 

# TIME–Celcom:

# serviceId = CELCOM\d+ or CELCOM00\d+ (e.g., CELCOM0016996)

# 

# Assurance:

# serviceId + ticketId (TTKTxxxxxx)

# 

# SDU:

# 

# If no serviceId exists:

# 

# uniqueId = SDU\_<timestamp>

# 

# 3\. Required Fields by Order Type

# Order Type	Required Fields

# Activation	serviceId, customerName, contact, fullAddress, appointmentDateTime

# Modification Indoor	serviceId, customerName, address, appointmentDateTime, oldLocationNote, newLocationNote

# Modification Outdoor	serviceId, customerName, oldAddress, newAddress, appointmentDateTime

# Assurance	serviceId, ticketId, contact, issue, appointmentDateTime, workOrderUrl

# SDU	serviceId, address, customerName, appointment

# 

# All data normalization \& validation rules follow the email parser rules defined separately.

# 

# 4\. Customer Structure

# "customer": {

# &nbsp; "name": "KUAN TE SIANG",

# &nbsp; "contactNo": "0166587158",

# &nbsp; "email": "optional",

# &nbsp; "alternateContact": "optional"

# }

# 

# Contact Number Auto-Fix Rules:

# 

# Remove +60

# 

# Ensure 10 or 11 digits

# 

# If starts with 1xxxxxx → prefix with 0

# 

# Always store as:

# 0XXXXXXXXX (no symbols, no spaces)

# 

# 5\. Address Structure

# "address": {

# &nbsp; "fullAddress": "BLOCK B, LEVEL 33A, UNIT 20, UNITED POINT...",

# &nbsp; "unit": "Unit 20",

# &nbsp; "floor": "33A",

# &nbsp; "block": "B",

# &nbsp; "buildingName": "United Point Residence",

# &nbsp; "postcode": "51200",

# &nbsp; "city": "Kuala Lumpur"

# }

# 

# 

# Parser automatically extracts:

# 

# Block

# 

# Level/Floor

# 

# Unit

# 

# Building Name

# 

# Postcode

# 

# City

# 

# Address truncation:

# 

# Long addresses are truncated after 120 chars for display

# 

# Full address stored in DB

# 

# Full address sent to Google Sheets for CephasOps logs

# 

# 6\. Appointment Structure

# "appointment": {

# &nbsp; "date": "2025-11-29",

# &nbsp; "time": "11:00",

# &nbsp; "originalDateTime": "",

# &nbsp; "proposedDateTime": "",

# &nbsp; "approvedDateTime": "",

# &nbsp; "isRescheduled": false

# }

# 

# Rules:

# 

# Admin cannot change date/time without TIME approval

# 

# When admin clicks reschedule:

# 

# Move to ReschedulePendingApproval

# 

# appointment.proposedDateTime is stored

# 

# TIME approval email updates:

# 

# appointment.approvedDateTime

# 

# status = Assigned

# 

# 7\. Relocation Structure (Modification)

# 7.1 Indoor Relocation

# 

# Same unit, different room.

# 

# "relocation": {

# &nbsp; "type": "Indoor",

# &nbsp; "oldLocationNote": "Bedroom 3",

# &nbsp; "newLocationNote": "Living Hall"

# }

# 

# 7.2 Outdoor Relocation

# 

# Two addresses.

# 

# "relocation": {

# &nbsp; "type": "Outdoor",

# &nbsp; "oldAddress": { ... },

# &nbsp; "newAddress": { ... }

# }

# 

# 8\. Assurance Structure (TTKT/LOSi/LOBi)

# "assurance": {

# &nbsp; "ticketId": "TTKT202511178606510",

# &nbsp; "awoId": "",

# &nbsp; "issueCategory": "Link Down (LOSi/LOBi)",

# &nbsp; "issueRemarks": "",

# &nbsp; "initialTroubleshooting": \[

# &nbsp;   "Advise customer to check fibre connector",

# &nbsp;   "Advise customer to reboot ONU"

# &nbsp; ]

# }

# 

# 9\. Network Info Structure

# 

# Network configuration details for fiber internet service:

# 

# "networkInfo": {

# &nbsp; "package": "TIME Fibre 600Mbps Home Broadband",

# &nbsp; "bandwidth": "600 Mbps",

# &nbsp; "loginId": "user@time.com.my",

# &nbsp; "password": "********",

# &nbsp; "wanIp": "1.2.3.4",

# &nbsp; "lanIp": "192.168.1.1",

# &nbsp; "gateway": "192.168.1.1",

# &nbsp; "subnetMask": "255.255.255.0"

# }

# 

# **Fields:**

# - Package: Multi-line package/plan name

# - Bandwidth: Network bandwidth (e.g., "600 Mbps")

# - Login ID: Network login identifier

# - Password: Network password (masked in UI)

# - WAN IP: WAN IP address

# - LAN IP: LAN IP address

# - Gateway: Gateway IP address

# - Subnet Mask: Subnet mask

# 

# **Parser Extraction:**

# - "PACKAGE / BANDWIDTH" from Excel → split into Package and Bandwidth

# - "LOGIN ID" → LoginId

# - "PASSWORD" → Password

# - WAN/LAN/Gateway/Subnet Mask extracted from Excel if present

# 

# 10\. VOIP Structure

# 

# VOIP (Voice over IP) configuration details:

# 

# "voip": {

# &nbsp; "serviceId": "0327098036",

# &nbsp; "password": "abcd1234",

# &nbsp; "ipAddressOnu": "192.168.1.100",

# &nbsp; "gatewayOnu": "192.168.1.1",

# &nbsp; "subnetMaskOnu": "255.255.255.0",

# &nbsp; "ipAddressSrp": "10.0.0.1",

# &nbsp; "remarks": "VOIP service notes"

# }

# 

# **Fields:**

# - Service ID / Password: Split from "Service ID / Password" format (e.g., "0327098036/abcd1234")

# - IP Address ONU: ONU IP address for VOIP

# - Gateway ONU: ONU Gateway IP

# - Subnet Mask ONU: ONU Subnet Mask

# - IP Address SRP: SRP IP address

# - Remarks: Multi-line VOIP notes

# 

# **Parser Extraction:**

# - "SERVICE ID. / PASSWORD" from Excel → split by "/" into ServiceId and Password

# - "IP ADDRESS ONU" → IpAddressOnu

# - "GATEWAY ONU" → GatewayOnu

# - "SUBNET MASK ONU" → SubnetMaskOnu

# - "IP ADDRESS SRP" → IpAddressSrp

# - "Remarks" → Remarks

# 

# 11\. Materials Object

# 

# Auto-generated based on:

# 

# Building type (PRELAID / NON\_PRELAID / SDU)

# 

# Order type (Activation, Modification, SDU)

# 

# "materials": {

# &nbsp; "autoSuggested": \[

# &nbsp;   { "itemId": "PATCHCORD6M", "qty": 1 },

# &nbsp;   { "itemId": "PATCHCORD10M", "qty": 1 }

# &nbsp; ],

# &nbsp; "issuedToSI": \[],

# &nbsp; "used": \[],

# &nbsp; "returned": \[]

# }

# 

# 12\. Splitter Usage

# 

# Mandatory at job completion.

# 

# "splitterUsage": {

# &nbsp; "splitterId": "SP-UNITEDPOINT-32P-01",

# &nbsp; "port": 17,

# &nbsp; "validated": true,

# &nbsp; "validationRemark": ""

# }

# 

# Rules:

# 

# Port must not be used before

# 

# Splitter must belong to assigned building

# 

# Port cannot be standby port

# 

# System permanently locks port once used

# 

# 13\. Billing Structure

# "billing": {

# &nbsp; "billingScenario": "TIME\_PRINCIPAL | DIRECT",

# &nbsp; "billingPartnerId": "TIME",

# &nbsp; "priceTemplateId": "",

# &nbsp; "invoice": {

# &nbsp;   "invoiceNo": "",

# &nbsp;   "portalUploadDate": "",

# &nbsp;   "dueDate": "",

# &nbsp;   "payment": {

# &nbsp;     "date": "",

# &nbsp;     "amount": 0,

# &nbsp;     "ref": ""

# &nbsp;   }

# &nbsp; }

# }

# 

# Multi-partner logic:

# 

# All Digi, Celcom, CelcomDigi → Billed to TIME

# 

# Direct partners → Use partner-specific templates

# 

# 14\. Status \& Status History

# "status": "Pending"

# 

# 

# History item:

# 

# {

# &nbsp; "status": "Assigned",

# &nbsp; "timestamp": "2025-11-15T10:30:00",

# &nbsp; "actor": "ADMIN",

# &nbsp; "remark": "Assigned to SI Klavin"

# }

# 

# 15\. Reschedule History

# "rescheduleHistory": \[

# &nbsp; {

# &nbsp;   "requestedBy": "ADMIN",

# &nbsp;   "requestedDateTime": "2025-11-30 14:00",

# &nbsp;   "approvalSource": "EMAIL",

# &nbsp;   "approvedDateTime": "2025-12-01 09:00",

# &nbsp;   "emailId": "MSGID123"

# &nbsp; }

# ]

# 

# 16\. Attachments

# "attachments": \[

# &nbsp; {

# &nbsp;   "filename": "workorder.xlsx",

# &nbsp;   "type": "excel",

# &nbsp;   "sourceEmailId": "MSGID-22"

# &nbsp; }

# ]

# 

# 17\. API ENDPOINTS (Full)

# 17.1 POST /api/orders

# 

# Create new order (manual or API client)

# 

# 17.2 GET /api/orders/{id}

# 

# Return full order details

# Includes:

# 

# Customer

# 

# Address

# 

# Materials

# 

# Splitter usage

# 

# Docket status

# 

# Billing info

# 

# 17.3 POST /api/orders/{id}/assign

# 

# Admin assigns SI.

# 

# Payload:

# 

# {

# &nbsp; "siId": "SI001",

# &nbsp; "appointment": { "date": "2025-11-29", "time": "11:00" }

# }

# 

# 17.4 POST /api/orders/{id}/reschedule-request

# 

# Moves order into ReschedulePendingApproval.

# 

# 17.5 POST /api/orders/{id}/reschedule-approve

# 

# Parser or admin triggers this when receiving TIME approval email.

# 

# 17.6 POST /api/orders/{id}/status

# 

# General status change endpoint.

# 

# {

# &nbsp; "status": "OnTheWay",

# &nbsp; "timestamp": "2025-11-29 10:45",

# &nbsp; "actor": "SI"

# }

# 

# 17.7 POST /api/orders/{id}/splitter

# 

# Record splitter usage.

# 

# 17.8 POST /api/orders/{id}/invoice

# 18\. Validation Summary

# 

# Service ID must be unique per day.

# 

# Partner ID must match partner pattern.

# 

# Appointment must be approved by TIME if rescheduled.

# 

# Splitter usage mandatory before docket stage.

# 

# Contact number must be normalized.

# 

# Modification outdoor must include both addresses.

# 

# Assurance must include TTKT.

# 

# Billing scenario must follow rules.

# 

# 19\. End of Orders Specification

# 

# **Last Updated:** December 2025

# 

# **Updates:** Added Service ID Type (TBBN vs Partner Service ID), Network Info fields, VOIP fields, and unified order workflow.

# 

# This file is the official reference for developers and AI agents implementing CephasOps order management.

# 

# This file is the official reference for developers and AI agents implementing CephasOps order management.

