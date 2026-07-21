#!/bin/sh
#
# Updates the <version> element in every .nuspec under Nugets/ so packed
# NuGet packages carry a version matching the native FFmpegKit binaries
# they wrap.
#
# Usage: ./UpdateNuspecVersions.sh [version]
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

for nuspec in "$SCRIPT_DIR"/*/*.nuspec; do
    sed -i.bak -E "s#(<version>)[^<]*(</version>)#\1${VERSION}\2#" "$nuspec"
    rm -f "$nuspec.bak"
    echo "Updated $(basename "$nuspec") to version $VERSION"
done
