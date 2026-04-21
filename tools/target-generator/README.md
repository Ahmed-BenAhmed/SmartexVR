# Target generator

Generates printable marker sheets (QR-ish image + device label) for every
machine listed in `markers.yaml`. Produces:

- `out/<deviceId>.png` — 1024×1024 image suitable for Vuforia Target Manager upload
- `out/markers.pdf` — all markers laid out 2×3 per A4 page, ready to print
- `out/machines_database_spec.csv` — the device-id → target-name mapping Module B consumes

## Why a tool at all

Every machine on the factory floor needs a unique marker. Doing this by
hand — open Illustrator, pick a code, export, label, rinse, repeat × 8 —
is a 90-minute task that gets redone every time we add a machine. This
script turns it into a 5-second command.

## Vuforia caveat

Vuforia's Target Manager expects you to upload images one by one through
their web UI (or via the VWS REST API with a per-database key). This tool
generates the images and a CSV of the intended `deviceId ↔ targetName`
mapping; a human still uploads to the Vuforia dashboard. If we later get
a VWS key in ARConfig, the upload step can be automated — see
`# TODO(VWS)` below.

## Install

```
cd tools/target-generator
python -m pip install -r requirements.txt
```

## Run

```
# Edit markers.yaml to list your machines, then:
python generate.py
# Outputs land in ./out
```

## Files

- `generate.py` — the CLI
- `markers.yaml` — device list (edit this)
- `requirements.txt` — `qrcode`, `Pillow`, `PyYAML`, `reportlab`

## Next steps for integration

1. Upload each `out/<deviceId>.png` to developer.vuforia.com → Target
   Manager → Cloud Database "SmartexMachines" (name configured in
   `ARConfig.vuforiaDatabaseName`). Use `<deviceId>` as the target name so
   Module B's `VuforiaTargetScanner` can resolve `TrackableBehaviour.TargetName`
   directly to the backend's device ID.
2. Download the resulting `.xml` + `.dat` target dataset and drop it into
   `Assets/StreamingAssets/Vuforia/` (or import as a Vuforia target asset).
3. Run the marker-registration script against the backend so `/markers`
   knows about each new device (see Module B roadmap for the endpoint).
