# API Endpoints Reference

## Base URL
```
http://localhost:8000
```

---

## Health Check Endpoints

### GET /
Returns service information and health status.

**Request:**
```bash
curl http://localhost:8000/
```

**Response (200 OK):**
```json
{
  "status": "ok",
  "service": "Smartex AR Maintenance API",
  "version": "0.1.0"
}
```

---

### GET /health
Health check endpoint for load balancers.

**Request:**
```bash
curl http://localhost:8000/health
```

**Response (200 OK):**
```json
{
  "status": "healthy"
}
```

---

## Maintenance Endpoints

### GET /maintenance/procedures/{device_id}

Fetch the maintenance procedure for a specific device.

**Parameters:**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| device_id | string | Yes | Unique machine identifier (e.g., "ESP32_TEX_001") |

**Request:**
```bash
curl http://localhost:8000/maintenance/procedures/ESP32_TEX_001
```

**Response (200 OK):**
```json
{
  "procedure_id": "proc_fallback_cleaning_v1",
  "device_id": "ESP32_TEX_001",
  "title": "Weekly Machine Cleaning",
  "schema_version": 1,
  "steps": [
    {
      "id": 1,
      "text": "Power down machine",
      "hotspot_position": {
        "x": 0.0,
        "y": 0.5,
        "z": 0.0
      },
      "image_url": null
    },
    {
      "id": 2,
      "text": "Remove dust from vents",
      "hotspot_position": {
        "x": 0.3,
        "y": 0.7,
        "z": 0.0
      },
      "image_url": null
    },
    {
      "id": 3,
      "text": "Inspect heddle bearings",
      "hotspot_position": {
        "x": -0.3,
        "y": 0.3,
        "z": 0.0
      },
      "image_url": null
    },
    {
      "id": 4,
      "text": "Lubricate rail system",
      "hotspot_position": {
        "x": 0.0,
        "y": 0.1,
        "z": 0.0
      },
      "image_url": null
    },
    {
      "id": 5,
      "text": "Power on and verify operation",
      "hotspot_position": {
        "x": 0.2,
        "y": 0.2,
        "z": 0.0
      },
      "image_url": null
    }
  ]
}
```

**Response (404 Not Found):**
```json
{
  "detail": "No procedure found for device UNKNOWN_DEVICE"
}
```

**Error Codes:**
| Code | Meaning |
|------|---------|
| 200 | Procedure found |
| 404 | Device has no procedure |
| 422 | Invalid device_id format |
| 500 | Server error |

**Usage in Unity:**
```csharp
var deviceId = "ESP32_TEX_001";
var response = await maintenanceService.GetProcedure(deviceId);
// response.steps contains 5-step procedure with 3D positions
```

---

### POST /maintenance/logs

Log completion of maintenance steps.

**Request Body:**
```json
{
  "device_id": "ESP32_TEX_001",
  "procedure_id": "proc_fallback_cleaning_v1",
  "completed_steps": [1, 2, 3],
  "user_id": "tech_john_42",
  "notes": "Completed first 3 steps. Need to replace filter in step 4."
}
```

**Request (cURL):**
```bash
curl -X POST http://localhost:8000/maintenance/logs \
  -H "Content-Type: application/json" \
  -d '{
    "device_id": "ESP32_TEX_001",
    "procedure_id": "proc_fallback_cleaning_v1",
    "completed_steps": [1, 2, 3],
    "user_id": "tech_john_42",
    "notes": "Completed first 3 steps"
  }'
```

**Response (200 OK):**
```json
{
  "device_id": "ESP32_TEX_001",
  "procedure_id": "proc_fallback_cleaning_v1",
  "completed_steps": [1, 2, 3],
  "user_id": "tech_john_42",
  "notes": "Completed first 3 steps",
  "id": 1,
  "timestamp": "2025-05-25T14:30:00Z",
  "created_at": "2025-05-25T14:30:00Z"
}
```

**Request Body Schema:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| device_id | string | Yes | Machine identifier |
| procedure_id | string | Yes | Which procedure was being followed |
| completed_steps | integer[] | Yes | Array of step IDs (e.g., [1, 2, 3]) |
| user_id | string | Yes | Technician identifier |
| notes | string | No | Optional technician comments |

**Error Codes:**
| Code | Meaning |
|------|---------|
| 200 | Log saved successfully |
| 422 | Missing required field |
| 500 | Database error |

**Usage in Unity:**
```csharp
var log = new MaintenanceLog
{
    device_id = "ESP32_TEX_001",
    procedure_id = "proc_fallback_cleaning_v1",
    completed_steps = new List<int> { 1, 2, 3 },
    user_id = "tech_john_42",
    notes = "Done with step 3"
};
var response = await maintenanceService.LogCompletion(log);
// response.id contains the database record ID
```

---

### GET /maintenance/logs/{device_id}

Retrieve all maintenance logs for a device.

**Parameters:**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| device_id | string | Yes | Machine identifier |

**Request:**
```bash
curl http://localhost:8000/maintenance/logs/ESP32_TEX_001
```

**Response (200 OK):**
```json
[
  {
    "id": 2,
    "device_id": "ESP32_TEX_001",
    "procedure_id": "proc_fallback_cleaning_v1",
    "completed_steps": [3, 4, 5],
    "user_id": "tech_jane_50",
    "notes": null,
    "timestamp": "2025-05-25T15:45:00Z",
    "created_at": "2025-05-25T15:45:00Z"
  },
  {
    "id": 1,
    "device_id": "ESP32_TEX_001",
    "procedure_id": "proc_fallback_cleaning_v1",
    "completed_steps": [1, 2, 3],
    "user_id": "tech_john_42",
    "notes": "First 3 steps completed",
    "timestamp": "2025-05-25T14:30:00Z",
    "created_at": "2025-05-25T14:30:00Z"
  }
]
```

**Response (Empty):**
```json
[]
```

**Error Codes:**
| Code | Meaning |
|------|---------|
| 200 | Returns array (may be empty) |
| 500 | Server error |

**Usage in Unity:**
```csharp
var logs = await maintenanceService.GetLogs("ESP32_TEX_001");
// logs contains maintenance history for the device
```

---

## Data Models

### Procedure
```json
{
  "procedure_id": "string",          // Unique identifier
  "device_id": "string",              // Machine identifier
  "title": "string",                  // Human-readable title
  "schema_version": 1,                // API version for compatibility
  "steps": [
    {
      "id": 1,                        // Step number (1-indexed)
      "text": "string",               // Instruction text
      "hotspot_position": {
        "x": 0.0,                     // Machine-local coordinates
        "y": 0.5,
        "z": 0.0
      },
      "image_url": "string or null"   // Optional illustration
    }
  ]
}
```

### MaintenanceLog
```json
{
  "id": 1,                            // Database record ID
  "device_id": "string",              // Machine identifier
  "procedure_id": "string",           // Which procedure
  "completed_steps": [1, 2, 3],       // Steps finished
  "user_id": "string",                // Technician ID
  "notes": "string or null",          // Optional comments
  "timestamp": "2025-05-25T14:30:00Z",// When logged (ISO 8601)
  "created_at": "2025-05-25T14:30:00Z"// Database timestamp
}
```

---

## Integration Examples

### C# (Unity)
```csharp
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

// Fetch procedure
var service = ARServices.Get<IMaintenanceService>();
var procedure = await service.GetProcedure("ESP32_TEX_001");

// Display steps
foreach (var step in procedure.steps)
{
    Debug.Log($"Step {step.id}: {step.text}");
    Debug.Log($"Position: {step.hotspot_position.x}, {step.hotspot_position.y}, {step.hotspot_position.z}");
}

// Log completion
await service.LogCompletion(
    deviceId: "ESP32_TEX_001",
    procedureId: "proc_fallback_cleaning_v1",
    completedSteps: new int[] { 1, 2, 3 },
    userId: "tech_john_42"
);
```

### Python (Testing)
```python
import requests
import json

BASE_URL = "http://localhost:8000"

# Get procedure
response = requests.get(f"{BASE_URL}/maintenance/procedures/ESP32_TEX_001")
procedure = response.json()
print(f"Steps: {len(procedure['steps'])}")

# Log completion
log_data = {
    "device_id": "ESP32_TEX_001",
    "procedure_id": "proc_fallback_cleaning_v1",
    "completed_steps": [1, 2, 3],
    "user_id": "tech_john_42",
    "notes": "All good"
}
response = requests.post(f"{BASE_URL}/maintenance/logs", json=log_data)
print(f"Log ID: {response.json()['id']}")

# Get logs
response = requests.get(f"{BASE_URL}/maintenance/logs/ESP32_TEX_001")
logs = response.json()
print(f"Total logs: {len(logs)}")
```

---

## HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200 | Request successful |
| 201 | Resource created |
| 400 | Bad request (malformed JSON) |
| 404 | Not found (device/procedure doesn't exist) |
| 422 | Unprocessable entity (validation error) |
| 500 | Server error |
| 503 | Service unavailable |

---

## Rate Limiting

Currently **no rate limiting**. Production deployment should add:
- 1000 requests/minute per IP
- 10,000 requests/minute total

---

## CORS Headers

All endpoints support Cross-Origin requests:
```
Access-Control-Allow-Origin: *
Access-Control-Allow-Methods: GET, POST, PUT, DELETE, OPTIONS
Access-Control-Allow-Headers: Content-Type
```

---

## Testing the API

### Using Swagger UI
Go to: `http://localhost:8000/docs`
- Interactive API explorer
- Try endpoints directly in browser
- See request/response examples

### Using cURL
```bash
# Health check
curl http://localhost:8000/health

# Get procedure
curl http://localhost:8000/maintenance/procedures/ESP32_TEX_001

# Log completion
curl -X POST http://localhost:8000/maintenance/logs \
  -H "Content-Type: application/json" \
  -d '{"device_id":"ESP32_TEX_001","procedure_id":"proc_001","completed_steps":[1,2,3],"user_id":"tech1"}'
```

### Using Postman
1. Import collection from `http://localhost:8000/openapi.json`
2. Run endpoints with pre-filled examples
3. Test different device IDs and step combinations

---

## API Versioning

Current version: **1.0**

Breaking changes will increment major version:
- `v1` → `v2` (e.g., `/maintenance/v2/procedures/...`)

---

## Support

Issues? Check:
1. Backend running: `curl http://localhost:8000/health`
2. Device ID correct: Device must exist in procedures
3. Ports: Make sure 8000 is not blocked
4. See [BACKEND_SETUP.md](./BACKEND_SETUP.md) for troubleshooting
