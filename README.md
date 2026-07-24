# FFmpegKit.Android

[![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.Video.Android?label=nuget)](https://www.nuget.org/packages/FFmpegKit.Net.Video.Android)
[![release](https://github.com/sbokatuk/FFmpegKit.Android/actions/workflows/release.yml/badge.svg)](https://github.com/sbokatuk/FFmpegKit.Android/actions/workflows/release.yml)
[![Targets: net8.0 | net9.0 | net10.0](https://img.shields.io/badge/targets-net8.0%20%7C%20net9.0%20%7C%20net10.0-512BD4)](#installation)
[![ffmpeg 8.1.2](https://img.shields.io/badge/ffmpeg-8.1.2-632CA6)](#about)
[![Licence: MIT AND LGPL-3.0 or GPL-3.0](https://img.shields.io/badge/licence-MIT%20AND%20LGPL--3.0%20or%20GPL--3.0-orange)](#license)

.NET for Android and .NET MAUI bindings for the native **FFmpegKit** library.

> GitHub reports this repository as MIT because that is what it contains: binding source only, no native binaries. **The published packages are not MIT** — they embed native FFmpeg builds and are additionally covered by LGPL-3.0, or GPL-3.0 for the `-gpl` variants. See [License](#license).

Built against the prebuilt Android binaries from **[ffmpegkit-maintained/ffmpeg](https://github.com/ffmpegkit-maintained/ffmpeg)** — see [Where the native binaries come from](#where-the-native-binaries-come-from) for why that fork and not the original.

## About

This repository contains .NET bindings (`AndroidClassParser=class-parse`) on top of the FFmpegKit `.aar` build for Android. One project, `FFmpegKit.Android`, produces all eight package variants — the variant is selected with the `FFmpegKitBuildType` MSBuild property.

Packages target `net8.0-android34.0`, `net9.0-android35.0` and `net10.0-android36.0`.

Each .NET SDK's Android workload supports only two target frameworks — the .NET 9 band ships `Microsoft.Android.Sdk.net8` and `.net9`, the .NET 10 band ships `.net9` and `.net10` — so no single `dotnet pack` can produce all three. [`BuildNugets.sh`](src/FFmpegKit.Android/BuildNugets.sh) packs once per band and [`build/merge-packages.py`](build/merge-packages.py) merges the `lib/` trees and nuspec dependency groups into one package per variant.

### Where the native binaries come from

FFmpegKit has three relevant repositories, and only one of them still ships usable Android binaries:

| Repository | State | Prebuilt `.aar` |
| --- | --- | --- |
| [`arthenica/ffmpeg-kit`](https://github.com/arthenica/ffmpeg-kit) | archived | none — the `v5.1.LTS` release assets were deleted; Maven Central stops at `6.0.LTS` |
| [`arthenica/ffmpeg-kit-next`](https://github.com/arthenica/ffmpeg-kit-next) | active, the official continuation | none — releases up to `v8.1.0` carry source only, zero binary assets |
| [`ffmpegkit-maintained/ffmpeg`](https://github.com/ffmpegkit-maintained/ffmpeg) | active community fork | **yes** — published to Maven Central as `dev.ffmpegkit-maintained`, currently `8.1.7` |

So the `.aar` files come from the community fork, via Maven Central — see [`FetchJars.sh`](src/FFmpegKit.Android/Jars/FetchJars.sh). It keeps the original `com.arthenica.ffmpegkit` Java API, so the binding and its `Ffmpegkit.Droid` namespace are unaffected by the switch. Should `ffmpeg-kit-next` start publishing binaries, moving over would be a change to `FetchJars.sh` and `FFmpegKitNativeVersion` only.

The version is set by `FFmpegKitNativeVersion` in [`Directory.Build.props`](Directory.Build.props), which `FetchJars.sh` reads, so the download and the `.aar` the project expects cannot drift apart.

Every download is verified against a SHA-256 baseline committed at [`build/checksums/`](build/checksums) — one file per FFmpegKit line, recorded once with [`build/update-checksums.sh`](build/update-checksums.sh) (from Maven Central's own `.sha256` sidecars where published) and enforced on every fetch, locally and in CI. A corrupted or substituted artifact fails the fetch instead of ending up inside a shipped package.

The fork currently publishes three lines, each with all eight variants:

| FFmpegKit | Bundled FFmpeg | ABIs | minSdk |
| --- | --- | --- | --- |
| `6.0.3` | `n6.1.6` | `arm64-v8a`, `x86_64` | 24 |
| `7.1.6` | `n7.1.5` | `arm64-v8a`, `x86_64` | 24 |
| `8.1.7` | `n8.1.2` | `arm64-v8a`, `x86_64` | 24 |

**FFmpegKit's version is not FFmpeg's.** FFmpegKit `8.1.7` packages FFmpeg `n8.1.2`; the fork versions its own releases independently of the FFmpeg it pins. The bundled version above is read out of the shipped `libavcodec.so`, and every generated release note states it, because upstream's own notes mention other FFmpeg versions in passing and are easy to misread.

None of them ship 32-bit binaries, so dropping back to an older line does not restore `armeabi-v7a` or `x86` support.

Both constraints are surfaced at build time in consuming apps by a small `.targets` file every package carries: **`FFMPEGKIT001`** warns when `SupportedOSPlatformVersion` is below 24, **`FFMPEGKIT002`** when the build includes 32-bit runtime identifiers (`android-arm`, `android-x86`) — each otherwise only fails on a device, when `libffmpegkit.so` does not load. Both are ordinary MSBuild warnings, suppressible per project via `NoWarn` for apps that gate FFmpegKit usage at runtime.

### Releasing an older line

Tags name the FFmpeg version: **`v6.1.6.4` publishes `6.1.6.4`** and builds against the FFmpegKit release that packages FFmpeg 6.1.6, looked up in [`build/native-versions.tsv`](build/native-versions.tsv). The fourth component is the binding revision and any prerelease suffix is ignored when resolving (`v8.1.2.4-rc.1` → FFmpeg `8.1.2`). A tag naming a version that is not in the mapping fails the release with the list of known builds, so an old-style tag like `v8.1.7.4` cannot be published by mistake. No branch or `Directory.Build.props` edit is needed.

Locally, pass the native version as the second argument:

```sh
./src/FFmpegKit.Android/Jars/FetchJars.sh 6.0.3       # .aar files are named after the FFmpegKit release
./src/FFmpegKit.Android/BuildNugets.sh 6.1.6.4 6.0.3  # package version, FFmpegKit release
```

NuGet orders `8.1.2` above `6.1.6`, so publishing an older line later does not change what `dotnet add package` resolves by default.

`FFmpegKitConfig` also needs the two `smart-exception` jars at runtime. They are not bundled in the `.aar` and not declared in its `.pom`, so they are fetched separately and embedded into the binding.

## License

> This section describes what the upstream project states. It is not legal advice — if the distinction matters for your product, get it reviewed.

The C# binding code in this repository is [MIT](LICENSE). **The published NuGet packages are not**, because each one embeds native FFmpeg binaries built by [ffmpegkit-maintained/ffmpeg](https://github.com/ffmpegkit-maintained/ffmpeg), which carry their own copyleft terms. Each package therefore declares `MIT AND <native license>`:

| Package | Native license | SPDX expression |
| --- | --- | --- |
| `FFmpegKit.Net.Audio.Android` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `FFmpegKit.Net.Full.Android` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `FFmpegKit.Net.Https.Android` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `FFmpegKit.Net.Min.Android` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `FFmpegKit.Net.Video.Android` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `FFmpegKit.Net.FullGpl.Android` | **GPL-3.0** | `MIT AND GPL-3.0-only` |
| `FFmpegKit.Net.HttpsGpl.Android` | **GPL-3.0** | `MIT AND GPL-3.0-only` |
| `FFmpegKit.Net.MinGpl.Android` | **GPL-3.0** | `MIT AND GPL-3.0-only` |

The `-gpl` variants enable `x264`, `x265`, `xvid` and `vidstab`, which are GPL — upstream keeps them as separate artifacts specifically so they never contaminate the LGPL ones. Upstream's guidance is direct: **if your app is closed-source, use a non-GPL variant.**

Upstream states version 3.0 with no "or later" wording, hence the `-only` SPDX identifiers.

Every package ships the texts it is covered by under `licenses/` — `LICENSE` (MIT, the bindings) and `LGPL-3.0.txt` or `GPL-3.0.txt` (the native binaries). The same texts are in this repository under [`licenses/`](licenses). Per-dependency notices for the individual codecs (`x264`, `dav1d`, `freetype`, …) travel inside each `.aar` at `res/raw/license_*.txt` and end up in your app's resources.


## Installation
Install the package via NuGet. There are various packages depending on what you plan to use and if you require a GPL compatible package or not. These package variants match the different variants built in the FFmpegKit repository. The `-gpl` variants are GPL-3.0 — see [License](#license) before choosing one.

| Package | Link|
|------------|-----|
| FFmpegKit.Net.Audio.Android   | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.Audio.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Audio.Android) |
| FFmpegKit.Net.Full.Android   | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.Full.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Full.Android) |
| FFmpegKit.Net.FullGpl.Android   | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.FullGpl.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.FullGpl.Android) |
| FFmpegKit.Net.Https.Android   | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.Https.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Https.Android) |
| FFmpegKit.Net.HttpsGpl.Android   | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.HttpsGpl.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.HttpsGpl.Android) |
| FFmpegKit.Net.Min.Android   | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.Min.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Min.Android) |
| FFmpegKit.Net.MinGpl.Android   | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.MinGpl.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.MinGpl.Android) |
| FFmpegKit.Net.Video.Android   | [![NuGet](https://img.shields.io/nuget/v/FFmpegKit.Net.Video.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Video.Android) |


### Supported tracks

Three FFmpegKit lines are published in parallel, so you can pin to the FFmpeg generation your commands are validated against. All three are built from the same bindings and carry the same target frameworks, ABIs and licensing — only the native FFmpeg build differs. The badges above show the newest version overall, which is always the 8.x one.

| Track | FFmpeg | From FFmpegKit | Install |
| --- | --- | --- | --- |
| **`8.*`** (recommended) | `n8.1.2` | 8.1.7 | `dotnet add package FFmpegKit.Net.Video.Android --version "8.*"` |
| `7.*` | `n7.1.5` | 7.1.6 | `dotnet add package FFmpegKit.Net.Video.Android --version "7.*"` |
| `6.*` | `n6.1.6` | 6.0.3 | `dotnet add package FFmpegKit.Net.Video.Android --version "6.*"` |

A package version is **`<FFmpeg version>.<binding revision>`** — `8.1.2.4` is FFmpeg `8.1.2`, binding revision `4`. It deliberately names FFmpeg rather than FFmpegKit, because FFmpegKit's own release numbers do not track FFmpeg: FFmpegKit `8.1.7` packages FFmpeg `n8.1.2`. [`build/native-versions.tsv`](build/native-versions.tsv) maps between the two and is what the release workflow uses to turn a tag into the `.aar` to download.

A floating range therefore always resolves to the newest bindings for that FFmpeg line and never crosses into another one, which is what makes `8.*` safe to leave in a project file. Pin an exact version instead if you would rather approve every binding update yourself.

> Versions up to `8.1.7.3`, `7.1.6.3` and `6.0.3.3` used the older FFmpegKit-based numbering and are unlisted. `8.1.2.4` is not a downgrade from `8.1.7.3` — it is the same build, renamed to state the FFmpeg it contains.

Substitute any variant from the table above for `Video`. Every track ships `arm64-v8a` and `x86_64` at `minSdkVersion` 24, so choosing an older one is not a way to regain 32-bit support — see [Where the native binaries come from](#where-the-native-binaries-come-from).

For what changed in each, see the [release notes](docs/release-notes).

### Migrating from `Xamarin.FFmpegKit.*`

These packages replace the `Xamarin.FFmpegKit.*.Android` ones, which are no longer published. Change the package id and nothing else:

```diff
-<PackageReference Include="Xamarin.FFmpegKit.Video.Android" Version="4.5.1" />
+<PackageReference Include="FFmpegKit.Net.Video.Android" Version="8.1.2.4" />
```

**The `Ffmpegkit.Droid` namespace is unchanged**, so your `using` directives and calls stay as they are. It deliberately does not follow the package name: a namespace rooted at `FFmpegKit` containing a type also called `FFmpegKit` makes `FFmpegKit.Execute(...)` resolve the namespace instead of the class and fail to compile.

The assembly is now `FFmpegKit.Net.<Variant>.Android` (was `FFmpegKit.<Variant>.Android`), which matters only if you reference it by assembly name or use reflection.

Note that the native library also moved on from the archived `arthenica` builds — see [Where the native binaries come from](#where-the-native-binaries-come-from) — so upgrading from `4.5.1` is a jump from FFmpegKit 4.5 to 8.1.7, not just a repackaging.

## Usage

Include `Ffmpegkit.Droid` namespace
``` c#
using Ffmpegkit.Droid;
```

Execute your FFmpeg command

```
FFmpegKit.Execute("-i input.mov -c:v libx264 output.mp4");
```

> If your app's own root namespace starts with `FFmpegKit` (say, `FFmpegKit.MyApp`), the bare
> `FFmpegKit` above resolves to your namespace instead of the class and fails to compile — add
> `using FFmpeg = Ffmpegkit.Droid.FFmpegKit;` and call `FFmpeg.Execute(...)`, as the sample and
> the device tests do. The [migration notes](#installation) explain why the namespace is not
> `FFmpegKit.*` itself.

More examples and usage can be found in the [original FFmpegKit wiki](https://github.com/arthenica/ffmpeg-kit/wiki/Android). That repository is archived, but the Java API it documents is the one these bindings expose, so it remains the reference.

### Beyond the generated binding

The binding is a faithful projection of FFmpegKit's Java API, which is not always what you want from C#. These packages add a thin layer on top of it, in the `Ffmpegkit.Droid` namespace alongside everything else.

**Await a command instead of blocking.** `FFmpegKit.Execute` blocks for the length of the transcode, which on the UI thread means a frozen app:

```c#
var session = await FFmpegKit.ExecuteAsync("-i in.mov -c:v mpeg4 out.mp4", cancellationToken);
if (session.Succeeded()) { /* ... */ }
```

A failing command completes normally with a non-success `ReturnCode` rather than throwing, matching FFmpeg's own semantics. Cancelling asks FFmpeg to stop and the session completes with a cancelled code — a partial output file may exist.

**Report progress.** Supply the source duration and you get a percentage and an estimate:

```c#
var duration = (await FFprobeKit.GetMediaInformationAsync(input)).MediaInformation?.DurationOrNull;

await FFmpegKit.ExecuteAsync(command, new Progress<FFmpegProgress>(p =>
{
    ProgressBar.Progress = p.Percent ?? 0;   // null when no duration was supplied
}), duration);
```

Progress arrives on an FFmpegKit worker thread; `Progress<T>` marshals it back to the thread that created it.

**Read media information without parsing strings.** FFprobe reports numbers as invariant-format strings and sizes as boxed Java `Long`s. Parsing them yourself is a live bug: `double.Parse("12.345000")` returns **12,345,000** under a German locale and throws under a French one. The typed accessors parse invariantly and return `null` rather than throwing when a field is absent:

```c#
info.DurationOrNull      // TimeSpan?      (vs. Duration, a string)
info.BitrateBps          // long?
info.SizeBytes           // long?
info.TagValues           // IReadOnlyDictionary<string, string>  (vs. Tags, a Java JSONObject)

stream.PixelWidth        // int?           (vs. Width, a Java.Lang.Long)
stream.AverageFrameRateFps  // double?     evaluates "30000/1001"
stream.IsVideo / IsAudio
```

**Pass a lambda where an interface is expected**, for the log and statistics hooks:

```c#
FFmpegKitConfig.EnableLogCallback(log => Debug.WriteLine(log.Message));
```

**Use managed enums.** `SessionState` and `Level` are Java enums, so they cannot be used in a `switch`, and comparing them with `==` compares managed peer references rather than the underlying constants. `ToManaged()` converts at the boundary.

### Working with user-picked files

On Android 10 and later a file the user picks arrives as a `content://` URI, and FFmpeg cannot open one. Register it first:

```c#
var input = FFmpegKitConfig.GetSafParameterForRead(pickedUri);
await FFmpegKit.ExecuteAsync($"-i {input} -c:v mpeg4 \"{output}\"");
```

The returned value is a complete argument — do not wrap it in quotes. `GetSafParameterForWrite` does the same for output, but the document has to exist already, so create it with `ACTION_CREATE_DOCUMENT` first. Registrations last for the life of the process, so obtain them per operation rather than for every item in a long list.

MAUI's `FilePicker` copies the picked file into the cache and hands back a real path, so it needs none of this; SAF matters when you hold the raw URI, such as from a share intent or the photo picker.

### Session history

FFmpegKit keeps every session in memory, each holding its full log output, up to `FFmpegKitConfig.SessionHistorySize`. An app running many conversions will accumulate them with no obvious cause. Call `FFmpegKitConfig.ClearSessions()` when you are done with a batch, or lower the history size.


## Building

### Prerequisites

The .NET 8, 9 and 10 SDKs, each with the Android workload installed — every band supplies a different reference pack (`Ref.34`, `Ref.35`, `Ref.36`). The SDK is chosen by the `global.json` in the *working directory*, so install each band from a directory pinned to it:

```sh
for major in 8 9 10; do
  dir=$(mktemp -d) && cd "$dir"
  dotnet new globaljson --sdk-version "$(dotnet --list-sdks | grep "^${major}\." | tail -1 | cut -d' ' -f1)" --force
  dotnet workload install android
done
```

Python 3 is also needed, for the package merge step.

### All variants

```sh
./src/FFmpegKit.Android/Jars/FetchJars.sh          # downloads the .aar/.jar files
./src/FFmpegKit.Android/BuildNugets.sh             # packs all 8 variants into ./artifacts
./src/FFmpegKit.Android/BuildNugets.sh 8.1.2.4-rc.1  # ...or with an explicit package version
```

`FetchJars.sh` reads the FFmpegKit version from `FFmpegKitNativeVersion` in `Directory.Build.props`, the same property the `.csproj` uses to pick the `.aar`, so the two cannot drift apart. Pass a version to override it (`./FetchJars.sh 8.2.0`) when trying a newer upstream build before committing to it — record its checksum baseline first with `./build/update-checksums.sh 8.2.0`, since the fetch refuses to run against a line that has no committed baseline. `./FetchJars.sh --verify` re-checks the files already on disk without downloading anything.

### A single variant

```sh
# net8 + net9 assets (.NET 9 SDK, per global.json)
dotnet pack src/FFmpegKit.Android/FFmpegKit.Android.csproj \
    -c Release -p:FFmpegKitBuildType=Video -p:FFmpegKitSdkBand=net9 -o artifacts
```

`FFmpegKitBuildType` is one of `Audio`, `Full`, `FullGpl`, `Https`, `HttpsGpl`, `Min`, `MinGpl`, `Video`. `FFmpegKitSdkBand` is `net9` or `net10` and must match the SDK actually running the build. Each variant builds into its own `obj/` and `bin/` subdirectory, so they can be built in sequence without interfering with each other.

## Tests

**Package tests** run anywhere and inspect the packed `.nupkg` files — assembly present for both target frameworks, correct `.aar` for the variant, bound API surface, embedded `smart-exception` classes, nuspec metadata:

```sh
dotnet test tests/FFmpegKit.Android.PackageTests
FFMPEGKIT_VARIANTS=Video dotnet test tests/FFmpegKit.Android.PackageTests  # only what you packed
```

**Device tests** install the packed package into a small app and run real FFmpeg commands on a device or emulator (encode raw frames to mp4, probe the result, check failure reporting). They consume the package from `./artifacts` via the local feed in `NuGet.config`, so they exercise exactly what gets published:

```sh
# builds, installs, runs and reports - the same script CI uses
FFMPEGKIT_DEVICE_RID=android-arm64 \
    ./.github/scripts/run-device-tests.sh Video 8.1.2.4 net10.0-android36.0
```

Arguments are the variant, the package version in `./artifacts`, and which of the package's target frameworks to exercise. The emulator must be `x86_64` or `arm64-v8a`; the `.aar` ships no other ABIs.

## Example app

[`FFmpegKit.Android.Example`](samples/FFmpegKit.Android.Example) is a small .NET MAUI app that runs real conversions against the package you just built — resize, grayscale and audio extraction — with a before/after video preview.

```sh
./src/FFmpegKit.Android/BuildNugets.sh                    # produce ./artifacts first
dotnet build samples/FFmpegKit.Android.Example -t:Install     # deploy to a running device/emulator
```

It resolves `FFmpegKit.Net.Full.Android` from `./artifacts` through the local feed in `NuGet.config`, **not** from nuget.org, so it always exercises your local build. The version defaults to `FFmpegVersion`; pass `-p:FFmpegKitVersion=8.1.2.4-rc.1` to point it at a specific build.

It references the `Full` (LGPL) variant deliberately — swapping to a `-gpl` one would make the sample itself GPL-3.0.

The sample targets API 26 rather than the package's own floor of 24, because it previews results with CommunityToolkit's `MediaElement`, which requires 26. Your own app can still target 24.

CI builds it on every pull request and release, against the package produced by that same run. It consumes the package through a `PackageReference` exactly as you would, so it is what catches an API that no longer matches the documentation here — it is only built, never deployed, since the device tests already prove the binding runs.

It is deliberately **not** in `FFmpegKit.sln`, so that `dotnet build FFmpegKit.sln` does not require the MAUI workload.

## CI

| Workflow | Trigger | What it does |
| --- | --- | --- |
| [`pr.yml`](.github/workflows/pr.yml) | pull request | Builds and packs all 8 variants as `<version>-beta.<pr>.<run>`, runs package tests and the emulator smoke tests (net8 and net10 legs in parallel), then publishes the betas to nuget.org. Forked PRs build and test but skip publishing, since they cannot read secrets. |
| [`release.yml`](.github/workflows/release.yml) | tag `v*` | Same build and tests at the tag's version, publishes to nuget.org, then creates a GitHub release with the changelog since the previous tag and links to every package. |

Both call the reusable [`build.yml`](.github/workflows/build.yml).

Note that prereleases pushed to nuget.org cannot be deleted, only unlisted — every pull request push publishes eight packages.
