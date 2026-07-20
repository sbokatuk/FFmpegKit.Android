# FFmpegKit.Android

> ⚠️ **This project is under active development.** APIs, repository structure, and NuGet packages may change without notice. Use in production at your own risk.

Xamarin/.NET for Android bindings for the native **FFmpegKit** library.

Original project: **[arthenica/ffmpeg-kit-next](https://github.com/arthenica/ffmpeg-kit-next)**

## About

This repository contains .NET bindings (`.csproj` with `AndroidClassParser=class-parse`) on top of the FFmpegKit `.aar` build for Android, along with scaffolding for the following NuGet packages:

- `Xamarin.FFmpegKit.Video.Android`
- `Xamarin.FFmpegKit.FullGpl.Android`

## Build

1. Download the desired `.aar` release from [arthenica/ffmpeg-kit-next](https://github.com/arthenica/ffmpeg-kit-next/releases).
2. Place the file into `FFmpegKit.Android/Jars`.
3. Make sure the `.aar` filename referenced in `<LibraryProjectZip>` inside `FFmpegKit.Android.csproj` matches the downloaded file.
4. Open `FFmpegKit.sln` and build the project.

## License

The bindings are distributed under the [MIT](LICENSE) license. The license of the native FFmpegKit/FFmpeg library itself is defined by the original project — see [arthenica/ffmpeg-kit-next](https://github.com/arthenica/ffmpeg-kit-next).
