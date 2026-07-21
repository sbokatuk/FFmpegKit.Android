# FFmpegKit.Android

> ⚠️ **This project is under active development.** APIs, repository structure, and NuGet packages may change without notice. Use in production at your own risk.

Xamarin/.NET for Android bindings for the native **FFmpegKit** library.

Original project: **[ffmpegkit-maintained/ffmpeg](https://github.com/ffmpegkit-maintained/ffmpeg)**

## About

This repository contains .NET bindings (`.csproj` with `AndroidClassParser=class-parse`) on top of the FFmpegKit `.aar` build for Android, along with scaffolding for the following NuGet packages.

## Installation

Install the package that matches what you need via NuGet (`dotnet add package <name>` or a `<PackageReference Include="<name>" Version="..." />` in your `.csproj`). The variants mirror the different native FFmpegKit builds:

- `Audio`/`Video`/`Min`/`Https`/`Full` are non-GPL builds (safe to use in closed-source apps).
- `MinGpl`/`HttpsGpl`/`FullGpl` additionally bundle GPL-licensed codecs (e.g. `x264`); using them means your app must comply with the GPL for the parts that link against FFmpegKit.

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

Include the `Ffmpegkit.Droid` namespace:
```c#
using Ffmpegkit.Droid;
```

Execute your FFmpeg command:
```c#
FFmpegKit.Execute("-i input.mov -c:v libx264 output.mp4");
```

More examples and usage can be found in the [FFmpegKit wiki](https://github.com/arthenica/ffmpeg-kit/wiki/Android).

## A note on binary sources

`arthenica/ffmpeg-kit` is archived and its `v5.1.LTS` release assets have been deleted from GitHub. Its successor, `ffmpeg-kit-next`, is source-only (no prebuilt binaries since v6.1.0). Because of this, the `.aar` files used to build these bindings are fetched from the community-maintained mirror **[dev.ffmpegkit-maintained](https://repo1.maven.org/maven2/dev/ffmpegkit-maintained/)** on Maven Central, currently pinned to version **8.1.7**.

## Building with build scripts (macOS tested)

The simplest way to build everything is the top-level wrapper script, which fetches the `.aar` binaries for a given FFmpegKit version and then builds all NuGet package variants against them:

```sh
$ ./BuildAll.sh 8.1.7
```

The version argument is optional and defaults to `8.1.7`. Under the hood it just runs, in order:

```sh
cd FFmpegKit.Android/Jars
./FetchJars.sh 8.1.7
cd ..
./BuildNugets.sh 8.1.7
```

- `FetchJars.sh` downloads the `.aar` files for the given version (plus the smart-exception jars) into `Jars/`.
- `BuildNugets.sh` builds all 8 variants (passing `/p:FFmpegKitVersion=8.1.7` to `msbuild` so the `.csproj` picks up the matching `.aar` filenames), runs `Nugets/UpdateNuspecVersions.sh 8.1.7` to stamp the same version into every `.nuspec`'s `<version>` element before packing the NuGet packages, and finally runs `FFmpegKit.Android.Example/UpdateExampleReference.sh 8.1.7` to point the example app (see below) at the version just built. Because this last step lives in `BuildNugets.sh` itself, the example stays in sync even if you run `BuildNugets.sh` directly instead of through `BuildAll.sh`.

This will create nupkg packages of all 8 variants of FFmpegKit.Android.

Tip: If you only want to build one variant of FFmpegKit.Android and its NuGet package, comment out the other lines in `FetchJars.sh` and `BuildNugets.sh`.

## Building manually

1. Download `smart-exception-common-0.2.1.jar` and `smart-exception-java-0.2.1.jar` from the [smart-exception](https://github.com/tanersener/smart-exception/) repository. Place them in the `Jars` folder.
2. Download the `.aar` variants you need for the version you want to build (e.g. `ffmpeg-kit-audio-8.1.7.aar`, `ffmpeg-kit-full-8.1.7.aar`, `ffmpeg-kit-full-gpl-8.1.7.aar`, `ffmpeg-kit-https-8.1.7.aar`, `ffmpeg-kit-https-gpl-8.1.7.aar`, `ffmpeg-kit-min-8.1.7.aar`, `ffmpeg-kit-min-gpl-8.1.7.aar`, `ffmpeg-kit-video-8.1.7.aar`) from [dev.ffmpegkit-maintained on Maven Central](https://repo1.maven.org/maven2/dev/ffmpegkit-maintained/). Place them in the `Jars` folder.

   NOTE: If you only intend to build one binding, you only need to download that one `.aar` file.
3. From the directory containing the `.csproj` file, run the build command:
    ```
    msbuild FFmpegKit.Android.csproj /p:Configuration=Release /p:FFmpegKitVersion={VERSION} /p:FFmpegKitBuildType={TYPE} -target:Clean,Build
    ```
    where `{TYPE}` is the FFmpegKit variant you are building (`Audio`, `Full`, `FullGpl`, `Https`, `HttpsGpl`, `Min`, `MinGpl` or `Video`) and `{VERSION}` is the FFmpegKit version whose `.aar` you downloaded (defaults to `8.1.7` if omitted).
4. To build the NuGet package, run the pack command:
    ```
    nuget pack ../Nugets/Xamarin.FFmpegKit.{TYPE}.Android/Xamarin.FFmpegKit.{TYPE}.Android.nuspec -Symbols -SymbolPackageFormat snupkg
    ```
    where `{TYPE}` is the same as in the previous step.

## Example app

`FFmpegKit.Android.Example` is a minimal .NET MAUI app (Android only, `net8.0-android`) used to smoke-test the built NuGet package: it bundles a small sample video, lets you pick a conversion, runs it with FFmpegKit, and plays the original and the result side by side (via `CommunityToolkit.Maui.MediaElement`) so you can compare them.

> ⚠️ **The example does not use a package from nuget.org.** It restores `Xamarin.FFmpegKit.Full.Android` from a local `.nupkg` that `BuildNugets.sh` produces (see below), so **you must run `./BuildAll.sh` (or `FFmpegKit.Android/BuildNugets.sh`) at least once before the example will restore or build.** Without that `.nupkg` present, `dotnet restore`/`dotnet build` on the example fails with a NuGet "unable to find package" error.

Available conversions (kept minimal on purpose):
- **Resize to 160x120** — `-vf scale=160:120 -c:v mpeg4 -c:a aac`
- **Grayscale** — `-vf hue=s=0 -c:v mpeg4 -c:a aac`
- **Extract audio only (AAC)** — `-vn -c:a aac`

It references the **`Xamarin.FFmpegKit.Full.Android`** package directly from the local build output rather than nuget.org:

- `NuGet.Config` in the example project adds a local package source, `LocalFFmpegKit`, pointing at `../FFmpegKit.Android` — the folder `BuildNugets.sh` packs the `.nupkg` into.
- The `<PackageReference>` version is driven by the `FFmpegKitVersion` MSBuild property (default `8.1.7`).

After `BuildNugets.sh` packs a version, it calls `FFmpegKit.Android.Example/UpdateExampleReference.sh <version>` to update `FFmpegKitVersion` in the example's `.csproj` to match, and clears any stale copy of that version from the local NuGet cache (`~/.nuget/packages`) so the example always restores the freshly built `.nupkg` instead of an old cached one.

To run it:
```sh
./BuildAll.sh                          # 1. build the local Xamarin.FFmpegKit.Full.Android .nupkg first
cd FFmpegKit.Android.Example
dotnet build -t:Run -f net8.0-android   # 2. only then restore/run the example
```

## License

The bindings are distributed under the [MIT](LICENSE) license. The license of the native FFmpegKit/FFmpeg library itself is defined by the original project — see [ffmpegkit-maintained/ffmpeg](https://github.com/ffmpegkit-maintained/ffmpeg).
