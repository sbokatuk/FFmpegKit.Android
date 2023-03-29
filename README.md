# FFmpegKit.Android
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


## Building with build scripts (macOS tested)
1. Navigate to the Jars directory in terminal
2. Run FetchJars.sh 
    ``` sh
    $ ./FetchJars.sh
    ```
3. Go back up one directory
4. Run BuildNugets.sh
    ``` sh
    $ ./BuildNugets.sh
    ```
5. This will now create nupkg packages of all 8 variants of FFmpegKit.Android.

Tip: If you only want to build one variant of FFmpegKit.Android and its nuget packages, comment out other lines in `FetchJars.sh` and `BuildNugets.sh`.

## Building manually
1. Download `smart-exception-common-0.2.1.jar` and `smart-exception-java-0.2.1.jar` from the [smart-exception](https://github.com/tanersener/smart-exception/) repository.
2. Place in `Jars` folder.
3. Download `ffmpeg-kit-audio-5.1.LTS.aar`, `ffmpeg-kit-full-5.1.LTS.aar`, `ffmpeg-kit-full-gpl-5.1.LTS.aar`, `ffmpeg-kit-https-5.1.LTS.aar`, `ffmpeg-kit-https-gpl-5.1.LTS.aar`, `ffmpeg-kit-min-5.1.LTS.aar`, `ffmpeg-kit-min-gpl-5.1.LTS.aar` and `ffmpeg-kit-video-5.1.LTS.aar` from the [FFmpegKit](https://github.com/arthenica/ffmpeg-kit/) repository from the releases tab, under LTS build. 
NOTE: If you only intend to build one binding then you only need to download that one aar file.
4. Place in `Jars` folder.
5. In the directory relative to the csproj file run the build command
```
msbuild FFmpegKit.Android.csproj /p:Configuration=Release  /p:FFmpegKitBuildType={TYPE} -target:Clean,Build
```
where `{TYPE}` is the FFmpegKit variant you are building. Possible options are `Audio`, `Full`, `FullGpl`, `Https`, `HttpsGpl`, `Min`, `MinGpl` or `Video`.

6. To build the nuget package run the pack command
```
nuget pack ../Nugets/Xamarin.FFmpegKit.{TYPE}.Android/Xamarin.FFmpegKit.{TYPE}.Android.nuspec -Symbols -SymbolPackageFormat snupkg
```
where type is the same as from the previous step.