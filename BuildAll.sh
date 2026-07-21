#!/bin/sh
#
# Wrapper around FetchJars.sh and BuildNugets.sh: fetches the .aar binaries
# for the given FFmpegKit version and builds all NuGet package variants
# against them. BuildNugets.sh itself points FFmpegKit.Android.Example at the
# freshly built package, so that stays in sync even if BuildNugets.sh is run
# directly instead of through this wrapper.
#
# Usage: ./BuildAll.sh [version]
# Defaults to 8.1.7 if no version is given.

set -e

FFMPEG_KIT_VERSION="${1:-8.1.7}"

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"

cd "$SCRIPT_DIR/FFmpegKit.Android/Jars"
./FetchJars.sh "$FFMPEG_KIT_VERSION"

cd "$SCRIPT_DIR/FFmpegKit.Android"
./BuildNugets.sh "$FFMPEG_KIT_VERSION"
