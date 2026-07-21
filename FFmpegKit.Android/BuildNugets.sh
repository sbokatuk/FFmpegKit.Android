#!/bin/sh

set -e

FFMPEG_KIT_VERSION="${1:-8.1.7}"

msbuild FFmpegKit.Android.csproj /p:Configuration=Release  /p:FFmpegKitVersion="$FFMPEG_KIT_VERSION" /p:FFmpegKitBuildType=Audio -target:Clean,Build
msbuild FFmpegKit.Android.csproj /p:Configuration=Release  /p:FFmpegKitVersion="$FFMPEG_KIT_VERSION" /p:FFmpegKitBuildType=Full -target:Clean,Build
msbuild FFmpegKit.Android.csproj /p:Configuration=Release  /p:FFmpegKitVersion="$FFMPEG_KIT_VERSION" /p:FFmpegKitBuildType=FullGpl -target:Clean,Build
msbuild FFmpegKit.Android.csproj /p:Configuration=Release  /p:FFmpegKitVersion="$FFMPEG_KIT_VERSION" /p:FFmpegKitBuildType=Https -target:Clean,Build
msbuild FFmpegKit.Android.csproj /p:Configuration=Release  /p:FFmpegKitVersion="$FFMPEG_KIT_VERSION" /p:FFmpegKitBuildType=HttpsGpl -target:Clean,Build
msbuild FFmpegKit.Android.csproj /p:Configuration=Release  /p:FFmpegKitVersion="$FFMPEG_KIT_VERSION" /p:FFmpegKitBuildType=Min -target:Clean,Build
msbuild FFmpegKit.Android.csproj /p:Configuration=Release  /p:FFmpegKitVersion="$FFMPEG_KIT_VERSION" /p:FFmpegKitBuildType=MinGpl -target:Clean,Build
msbuild FFmpegKit.Android.csproj /p:Configuration=Release  /p:FFmpegKitVersion="$FFMPEG_KIT_VERSION" /p:FFmpegKitBuildType=Video -target:Clean,Build

../Nugets/UpdateNuspecVersions.sh "$FFMPEG_KIT_VERSION"

nuget pack ../Nugets/Xamarin.FFmpegKit.Audio.Android/Xamarin.FFmpegKit.Audio.Android.nuspec -Symbols -SymbolPackageFormat snupkg
nuget pack ../Nugets/Xamarin.FFmpegKit.Full.Android/Xamarin.FFmpegKit.Full.Android.nuspec -Symbols -SymbolPackageFormat snupkg
nuget pack ../Nugets/Xamarin.FFmpegKit.FullGpl.Android/Xamarin.FFmpegKit.FullGpl.Android.nuspec -Symbols -SymbolPackageFormat snupkg
nuget pack ../Nugets/Xamarin.FFmpegKit.Https.Android/Xamarin.FFmpegKit.Https.Android.nuspec -Symbols -SymbolPackageFormat snupkg
nuget pack ../Nugets/Xamarin.FFmpegKit.HttpsGpl.Android/Xamarin.FFmpegKit.HttpsGpl.Android.nuspec -Symbols -SymbolPackageFormat snupkg
nuget pack ../Nugets/Xamarin.FFmpegKit.Min.Android/Xamarin.FFmpegKit.Min.Android.nuspec -Symbols -SymbolPackageFormat snupkg
nuget pack ../Nugets/Xamarin.FFmpegKit.MinGpl.Android/Xamarin.FFmpegKit.MinGpl.Android.nuspec -Symbols -SymbolPackageFormat snupkg
nuget pack ../Nugets/Xamarin.FFmpegKit.Video.Android/Xamarin.FFmpegKit.Video.Android.nuspec -Symbols -SymbolPackageFormat snupkg

../FFmpegKit.Android.Example/UpdateExampleReference.sh "$FFMPEG_KIT_VERSION"
