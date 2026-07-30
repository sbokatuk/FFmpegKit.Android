# FFmpegKit.Android

.NET for Android / .NET MAUI **binding** (`AndroidClassParser=class-parse`) over the native FFmpegKit `.aar`.

## Overview

- One project, `src/FFmpegKit.Android`, produces all eight packages `FFmpegKit.Net.<Variant>.Android` — `Audio|Full|FullGpl|Https|HttpsGpl|Min|MinGpl|Video`, selected by `FFmpegKitBuildType`.
- Native binaries come from the community fork **[ffmpegkit-maintained/ffmpeg](https://github.com/ffmpegkit-maintained/ffmpeg)** via Maven Central (`dev.ffmpegkit-maintained`). `arthenica/ffmpeg-kit` is archived with its assets deleted and `ffmpeg-kit-next` ships no binaries — do not point anything back at them.
- **Two version schemes, never interchangeable.** `FFmpegVersion` (8.1.2) is the FFmpeg inside and what package versions are based on; `FFmpegKitNativeVersion` (8.1.7) is the fork release that names the `.aar`. Both live in `Directory.Build.props`; `build/native-versions.tsv` is the single source of truth for the pairing (`6.1.6↔6.0.3`, `7.1.5↔7.1.6`, `8.1.2↔8.1.7`). Convert with `./build/resolve-versions.sh --ffmpeg 8.1.2` or `--ffmpegkit 8.1.7`.
- Package version = `<FFmpeg version>.<binding revision>`, e.g. `8.1.2.5`. Three tracks publish in parallel; all ship `arm64-v8a` + `x86_64` at minSdk 24, never 32-bit.
- Prerequisites: .NET 8, 9 and 10 SDKs each with the Android workload (the SDK is chosen by the *working directory's* `global.json`), plus Python 3 for the merge step.

## Build and verify

```sh
./src/FFmpegKit.Android/Jars/FetchJars.sh            # download + SHA-256 verify the 8 .aar and 2 .jar files
./src/FFmpegKit.Android/Jars/FetchJars.sh --verify    # re-check what is on disk, no downloads
./src/FFmpegKit.Android/BuildNugets.sh                # pack all 8 variants (two SDK passes, merged) into ./artifacts
./src/FFmpegKit.Android/BuildNugets.sh 8.1.2.5 8.1.7  # package version, FFmpegKit release
dotnet test tests/FFmpegKit.Android.PackageTests      # validate the packed .nupkg files
```

- Single variant: `dotnet pack src/FFmpegKit.Android/FFmpegKit.Android.csproj -c Release -p:FFmpegKitBuildType=Video -p:FFmpegKitSdkBand=net9 -o artifacts`. `FFmpegKitSdkBand` is `net9` (net8.0-android34.0 + net9.0-android35.0) or `net10` (net10.0-android36.0) and must match the SDK actually running.
- Trying a newer upstream line is **baseline first**: `./build/update-checksums.sh 8.2.0`, review and commit the baseline, then `./src/FFmpegKit.Android/Jars/FetchJars.sh 8.2.0`. A line with no committed baseline refuses to fetch.
- `dotnet build FFmpegKit.sln` builds the binding and both test projects only; the MAUI sample is deliberately outside the solution so no MAUI workload is needed.

## Layout

- `src/FFmpegKit.Android/` — `FFmpegKit.Android.csproj`, `Additions/` (hand-written C#), `Transforms/` (Metadata.xml, EnumFields.xml, EnumMethods.xml), `Jars/` (only `AboutJars.txt` and `FetchJars.sh` are tracked), `BuildNugets.sh`, `FFmpegKit.Net.Android.targets` (shipped to consumers).
- `build/` — `checksums/<ffmpegkit>.sha256`, `native-versions.tsv`, `resolve-versions.sh`, `update-checksums.sh`, `merge-packages.py`, `upstream.tsv`, `check-upstream.sh`.
- `tests/FFmpegKit.Android.PackageTests` (runs anywhere), `tests/FFmpegKit.Android.DeviceTests` (on-device smoke), `samples/FFmpegKit.Android.Example` (MAUI), `docs/release-notes/`, `.github/scripts/`.

## Conventions

- The namespace is `Ffmpegkit.Droid` and stays that way — a namespace rooted at `FFmpegKit` makes `FFmpegKit.Execute(...)` ambiguous for every consumer. Assembly and package ids are `FFmpegKit.Net.<Variant>.Android`.
- Build files, scripts and workflows are heavily commented and explain *why* (per-variant `obj/`+`bin/`, the two-pass pack, `PrepareForBuild` hook, the dual numbering). Preserve and extend those comments; do not strip them when editing.
- British spelling in prose ("licence", "behaviour", "initialiser") to match the README.
- `Additions/` types parse invariantly (`CultureInfo.InvariantCulture`) and return `null` for absent or unparsable fields rather than throwing; async wrappers complete with a non-success `ReturnCode` instead of throwing.
- Bind exactly one `.aar` per build (`Bind="true"`); the two `smart-exception` jars are embedded with `Bind="false"` because `FFmpegKitConfig`'s static initialiser needs them at runtime.

## CI and release flow

Everything runs on **ubuntu-latest**. `pr.yml` and `release.yml` both call the reusable `build.yml` (`verify` input: true on PRs, false on releases).

- Pull request → pack all eight variants as `<FFmpegVersion>-beta.<pr>.<run>`, package tests, MAUI sample builds (net8 + net10), emulator smoke tests, then publish the betas to nuget.org. Forked PRs build and test but skip publishing. Betas cannot be deleted, only unlisted.
- Merging `docs/release-notes/<version>.md` to `main` is the release: `auto-release.yml` tags the merge `v<version>` and dispatches `release.yml`, whose `guard` job proves the commit is an ancestor of the default branch before anything publishes.
- Tags name the **FFmpeg** version: `v6.1.6.4` publishes 6.1.6.4 and binds FFmpegKit 6.0.3 from the mapping; a version outside `build/native-versions.tsv` fails the release with the list of known builds.
- `upstream-drift.yml` (daily) runs `build/check-upstream.sh` over `build/upstream.tsv` and files one issue per group.

## Testing

- Run `dotnet test tests/FFmpegKit.Android.PackageTests` before every pull request, after packing. Narrow it while iterating with `FFMPEGKIT_VARIANTS=Video`.
- Run the device smoke tests when touching `Additions/`, `Transforms/`, the `.aar`/jar wiring or the consumer `.targets`:
  `FFMPEGKIT_DEVICE_RID=android-arm64 ./.github/scripts/run-device-tests.sh Video 8.1.2.4 net10.0-android36.0` — arguments are variant, packed version in `./artifacts`, target framework. The emulator or device must be `x86_64` or `arm64-v8a`.

## Hard rules

- Never commit `.aar` or `.jar` binaries. Never fetch a line with no committed SHA-256 baseline, and never weaken, bypass or skip the verification in `FetchJars.sh`.
- Never confuse the two version numbers: package versions and tags name **FFmpeg**, `.aar` file names use **FFmpegKit**, and every pairing must exist as a row in `build/native-versions.tsv`.
- Never rename `Ffmpegkit.Droid`; keep the `FFmpegKit.Net.<Variant>.Android` assembly and package ids.
- Never drop `FFMPEGKIT001` / `FFMPEGKIT002` or move them off `PrepareForBuild`, and never promise 32-bit or minSdk < 24 support.
- Keep the GPL/LGPL split exact (`-gpl` variants are `MIT AND GPL-3.0-only`, the rest `MIT AND LGPL-3.0-only`, texts packed from `licenses/`); the sample must keep referencing a non-GPL variant.
- Never point the tests or the sample at nuget.org instead of `./artifacts`; never hand-edit merged packages — fix `build/merge-packages.py`.
- Do not bypass the release `guard` job, and do not tag a version outside the mapping.

## References

- [arthenica/ffmpeg-kit Android wiki](https://github.com/arthenica/ffmpeg-kit/wiki/Android) — archived, still the reference for the Java API these bindings expose.
- [ffmpegkit-maintained/ffmpeg](https://github.com/ffmpegkit-maintained/ffmpeg) — where the binaries come from.
- Siblings [sbokatuk/FFmpegKit.iOS](https://github.com/sbokatuk/FFmpegKit.iOS) and [sbokatuk/FFmpegKit.Mac](https://github.com/sbokatuk/FFmpegKit.Mac) — same family, but a *different* native source (`sk3llo/ffmpeg_kit_flutter`); do not copy their fetch logic here.
- Umbrella [sbokatuk/FFMpegKit.Net](https://github.com/sbokatuk/FFMpegKit.Net) — re-pin it after releasing here.

Trust these instructions and search the codebase only when something here is incomplete or wrong.
