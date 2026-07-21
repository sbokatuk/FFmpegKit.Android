#!/bin/sh
#
# Points FFmpegKit.Android.Example.csproj at the version of the local
# Xamarin.FFmpegKit.Full.Android package that BuildAll.sh just built, and
# clears any stale copy of that version from the local NuGet cache so the
# example always restores the freshly built .nupkg instead of an old cached
# one (NuGet caches package+version pairs and won't re-pull a matching one).
#
# Usage: ./UpdateExampleReference.sh [version]
# Defaults to 8.1.7 if no version is given.

set -e

VERSION="${1:-8.1.7}"

case "$VERSION" in
    [A-Za-z0-9]*)
        case "$VERSION" in
            *[!A-Za-z0-9._+-]*)
                echo "error: invalid version '$VERSION' (allowed characters: letters, digits, '.', '_', '+', '-')" >&2
                exit 1
                ;;
        esac
        ;;
    *)
        echo "error: invalid version '$VERSION' (must start with a letter or digit)" >&2
        exit 1
        ;;
esac

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CSPROJ="$SCRIPT_DIR/FFmpegKit.Android.Example.csproj"

sed -i.bak -E "s#(<FFmpegKitVersion[^>]*>)[^<]*(</FFmpegKitVersion>)#\1${VERSION}\2#" "$CSPROJ"
rm -f "$CSPROJ.bak"
echo "Updated FFmpegKitVersion in $(basename "$CSPROJ") to $VERSION"

rm -rf "$HOME/.nuget/packages/xamarin.ffmpegkit.full.android/$VERSION"
