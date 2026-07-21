#!/bin/sh

set -e

# Builds and packs every FFmpegKit variant. Run Jars/FetchJars.sh first.
#
# Usage:
#   ./BuildNugets.sh                 # version from Directory.Build.props
#   ./BuildNugets.sh 8.1.7-beta.4    # explicit version
#
# Packages are written to ../artifacts.

cd "$(dirname "$0")"

VERSION="$1"
OUTPUT="../artifacts"

VERSION_ARG=""
if [ -n "$VERSION" ]; then
    VERSION_ARG="-p:Version=$VERSION"
fi

for build_type in Audio Full FullGpl Https HttpsGpl Min MinGpl Video; do
    echo "==> packing $build_type"
    dotnet pack FFmpegKit.Android.csproj \
        -c Release \
        -p:FFmpegKitBuildType="$build_type" \
        $VERSION_ARG \
        -o "$OUTPUT"
done
