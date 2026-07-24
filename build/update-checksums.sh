#!/bin/sh

set -e

# Records the SHA-256 baseline that Jars/FetchJars.sh verifies every download against,
# one file per FFmpegKit line: build/checksums/<ffmpegkit version>.sha256.
#
# For the .aar files the digest is taken from Maven Central's .sha256 sidecar when one is
# published, so the recorded value comes from repository metadata rather than whatever this
# machine happened to download. When no sidecar exists the artifact itself is downloaded and
# hashed - trust-on-first-use, which still pins every later fetch to today's bytes. The
# smart-exception jars come from GitHub releases, which publish no digests, so they are
# always download-and-hash.
#
# Run this once when adopting a new upstream line (a new native-versions.tsv row), review the
# diff, and commit the result. It is deliberately not run by CI: the whole point is that CI
# verifies against a baseline a human recorded.
#
# Usage:
#   build/update-checksums.sh          # version from Directory.Build.props
#   build/update-checksums.sh 7.1.6    # an older line, e.g. when adding a native-versions.tsv row

cd "$(dirname "$0")/.."

PROPS="Directory.Build.props"
MAVEN_BASE="https://repo1.maven.org/maven2/dev/ffmpegkit-maintained"

read_property() {
    sed -n "s:.*<$1>\(.*\)</$1>.*:\1:p" "$PROPS" | head -1
}

require_version() {
    if [ -z "$2" ]; then
        echo "error: could not read $1 from $PROPS" >&2
        exit 1
    fi
    case "$2" in
        *[!A-Za-z0-9._-]*)
            echo "error: invalid $1 '$2'" >&2
            exit 1
            ;;
    esac
}

FFMPEG_KIT_VERSION="$1"
if [ -z "$FFMPEG_KIT_VERSION" ]; then
    FFMPEG_KIT_VERSION=$(read_property FFmpegKitNativeVersion)
fi
require_version FFmpegKitNativeVersion "$FFMPEG_KIT_VERSION"

SMART_EXCEPTION_VERSION=$(read_property SmartExceptionVersion)
require_version SmartExceptionVersion "$SMART_EXCEPTION_VERSION"

OUT="build/checksums/$FFMPEG_KIT_VERSION.sha256"
mkdir -p build/checksums

STAGING=$(mktemp -d)
trap 'rm -rf "$STAGING"' EXIT

sha256_of() {
    if command -v shasum >/dev/null 2>&1; then
        shasum -a 256 "$1" | awk '{print $1}'
    else
        sha256sum "$1" | awk '{print $1}'
    fi
}

# $1 = url, $2 = file name. Prints the digest.
digest_of() {
    # A sidecar is a few dozen bytes; only fall back to the full artifact when it is absent
    # or malformed.
    sidecar=$(curl -fsL --retry 3 --retry-delay 2 "$1.sha256" 2>/dev/null | awk '{print $1}' | tr '[:upper:]' '[:lower:]') || sidecar=""
    case "$sidecar" in
        *[!0-9a-f]* | "") ;;
        *)
            if [ ${#sidecar} -eq 64 ]; then
                echo "$sidecar"
                return
            fi
            ;;
    esac

    curl -fL --retry 3 --retry-delay 2 -o "$STAGING/$2" "$1" >&2
    sha256_of "$STAGING/$2"
}

: > "$STAGING/baseline"

for artifact in common java; do
    name="smart-exception-$artifact-$SMART_EXCEPTION_VERSION.jar"
    url="https://github.com/tanersener/smart-exception/releases/download/v$SMART_EXCEPTION_VERSION/$name"
    printf '%s  %s\n' "$(digest_of "$url" "$name")" "$name" >> "$STAGING/baseline"
    echo "==> $name"
done

for variant in audio full full-gpl https https-gpl min min-gpl video; do
    name="ffmpeg-kit-$variant-$FFMPEG_KIT_VERSION.aar"
    url="$MAVEN_BASE/ffmpeg-kit-$variant/$FFMPEG_KIT_VERSION/$name"
    printf '%s  %s\n' "$(digest_of "$url" "$name")" "$name" >> "$STAGING/baseline"
    echo "==> $name"
done

sort -k2 "$STAGING/baseline" > "$OUT"
echo "==> wrote $OUT"
