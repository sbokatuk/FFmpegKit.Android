# FFmpegKit.Android

.NET for Android and .NET MAUI bindings for the native **FFmpegKit** library.

Built against the prebuilt Android binaries from **[ffmpegkit-maintained/ffmpeg](https://github.com/ffmpegkit-maintained/ffmpeg)** — see [Where the native binaries come from](#where-the-native-binaries-come-from) for why that fork and not the original.

## About

This repository contains .NET bindings (`AndroidClassParser=class-parse`) on top of the FFmpegKit `.aar` build for Android. One project, `FFmpegKit.Android`, produces all eight package variants — the variant is selected with the `FFmpegKitBuildType` MSBuild property.

Packages target `net8.0-android34.0`, `net9.0-android35.0` and `net10.0-android36.0`.

Each .NET SDK's Android workload supports only two target frameworks — the .NET 9 band ships `Microsoft.Android.Sdk.net8` and `.net9`, the .NET 10 band ships `.net9` and `.net10` — so no single `dotnet pack` can produce all three. [`BuildNugets.sh`](FFmpegKit.Android/BuildNugets.sh) packs once per band and [`build/merge-packages.py`](build/merge-packages.py) merges the `lib/` trees and nuspec dependency groups into one package per variant.

### Where the native binaries come from

FFmpegKit has three relevant repositories, and only one of them still ships usable Android binaries:

| Repository | State | Prebuilt `.aar` |
| --- | --- | --- |
| [`arthenica/ffmpeg-kit`](https://github.com/arthenica/ffmpeg-kit) | archived | none — the `v5.1.LTS` release assets were deleted; Maven Central stops at `6.0.LTS` |
| [`arthenica/ffmpeg-kit-next`](https://github.com/arthenica/ffmpeg-kit-next) | active, the official continuation | none — releases up to `v8.1.0` carry source only, zero binary assets |
| [`ffmpegkit-maintained/ffmpeg`](https://github.com/ffmpegkit-maintained/ffmpeg) | active community fork | **yes** — published to Maven Central as `dev.ffmpegkit-maintained`, currently `8.1.7` |

So the `.aar` files come from the community fork, via Maven Central — see [`FetchJars.sh`](FFmpegKit.Android/Jars/FetchJars.sh). It keeps the original `com.arthenica.ffmpegkit` Java API, so the binding and its `Ffmpegkit.Droid` namespace are unaffected by the switch. Should `ffmpeg-kit-next` start publishing binaries, moving over would be a change to `FetchJars.sh` and `FFmpegKitNativeVersion` only.

The version is set by `FFmpegKitNativeVersion` in [`Directory.Build.props`](Directory.Build.props), which `FetchJars.sh` reads, so the download and the `.aar` the project expects cannot drift apart.

The fork currently publishes three lines, each with all eight variants:

| FFmpegKit | ABIs | minSdk |
| --- | --- | --- |
| `6.0.3` | `arm64-v8a`, `x86_64` | 24 |
| `7.1.6` | `arm64-v8a`, `x86_64` | 24 |
| `8.1.7` | `arm64-v8a`, `x86_64` | 24 |

None of them ship 32-bit binaries, so dropping back to an older line does not restore `armeabi-v7a` or `x86` support.

### Releasing an older line

Package version and native version are the same number, so the tag selects both: **`v6.0.3` builds against FFmpegKit 6.0.3** and publishes `6.0.3` packages. A prerelease suffix is ignored when resolving the native version (`v7.1.6-beta.1` → native `7.1.6`), and a fourth component marks a binding-only revision (`v6.0.3.1` → native `6.0.3`). No branch or `Directory.Build.props` edit is needed.

Locally, pass the native version as the second argument:

```sh
./FFmpegKit.Android/Jars/FetchJars.sh 6.0.3     # fetch that line's .aar files
./FFmpegKit.Android/BuildNugets.sh 6.0.3 6.0.3  # package version, native version
```

NuGet orders `8.1.7` above `6.0.3`, so publishing an older line later does not change what `dotnet add package` resolves by default.

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
| FFmpegKit.Net.Audio.Android   | [![NuGet](https://img.shields.io/nuget/vpre/FFmpegKit.Net.Audio.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Audio.Android) |
| FFmpegKit.Net.Full.Android   | [![NuGet](https://img.shields.io/nuget/vpre/FFmpegKit.Net.Full.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Full.Android) |
| FFmpegKit.Net.FullGpl.Android   | [![NuGet](https://img.shields.io/nuget/vpre/FFmpegKit.Net.FullGpl.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.FullGpl.Android) |
| FFmpegKit.Net.Https.Android   | [![NuGet](https://img.shields.io/nuget/vpre/FFmpegKit.Net.Https.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Https.Android) |
| FFmpegKit.Net.HttpsGpl.Android   | [![NuGet](https://img.shields.io/nuget/vpre/FFmpegKit.Net.HttpsGpl.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.HttpsGpl.Android) |
| FFmpegKit.Net.Min.Android   | [![NuGet](https://img.shields.io/nuget/vpre/FFmpegKit.Net.Min.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Min.Android) |
| FFmpegKit.Net.MinGpl.Android   | [![NuGet](https://img.shields.io/nuget/vpre/FFmpegKit.Net.MinGpl.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.MinGpl.Android) |
| FFmpegKit.Net.Video.Android   | [![NuGet](https://img.shields.io/nuget/vpre/FFmpegKit.Net.Video.Android.svg?label=NuGet)](https://www.nuget.org/packages/FFmpegKit.Net.Video.Android) |


### Migrating from `Xamarin.FFmpegKit.*`

These packages replace the `Xamarin.FFmpegKit.*.Android` ones, which are no longer published. Change the package id and nothing else:

```diff
-<PackageReference Include="Xamarin.FFmpegKit.Video.Android" Version="4.5.1" />
+<PackageReference Include="FFmpegKit.Net.Video.Android" Version="8.1.7" />
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

More examples and usage can be found in the [original FFmpegKit wiki](https://github.com/arthenica/ffmpeg-kit/wiki/Android). That repository is archived, but the Java API it documents is the one these bindings expose, so it remains the reference.


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
./FFmpegKit.Android/Jars/FetchJars.sh          # downloads the .aar/.jar files
./FFmpegKit.Android/BuildNugets.sh             # packs all 8 variants into ./artifacts
./FFmpegKit.Android/BuildNugets.sh 8.1.7-rc.1  # ...or with an explicit package version
```

`FetchJars.sh` reads the FFmpegKit version from `FFmpegKitNativeVersion` in `Directory.Build.props`, the same property the `.csproj` uses to pick the `.aar`, so the two cannot drift apart. Pass a version to override it (`./FetchJars.sh 8.2.0`) when trying a newer upstream build before committing to it.

### A single variant

```sh
# net8 + net9 assets (.NET 9 SDK, per global.json)
dotnet pack FFmpegKit.Android/FFmpegKit.Android.csproj \
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
    ./.github/scripts/run-device-tests.sh Video 8.1.7 net10.0-android36.0
```

Arguments are the variant, the package version in `./artifacts`, and which of the package's target frameworks to exercise. The emulator must be `x86_64` or `arm64-v8a`; the `.aar` ships no other ABIs.

## Example app

[`FFmpegKit.Android.Example`](FFmpegKit.Android.Example) is a small .NET MAUI app that runs real conversions against the package you just built — resize, grayscale and audio extraction — with a before/after video preview.

```sh
./FFmpegKit.Android/BuildNugets.sh                    # produce ./artifacts first
dotnet build FFmpegKit.Android.Example -t:Install     # deploy to a running device/emulator
```

It resolves `FFmpegKit.Net.Full.Android` from `./artifacts` through the local feed in `NuGet.config`, **not** from nuget.org, so it always exercises your local build. The version defaults to `FFmpegKitNativeVersion`; pass `-p:FFmpegKitVersion=8.1.7-rc.1` to point it at a specific build.

It references the `Full` (LGPL) variant deliberately — swapping to a `-gpl` one would make the sample itself GPL-3.0.

The app is intentionally **not** part of `FFmpegKit.sln` and not built in CI: it needs the MAUI workload, which would add several minutes to every run while proving less than the device tests already do.

## CI

| Workflow | Trigger | What it does |
| --- | --- | --- |
| [`pr.yml`](.github/workflows/pr.yml) | pull request | Builds and packs all 8 variants as `<version>-beta.<pr>.<run>`, runs package tests and the emulator smoke test, then publishes the betas to nuget.org. Forked PRs build and test but skip publishing, since they cannot read secrets. |
| [`release.yml`](.github/workflows/release.yml) | tag `v*` | Same build and tests at the tag's version, publishes to nuget.org, then creates a GitHub release with the changelog since the previous tag and links to every package. |

Both call the reusable [`build.yml`](.github/workflows/build.yml).

### Publishing credentials

Publishing uses [nuget.org Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) — no long-lived API key. Each publish job requests a GitHub OIDC token (`id-token: write`), exchanges it via `NuGet/login@v1` for an API key valid for one hour, and pushes with that.

Setup on nuget.org (**Account → Trusted Publishing**): a policy binds to exactly **one** workflow file, so this repository needs **two**, identical apart from the workflow file name:

| Field | Value |
| --- | --- |
| Package Owner | `s.bokatuk` |
| Repository Owner | `sbokatuk` |
| Repository | `FFmpegKit.Android` — the name only, not a URL |
| Workflow File | `pr.yml` for one policy, `release.yml` for the other |
| Environment | `production` — must match `environment:` on the publish job |

No repository secrets are required. The workflows pass the nuget.org **profile name** (`s.bokatuk`, not an email address) to `NuGet/login`, defaulted inline since it is already public as the package author. Set a `NUGET_USER` secret to override it if the owner ever changes.

Policies created against a private repository start out active for 7 days only; they become permanent after the first successful publish, which supplies the repository and owner IDs that lock the policy down.

Note that prereleases pushed to nuget.org cannot be deleted, only unlisted — every pull request push publishes eight packages.
