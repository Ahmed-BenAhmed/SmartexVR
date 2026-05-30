# Module D: AR Maintenance Workflow

## Overview

Module D implements an **AR-guided maintenance workflow** for SmartexVR machines. Technicians view maintenance procedures in AR with step-by-step instructions and hotspot callouts positioned on the machine itself.

## Features

✅ **Real-time Procedure Fetching**
- Fetches maintenance procedures from FastAPI backend
- Device-specific procedures (different steps for different machines)
- Automatic fallback to bundled JSON if backend unavailable

✅ **AR User Interface**
- Floating banner displays when machine needs maintenance (health < 0.4)
- Numbered callouts at exact 3D positions on the machine
- World-space AR labels using TextMeshPro
- Automatic cleanup when maintenance complete

✅ **Completion Logging**
- Logs which steps were completed
- Captures technician user ID
- Timestamps automatically recorded
- Optional notes for complex issues

✅ **Mock-Driven Development**
- `MaintenanceService` (production) and `MockMaintenanceService` (testing)
- Same `IMaintenanceService` interface
- Other modules can test without backend running

---

## Architecture

### Components

#### **1. MaintenanceService.cs**
**Location:** `Assets/Scripts/AR/Maintenance/MaintenanceService.cs`

**Responsibility:** HTTP client for backend communication

**Key Methods:**
```csharp
public async Task<Procedure> GetProcedure(string deviceId, CancellationToken ct = default)
// Fetches procedure from GET /maintenance/procedures/{deviceId}
// Returns fallback if backend unreachable

public async Task LogCompletion(string deviceId, string procedureId, int[] completedSteps, string userId)
// POSTs completion to POST /maintenance/logs
```

**Configuration:**
- Backend URL from `SmartexConfig.relayBaseUrl` (never hardcoded)
- Fallback JSON: `Resources/maintenance/fallback.json`

---

#### **2. MaintenanceUIController.cs**
**Location:** `Assets/Scripts/AR/Maintenance/MaintenanceUIController.cs`

**Responsibility:** AR UI display and management

**Key Methods:**
```csharp
public void OnMachineRecognized(RecognizedMachine machine)
// Creates banner when health < 0.4

private async void ShowMaintenanceGuide(RecognizedMachine machine)
// Fetches procedure and displays steps

private void CreateStepCallouts(RecognizedMachine machine, Procedure procedure)
// Instantiates TextMeshPro labels at hotspot positions
```

**Configuration:**
- `_healthThreshold = 0.4f` - Minimum health to trigger maintenance
- `_bannerYOffset = 0.3f` - Banner height above machine
- Integrates with `IMachineRecognizer` for detection
- Uses AR anchoring (Vuforia ImageTarget as parent)

---

#### **3. Data Models**
**Location:** `Assets/Scripts/Contracts/DataTypes.cs`

```csharp
[Serializable]
public class Procedure
{
    public string procedure_id;
    public string device_id;
    public string title;
    public int schema_version;
    public List<ProcedureStep> steps;
}

[Serializable]
public class ProcedureStep
{
    public int id;
    public string text;
    public Position hotspot_position;  // 3D coords in machine local space
    public string image_url;
}

[Serializable]
public class MaintenanceLog
{
    public string device_id;
    public string procedure_id;
    public List<int> completed_steps;  // Step IDs (1, 2, 3...)
    public string user_id;
    public string notes;
}
```

---

### Data Flow

```
┌─────────────────┐
│  VR Headset     │
│  (Unity AR)     │
└────────┬────────┘
         │
         │ OnMachineRecognized (IMachineRecognizer)
         ↓
┌─────────────────────────────────┐
│ MaintenanceUIController         │
│ - Checks health < 0.4           │
│ - Fetches procedure             │
│ - Creates AR callouts           │
└────────┬────────────────────────┘
         │
         │ HTTP GET/POST
         ↓
┌────────────────────────────────────┐
│ FastAPI Backend (localhost:8000)   │
│ - GET /procedures/{device_id}      │
│ - POST /logs (save completions)    │
│ - GET /logs/{device_id} (history)  │
└────────┬───────────────────────────┘
         │
         │ SQLite ORM (SQLAlchemy)
         ↓
┌────────────────────────────────────┐
│ SQLite Database                    │
│ - maintenance_logs table           │
│ - procedures table                 │
└────────────────────────────────────┘
```

---

## Integration Points

### With Machine Recognizer
```csharp
// MaintenanceUIController subscribes to recognition events
_recognizer = ARServices.Get<IMachineRecognizer>();
_recognizer.OnMachineRecognized += OnMachineRecognized;
_recognizer.OnMachineLost += OnMachineLost;
```

### With AR Anchoring
```csharp
// Callouts parented to machine anchor (Vuforia ImageTarget)
// Automatically track with machine via transform hierarchy
// Hotspot positions in machine local space
callout.transform.SetParent(machineAnchor.transform);
callout.transform.localPosition = hotspotPosition;
```

### With Config System
```csharp
// Backend URL from SmartexConfig (never hardcoded)
SmartexConfig cfg = SmartexConfig.Instance;
string backendUrl = cfg.relayBaseUrl;  // e.g., "http://192.168.1.100:8000"
```

---

## Testing

### Using MockMaintenanceService
```csharp
// For testing without backend
ARServices.Register<IMaintenanceService>(new MockMaintenanceService());

// Or use real backend when available
MaintenanceService svc = new MaintenanceService();
ARServices.Register<IMaintenanceService>(svc);
```

### Using Fallback Data
When backend is down, `MaintenanceService` automatically loads `Resources/maintenance/fallback.json`:
- 5-step "Weekly Machine Cleaning" procedure
- Sample hotspot positions
- Works offline without network

---

## Configuration

### SmartexConfig Required
```csharp
// In SmartexConfig.cs
public string relayBaseUrl = "http://192.168.1.100:8000";  // Backend address
```

### Vuforia Setup
- ImageTarget configured for machine recognition
- Database includes machine image markers
- `IMachineRecognizer` implementation detects targets

### UI Resources
- TextMeshPro font asset for callout labels
- Prefab for banner display
- Estimated prefabs for step numbers

---

## Error Handling

| Scenario | Behavior |
|----------|----------|
| Backend unreachable | Load fallback.json, show offline warning |
| Device not found | Return 404, no maintenance shown |
| Missing procedure | Log error, show generic message |
| Network timeout | Retry with exponential backoff |
| Unknown device_id | Skip maintenance (normal operation) |

---

## Future Enhancements

- [ ] Image URLs for step illustrations
- [ ] Video tutorials for complex steps
- [ ] Technician annotations/notes storage
- [ ] Estimated time to complete
- [ ] Part ordering integration
- [ ] Machine history analytics
- [ ] PostgreSQL migration (currently SQLite)
- [ ] Multi-language support for procedures

---

## Related Documentation

- **Backend Setup:** See [BACKEND_SETUP.md](../backend/services/maintenance/README.md)
- **API Reference:** See [API_ENDPOINTS.md](./API_ENDPOINTS.md)
- **Architecture:** See [system-architecture.md](../system-archeticture.md)
