# Contracts/ — shared interfaces + mocks

Everything in this folder is the "frozen" contract surface between the seven
AR modules. **If you change a signature here, coordinate on the team channel
before merging** — breaking a contract breaks every consumer.

## What's here

| File | Owner | What it is |
|---|---|---|
| `IMachineRecognizer.cs` | Module B | Vuforia recognition event source |
| `IMaintenanceService.cs` | Module D | Maintenance procedures + logs |
| `IRemoteAssistService.cs` | Module E | WebRTC expert-assist session |
| `ITrainingService.cs` | Module F | Training modules + assessments |
| `DataTypes.cs` | shared | DTOs used across the interfaces |
| `ARServices.cs` | shared | Static service locator — `ARServices.Recognizer`, etc. |
| `Mocks/Mock*.cs` | each module | Editor-runnable fakes |
| `Mocks/ContractsSandboxDriver.cs` | shared | Drop-in MonoBehaviour that wires all four mocks |

## How to use in your module

```csharp
// Subscribe to a contract — never cast to the concrete type.
void OnEnable()  { ARServices.Recognizer.OnMachineRecognized += HandleRecognized; }
void OnDisable() { ARServices.Recognizer.OnMachineRecognized -= HandleRecognized; }

void HandleRecognized(RecognizedMachine m)
{
    var panel = Instantiate(panelPrefab, m.AnchorTransform);  // parented = tracked
    panel.Bind(m.Data);
}
```

## Running the sandbox (no Vuforia, no phone)

1. Open / create scene `Assets/Scenes/_ContractsSandbox.unity`.
2. Add an empty GameObject, attach **ContractsSandboxDriver**.
3. Hit Play. Keys:
   - `1..8` — emit `OnMachineRecognized` for `ESP32_TEX_001..008`
   - `0` — emit `OnMachineLost` for the last one
4. Anchors are placed 2 m in front of `Camera.main`; your panel / banner /
   hotspot should appear there.

## Replacing a mock with the real implementation

In a production bootstrapper scene:

```csharp
void Awake()
{
    // Production components register themselves in their own Awake, OR
    // a central bootstrapper does it explicitly:
    ARServices.Register(FindFirstObjectByType<VuforiaTargetScanner>());
    ARServices.Register(new MaintenanceService(httpClient));
    ARServices.Register(new RemoteAssistService(wsClient));
    ARServices.Register(new TrainingService(httpClient));
}
```

That's the *only* line that changes. Every consumer keeps working.

## Assembly layout

- `Smartex.AR.Contracts.asmdef` — interfaces + DTOs + locator. Every module
  asmdef should reference this.
- `Smartex.AR.Contracts.Mocks.asmdef` — the fakes. Before shipping a release
  build, add `UNITY_EDITOR` as a `defineConstraint` here to exclude it from
  device builds.
