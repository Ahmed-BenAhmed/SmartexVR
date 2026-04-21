<!-- Keep this template short enough that nobody skips it. -->

## What + why

<!-- One or two sentences. What does this PR change? Why? Link the issue / module. -->

Module: <!-- A / B / C / D / E / F / G -->
Closes: <!-- #issue -->

## How to test

<!-- Commands, menus, scene to open, device(s) used. Be specific. -->

- [ ] Opens cleanly in Unity 6000.3.11f1 with zero new console errors
- [ ] Ran in Editor on the target scene
- [ ] Tested on device: <!-- Quest 2 / Android phone / iPhone / N/A -->

## Checklist (merge blockers)

- [ ] No hardcoded URLs, tokens, or Vuforia license keys — everything reads from `ARConfig.Instance` or `SmartexConfig.Instance`
- [ ] New public APIs live behind one of the four `Contracts/` interfaces (or a new interface was added with a Mock\* sibling)
- [ ] No `.meta` file conflicts — ran `git status` and every new asset has a committed `.meta`
- [ ] Didn't bump Unity version, URP version, or any XR package patch version without team sign-off
- [ ] If touching the build: ran `File → Build Profiles → Android → Build` at least once locally
- [ ] If touching tracking / anchoring: tested under realistic factory lighting (fluorescent + mixed, not just a desk lamp)
- [ ] If touching performance-sensitive code: attached a Profiler screenshot (CPU + GPU ms) to the PR

## Screenshots / recording

<!-- Drop a gif or still. For AR / VR changes this is not optional — the reviewer cannot tell from the diff alone whether the UX is right. -->

## Risk + rollback

<!-- One line: worst case if this ships broken, and how to revert. -->

---

### Reviewer checklist

- [ ] Code review: readable, no dead branches, no TODOs without an owner
- [ ] Contract impact: did interfaces in `Assets/Scripts/Contracts/` change? If yes, all consumers updated in the same PR
- [ ] Perf: no new per-frame allocations in hot paths (Update, LateUpdate, OnPreRender)
- [ ] Docs: if behavior changed for other modules, `Docs/` reflects it
