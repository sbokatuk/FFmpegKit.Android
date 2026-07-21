# FFmpegKit.Android

> ⚠️ **This project is under active development.** APIs, repository structure, and NuGet packages may change without notice. Use in production at your own risk.

Xamarin/.NET for Android bindings for the native **FFmpegKit** library.

Original project: **[arthenica/ffmpeg-kit-next](https://github.com/arthenica/ffmpeg-kit-next)**

## About

This repository contains .NET bindings (`AndroidClassParser=class-parse`) on top of the FFmpegKit `.aar` build for Android. One project, `FFmpegKit.Android`, produces all eight package variants — the variant is selected with the `FFmpegKitBuildType` MSBuild property.

Packages target `net8.0-android34.0`, `net9.0-android35.0` and `net10.0-android36.0`.

Each .NET SDK's Android workload supports only two target frameworks — the .NET 9 band ships `Microsoft.Android.Sdk.net8` and `.net9`, the .NET 10 band ships `.net9` and `.net10` — so no single `dotnet pack` can produce all three. [`BuildNugets.sh`](FFmpegKit.Android/BuildNugets.sh) packs once per band and [`build/merge-packages.py`](build/merge-packages.py) merges the `lib/` trees and nuspec dependency groups into one package per variant.

### Where the native binaries come from

The original `arthenica/ffmpeg-kit` repository is archived and its `v5.1.LTS` release assets have been deleted; its successor `ffmpeg-kit-next` is source-only. The `.aar` files are therefore pulled from the community-maintained Maven Central mirror `dev.ffmpegkit-maintained` — see [`FetchJars.sh`](FFmpegKit.Android/Jars/FetchJars.sh). The version is set by `FFmpegKitNativeVersion` in [`Directory.Build.props`](Directory.Build.props), and `FetchJars.sh` must be kept in sync with it.

`FFmpegKitConfig` also needs the two `smart-exception` jars at runtime. They are not bundled in the `.aar` and not declared in its `.pom`, so they are fetched separately and embedded into the binding.

## License

> This section describes what the upstream project states. It is not legal advice — if the distinction matters for your product, get it reviewed.

The C# binding code in this repository is [MIT](LICENSE). **The published NuGet packages are not**, because each one embeds native FFmpeg binaries built by [ffmpegkit-maintained/ffmpeg](https://github.com/ffmpegkit-maintained/ffmpeg), which carry their own copyleft terms. Each package therefore declares `MIT AND <native license>`:

| Package | Native license | SPDX expression |
| --- | --- | --- |
| `Xamarin.FFmpegKit.Audio.Android` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `Xamarin.FFmpegKit.Full.Android` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `Xamarin.FFmpegKit.Https.Android` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `Xamarin.FFmpegKit.Min.Android` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `Xamarin.FFmpegKit.Video.Android` | LGPL-3.0 | `MIT AND LGPL-3.0-only` |
| `Xamarin.FFmpegKit.FullGpl.Android` | **GPL-3.0** | `MIT AND GPL-3.0-only` |
| `Xamarin.FFmpegKit.HttpsGpl.Android` | **GPL-3.0** | `MIT AND GPL-3.0-only` |
| `Xamarin.FFmpegKit.MinGpl.Android` | **GPL-3.0** | `MIT AND GPL-3.0-only` |

The `-gpl` variants enable `x264`, `x265`, `xvid` and `vidstab`, which are GPL — upstream keeps them as separate artifacts specifically so they never contaminate the LGPL ones. Upstream's guidance is direct: **if your app is closed-source, use a non-GPL variant.**

Upstream states version 3.0 with no "or later" wording, hence the `-only` SPDX identifiers.

Every package ships the texts it is covered by under `licenses/` — `LICENSE` (MIT, the bindings) and `LGPL-3.0.txt` or `GPL-3.0.txt` (the native binaries). The same texts are in this repository under [`licenses/`](licenses). Per-dependency notices for the individual codecs (`x264`, `dav1d`, `freetype`, …) travel inside each `.aar` at `res/raw/license_*.txt` and end up in your app's resources.


## Installation
Install the package via NuGet. There are various packages depending on what you plan to use and if you require a GPL compatible package or not. These package variants match the different variants built in the FFmpegKit repository. The `-gpl` variants are GPL-3.0 — see [License](#license) before choosing one.

| Package | Link|
|------------|-----|
| Xamarin.FFmpegKit.Audio.Android   | [![NuGet](https://img.shields.io/nuget/vpre/Xamarin.FFmpegKit.Audio.Android.svg?label=NuGet)](https://www.nuget.org/packages/Xamarin.FFmpegKit.Audio.Android) |
| Xamarin.FFmpegKit.Full.Android   | [![NuGet](https://img.shields.io/nuget/vpre/Xamarin.FFmpegKit.Full.Android.svg?label=NuGet)](https://www.nuget.org/packages/Xamarin.FFmpegKit.Full.Android) |
| Xamarin.FFmpegKit.FullGpl.Android   | [![NuGet](https://img.shields.io/nuget/vpre/Xamarin.FFmpegKit.FullGpl.Android.svg?label=NuGet)](https://www.nuget.org/packages/Xamarin.FFmpegKit.FullGpl.Android) |
| Xamarin.FFmpegKit.Https.Android   | [![NuGet](https://img.shields.io/nuget/vpre/Xamarin.FFmpegKit.Https.Android.svg?label=NuGet)](https://www.nuget.org/packages/Xamarin.FFmpegKit.Https.Android) |
| Xamarin.FFmpegKit.HttpsGpl.Android   | [![NuGet](https://img.shields.io/nuget/vpre/Xamarin.FFmpegKit.HttpsGpl.Android.svg?label=NuGet)](https://www.nuget.org/packages/Xamarin.FFmpegKit.HttpsGpl.Android) |
| Xamarin.FFmpegKit.Min.Android   | [![NuGet](https://img.shields.io/nuget/vpre/Xamarin.FFmpegKit.Min.Android.svg?label=NuGet)](https://www.nuget.org/packages/Xamarin.FFmpegKit.Min.Android) |
| Xamarin.FFmpegKit.MinGpl.Android   | [![NuGet](https://img.shields.io/nuget/vpre/Xamarin.FFmpegKit.MinGpl.Android.svg?label=NuGet)](https://www.nuget.org/packages/Xamarin.FFmpegKit.MinGpl.Android) |
| Xamarin.FFmpegKit.Video.Android   | [![NuGet](https://img.shields.io/nuget/vpre/Xamarin.FFmpegKit.Video.Android.svg?label=NuGet)](https://www.nuget.org/packages/Xamarin.FFmpegKit.Video.Android) |


## Usage

Include `Ffmpegkit.Droid` namespace
``` c#
using Ffmpegkit.Droid;
```

Execute your FFmpeg command

```
FFmpegKit.Execute("-i input.mov -c:v libx264 output.mp4");
```

More examples and usage can be found in the [FFmpegKit wiki](https://github.com/arthenica/ffmpeg-kit/wiki/Android).


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
./FFmpegKit.Android/BuildNugets.sh 8.1.7-rc.1  # ...or with an explicit version
```

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
