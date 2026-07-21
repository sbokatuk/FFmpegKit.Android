# FFmpegKit.Android

> ⚠️ **This project is under active development.** APIs, repository structure, and NuGet packages may change without notice. Use in production at your own risk.

Xamarin/.NET for Android bindings for the native **FFmpegKit** library.

Original project: **[arthenica/ffmpeg-kit-next](https://github.com/arthenica/ffmpeg-kit-next)**

## About

This repository contains .NET bindings (`AndroidClassParser=class-parse`) on top of the FFmpegKit `.aar` build for Android. One project, `FFmpegKit.Android`, produces all eight package variants — the variant is selected with the `FFmpegKitBuildType` MSBuild property.

Packages target `net8.0-android34.0` and `net9.0-android35.0`.

### Where the native binaries come from

The original `arthenica/ffmpeg-kit` repository is archived and its `v5.1.LTS` release assets have been deleted; its successor `ffmpeg-kit-next` is source-only. The `.aar` files are therefore pulled from the community-maintained Maven Central mirror `dev.ffmpegkit-maintained` — see [`FetchJars.sh`](FFmpegKit.Android/Jars/FetchJars.sh). The version is set by `FFmpegKitNativeVersion` in [`Directory.Build.props`](Directory.Build.props), and `FetchJars.sh` must be kept in sync with it.

`FFmpegKitConfig` also needs the two `smart-exception` jars at runtime. They are not bundled in the `.aar` and not declared in its `.pom`, so they are fetched separately and embedded into the binding.

## License

The bindings are distributed under the [MIT](LICENSE) license. The license of the native FFmpegKit/FFmpeg library itself is defined by the original project — see [arthenica/ffmpeg-kit-next](https://github.com/arthenica/ffmpeg-kit-next).
Xamarin.Android bindings of [FFmpegKit](https://github.com/arthenica/ffmpeg-kit)


## Installation
Install the package via NuGet. There are various packages depending on what you plan to use and if you require a GPL compatible package or not. These package variants match the different variants built in the FFmpegKit repository.

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

The build needs **both** the .NET 8 and .NET 9 Android workloads. `global.json` pins the .NET 9 SDK, which builds both target frameworks — but `net8.0-android34.0` compiles against `Microsoft.Android.Ref.34`, and that pack ships only in the .NET 8 workload band.

```sh
# from a directory with no global.json, so the .NET 8 SDK is selected
dotnet workload install android
# then, from this repository
dotnet workload install android
```

### All variants

```sh
./FFmpegKit.Android/Jars/FetchJars.sh          # downloads the .aar/.jar files
./FFmpegKit.Android/BuildNugets.sh             # packs all 8 variants into ./artifacts
./FFmpegKit.Android/BuildNugets.sh 8.1.7-rc.1  # ...or with an explicit version
```

### A single variant

```sh
dotnet pack FFmpegKit.Android/FFmpegKit.Android.csproj \
    -c Release -p:FFmpegKitBuildType=Video -o artifacts
```

`FFmpegKitBuildType` is one of `Audio`, `Full`, `FullGpl`, `Https`, `HttpsGpl`, `Min`, `MinGpl`, `Video`. Each variant builds into its own `obj/` and `bin/` subdirectory, so they can be built in sequence without interfering with each other.

## Tests

**Package tests** run anywhere and inspect the packed `.nupkg` files — assembly present for both target frameworks, correct `.aar` for the variant, bound API surface, embedded `smart-exception` classes, nuspec metadata:

```sh
dotnet test tests/FFmpegKit.Android.PackageTests
FFMPEGKIT_VARIANTS=Video dotnet test tests/FFmpegKit.Android.PackageTests  # only what you packed
```

**Device tests** install the packed package into a small app and run real FFmpeg commands on a device or emulator (encode raw frames to mp4, probe the result, check failure reporting). They consume the package from `./artifacts` via the local feed in `NuGet.config`, so they exercise exactly what gets published:

```sh
dotnet build tests/FFmpegKit.Android.DeviceTests -c Release \
    -p:FFmpegKitPackageVersion=8.1.7 -t:Install
adb shell am start -n com.sbokatuk.ffmpegkit.devicetests/.MainActivity
adb logcat -s "FFmpegKitE2E:*"
```

The emulator must be `x86_64` or `arm64-v8a`; the `.aar` ships no other ABIs.

## CI

| Workflow | Trigger | What it does |
| --- | --- | --- |
| [`pr.yml`](.github/workflows/pr.yml) | pull request | Builds and packs all 8 variants as `<version>-beta.<pr>.<run>`, runs package tests and the emulator smoke test, then publishes the betas to nuget.org. Forked PRs build and test but skip publishing, since they cannot read secrets. |
| [`release.yml`](.github/workflows/release.yml) | tag `v*` | Same build and tests at the tag's version, publishes to nuget.org, then creates a GitHub release with the changelog since the previous tag and links to every package. |

Both call the reusable [`build.yml`](.github/workflows/build.yml). Publishing needs a `NUGET_API_KEY` repository secret.

Note that prereleases pushed to nuget.org cannot be deleted, only unlisted — every pull request push publishes eight packages.
