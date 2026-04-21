# Performance baseline

Ground truth for what "good enough" means on our target devices. Re-measure
at the end of each two-week sprint; regressions >10% block merge until
understood.

## Why we bother

AR / VR is merciless. A desktop build can run at 300 fps on a laptop and
still crash on a Quest 2 because of GPU bandwidth, not logic. Without a
baseline the team can't tell whether a PR is slow or whether it was always
this slow. This doc gives every module owner a number to beat.

## Target devices (the only ones that matter for grading)

| Device | SoC | RAM | Display | Module(s) it gates |
|---|---|---|---|---|
| Oculus Quest 2 | Snapdragon XR2 | 6 GB | 1832×1920 per eye, 72 Hz default | VR baseline (C, D) |
| Pixel 6a / mid-range Android | Tensor G1 | 6 GB | 1080×2400, 60 Hz | AR baseline (A, B, E) |
| iPhone 11 | A13 | 4 GB | 828×1792, 60 Hz | ARKit secondary target |

If you only have a high-end phone, **profile anyway, then multiply CPU time
by ~1.5× as a rough mid-range estimate.** Don't ship anything you haven't
profiled on hardware at least once per sprint.

## How to measure

1. Build a **Development Build** with **Autoconnect Profiler** ON
   (`File → Build Profiles → Development Build ✅`, `Autoconnect Profiler ✅`).
2. Deploy to device over USB. Launch the app.
3. Window → Analysis → Profiler. Wait 30 s for the numbers to settle
   (garbage collection, texture streaming, etc.).
4. Record a **60-second steady-state capture** while walking a standard
   route — the 8-machine factory floor, looking at each machine for 5 s.
5. Log: mean FPS, 1% low FPS, CPU ms (main thread), GPU ms, draw calls,
   tris, memory (total allocated). Attach the `.data` profiler capture to
   the PR if you're investigating a regression.

## Baselines — fill in as each module lands

### Module A — Vuforia core (marker tracking)
| Metric | Pixel 6a target | Last measured | Owner |
|---|---|---|---|
| Mean FPS (steady, 1 marker in view) | ≥ 45 | _TBD_ | — |
| CPU main-thread ms | ≤ 18 | _TBD_ | — |
| GPU ms | ≤ 14 | _TBD_ | — |
| Tracking acquire latency | ≤ 600 ms | _TBD_ | — |

### Module B — Machine recognizer + panel spawning
| Metric | Pixel 6a target | Last measured | Owner |
|---|---|---|---|
| Mean FPS (8 machines known, 2 visible) | ≥ 40 | _TBD_ | — |
| New-panel spawn allocation (per event) | 0 B in steady state | _TBD_ | — |
| Draw calls added per panel | ≤ 4 | _TBD_ | — |

### Module C — VR factory floor (Quest 2)
| Metric | Quest 2 target | Last measured | Owner |
|---|---|---|---|
| Mean FPS | ≥ 72 (lock to refresh) | _TBD_ | — |
| CPU main-thread ms | ≤ 11 | _TBD_ | — |
| GPU ms | ≤ 11 | _TBD_ | — |
| Dropped frames per minute | ≤ 2 | _TBD_ | — |

### Module D — Machine detail / interaction
| Metric | Quest 2 target | Pixel 6a target | Last measured | Owner |
|---|---|---|---|---|
| Panel open → visible | ≤ 150 ms | ≤ 200 ms | _TBD_ | — |
| GC allocation per panel open | ≤ 2 KB | ≤ 4 KB | _TBD_ | — |

### Module E — WebRTC remote assist
| Metric | Pixel 6a target | Last measured | Owner |
|---|---|---|---|
| Encode+send CPU overhead | ≤ 6 ms / frame | _TBD_ | — |
| End-to-end glass-to-glass latency (LAN) | ≤ 250 ms | _TBD_ | — |
| Bandwidth (720p30) | ≤ 1.5 Mbps | _TBD_ | — |

### Module F — Training mode
| Metric | Pixel 6a target | Last measured | Owner |
|---|---|---|---|
| Localized string lookup | ≤ 0.1 ms | _TBD_ | — |
| Video asset warm-start | ≤ 400 ms | _TBD_ | — |

## Common regressions and where to look

- **FPS drop with no visible cause** → check draw calls. URP can batch
  aggressively but breaks batching on any material property block change.
- **Main-thread spikes every N frames** → likely a backend poll. Move to
  `UnityWebRequestAsyncOperation` + coroutine, never block the main thread.
- **GC spikes** → strings formatted per frame, LINQ in Update, or
  `FindObjectsOfType`. Cache and allocate once.
- **GPU time up after a prefab change** → check shadow casters and the
  per-pixel light count. `disableMachineShadowsOnMobile` in ARConfig is the
  fastest kill switch.

## When a regression lands

1. Author attaches a `.data` profiler capture + Frame Debugger screenshot.
2. Reviewer compares against the previous number from this doc.
3. If >10% worse and the author doesn't have a story, the PR waits.
4. If we decide it's acceptable (new feature pays for itself in UX), update
   the table in this doc **in the same PR**.
