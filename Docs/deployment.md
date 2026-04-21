# Deployment guide

How to get a build from `master` onto a real person's device, in the three
channels we care about: **internal team**, **pilot users at the factory**,
and **public store release**.

This is the operational doc — for the "why we chose these channels" see
`docs/smartexvr-ar-team-roadmap.md`.

## Channels at a glance

| Channel | Platforms | Who | Cadence | Signing |
|---|---|---|---|---|
| CI artifact | Android, iOS | The 7-person team | Every PR | Debug keystore |
| Google Play — Internal testing | Android | Up to 100 named testers | Weekly from master | Upload key |
| TestFlight | iOS | Up to 10 000 external testers | Weekly from master | Apple Dev cert |
| SideQuest / Meta App Lab | Quest 2 | Pilot factory + school demos | On demand | Oculus signing cert |
| Google Play — Production | Android | Public | Monthly, after pilot | Upload key |
| App Store | iOS | Public | Monthly, after pilot | Apple Dev cert |

## Versioning

We use **SemVer for the user-visible version + a monotonic `bundleVersionCode`.**

- `PlayerSettings.bundleVersion` = `MAJOR.MINOR.PATCH` — e.g. `0.3.1`
- `PlayerSettings.Android.bundleVersionCode` = integer, strictly increasing
- `PlayerSettings.iOS.buildNumber` = integer string, strictly increasing

CI bumps the build code automatically (`GITHUB_RUN_NUMBER`). The
`bundleVersion` is hand-edited when the team agrees we've hit a milestone.

Tag the git commit that produced a published build: `git tag v0.3.1 && git push --tags`.

## Android → Google Play Internal track (the 90% case)

1. **One-time setup**
   - Create an **upload keystore**: `keytool -genkey -v -keystore smartex-upload.keystore -alias smartex -keyalg RSA -keysize 2048 -validity 10000`
   - Upload to repo secrets (base64): `ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS`
   - In Play Console → Create app → enable Play App Signing (recommended). Upload `smartex-upload.keystore`'s *upload certificate*.
2. **Per release**
   - Merge to `master` → CI produces a signed `.aab` artifact.
   - Download from the GitHub Actions run page.
   - Play Console → your app → Testing → Internal testing → Create new release → Upload the `.aab`.
   - Release notes: paste the merged-PR titles since the last tag. Submit.
3. **Testers see the build** within ~10 min via the opt-in link.

Gotchas:
- Play will reject the first upload if target SDK < 34. Player Settings → Other Settings → Target API Level = **34 or higher**.
- **Do not** commit the keystore to git. Ever.

## iOS → TestFlight

1. **One-time setup**
   - Apple Developer account (€99/yr, shared).
   - App ID registered in developer.apple.com (bundle id: `com.smartex.vrar` or your chosen one).
   - A distribution provisioning profile.
2. **Per release**
   - CI's `build-ios` job produces an Xcode project (not an `.ipa` — Unity on Linux/Windows cannot produce `.ipa`).
   - Download the Xcode project artifact → open in Xcode on a Mac → **Product → Archive**.
   - Organizer window → **Distribute App → App Store Connect → Upload**.
   - App Store Connect → TestFlight → add build to external testers.

If no Mac is available the iOS channel is simply closed for that sprint. That's fine.

## Quest 2 → Internal demo (SideQuest / Meta App Lab)

For a school demo or a factory pilot on a Quest, we sideload rather than publish.

- `adb install -r SmartexVR.apk` with the headset in Developer Mode.
- Or drop the APK into SideQuest and install via USB.
- For a long-running pilot we'll publish on **Meta App Lab** (unlisted store) — that's a ~2-week review process and we only do it once we have a stable build.

## Release checklist (owner: whoever cuts the release)

Before pushing to Internal / TestFlight:

- [ ] `master` CI is green on all jobs (android, ios, test, secrets-guard)
- [ ] `PlayerSettings.bundleVersion` bumped if this is a milestone
- [ ] `Docs/performance-baseline.md` numbers not worse than last release
- [ ] `CHANGELOG.md` (or the GitHub Releases page) has notes for testers
- [ ] Tested the APK on a physical Quest 2 and a physical Android phone
- [ ] ARConfig.asset in `Resources/` points at the **production** backend,
      not `localhost`

After cutting:

- [ ] Tagged the commit: `git tag vX.Y.Z && git push --tags`
- [ ] Pinged the `#smartex-releases` channel with: version, what's new, what to test, known issues

## Rollback

Internal / TestFlight: just upload an older `.aab` / rebuild the previous
tag. No store review to wait for.

Production: Play Console supports **staged rollouts** (start at 10 %, watch
the crash rate, ramp up). If a release is bad, halt the rollout — existing
users stay on the old version.
