
# PARSER_INPUT_EXAMPLES.md – Full Version

## 1. Activation Email Example
Subject:
```
Activation - CEPHAS TRADING - TBBNB062587G - 14/02/2025
```

Body:
```
Customer: John Lim
Address: 1 Jalan SS, Subang Jaya
Appointment: 14 Feb 2025 10:00 AM
Package: TIME 500Mbps
Installer: CEPHAS TRADING
```

Attachment (Excel columns):
- ServiceId
- CustomerName
- Address
- AppointmentDate
- RouterModel
- CableLengthNeeded

Parsed JSON:
```json
{
  "type": "Activation",
  "serviceId": "TBBNB062587G",
  "customerName": "John Lim",
  "address": "...",
  "appointment": "2025-02-14T10:00:00",
  "routerModel": "HG8145V5"
}
```

## 2. Assurance Email Example
Subject:
```
APP M T - <CEPHAS TRADING ><TBBNA261593G><Chow Yu Yang><TTKT202511138603863><AWO437884>
```

Parsed fields:
```json
{
  "type": "Assurance",
  "serviceId": "TBBNA261593G",
  "ttkt": "TTKT202511138603863",
  "awo": "AWO437884",
  "customer": "Chow Yu Yang"
}
```

