#!/bin/sh

set -e

# Downloads the native FFmpegKit binaries the binding is built against.
#
# NOTE: the original arthenica/ffmpeg-kit repo is archived and its v5.1.LTS release
# assets have been deleted from GitHub. Its successor, ffmpeg-kit-next, is source-only
# (no prebuilt binaries since v6.1.0). The .aar files below instead come from the
# community-maintained Maven Central mirror at dev.ffmpegkit-maintained.
#
# Every downloaded file is verified against the SHA-256 baseline committed at
# build/checksums/<ffmpegkit version>.sha256. These are tens of megabytes of native code
# that get linked into consumers' apps: a corrupted or substituted artifact must fail
# here, not at packaging time or - worse - silently on devices. Baselines are recorded
# by build/update-checksums.sh when a new upstream line is adopted.
#
# Usage:
#   ./FetchJars.sh                  # version from Directory.Build.props
#   ./FetchJars.sh 8.2.0            # override, e.g. to try a newer upstream build
#   ./FetchJars.sh --verify         # no downloads: check the files already present here
#   ./FetchJars.sh --verify 8.2.0

cd "$(dirname "$0")"

ROOT="$(cd ../../.. && pwd)"
PROPS="$ROOT/Directory.Build.props"
MAVEN_BASE="https://repo1.maven.org/maven2/dev/ffmpegkit-maintained"

VERIFY_ONLY=0
FFMPEG_KIT_VERSION=""
for arg in "$@"; do
    case "$arg" in
        --verify) VERIFY_ONLY=1 ;;
        -*) echo "error: unknown option '$arg'" >&2; exit 1 ;;
        *) FFMPEG_KIT_VERSION="$arg" ;;
    esac
done

read_property() {
    sed -n "s:.*<$1>\(.*\)</$1>.*:\1:p" "$PROPS" | head -1
}

# Versions are interpolated into URLs and file names, so reject anything exotic up front.
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

# Read the version from Directory.Build.props rather than repeating it here: the .aar file names
# are baked into the .csproj via FFmpegKitNativeVersion, so a second copy that drifts out of sync
# fails the build with a confusing "file not found" on an .aar nobody downloaded.
if [ -z "$FFMPEG_KIT_VERSION" ]; then
    FFMPEG_KIT_VERSION=$(read_property FFmpegKitNativeVersion)
fi
require_version FFmpegKitNativeVersion "$FFMPEG_KIT_VERSION"

# The smart-exception version lives in Directory.Build.props too: the .csproj embeds the jars
# via the same property, so there is exactly one place to bump and nothing to drift. A wrong
# pairing here fails only on device, with NoClassDefFoundError.
SMART_EXCEPTION_VERSION=$(read_property SmartExceptionVersion)
require_version SmartExceptionVersion "$SMART_EXCEPTION_VERSION"

CHECKSUMS="$ROOT/build/checksums/$FFMPEG_KIT_VERSION.sha256"
if [ ! -f "$CHECKSUMS" ]; then
    echo "error: no checksum baseline for FFmpegKit $FFMPEG_KIT_VERSION" >&2
    echo "       expected $CHECKSUMS" >&2
    echo "       run build/update-checksums.sh $FFMPEG_KIT_VERSION to record one" >&2
    exit 1
fi

sha256_of() {
    if command -v shasum >/dev/null 2>&1; then
        shasum -a 256 "$1" | awk '{print $1}'
    else
        sha256sum "$1" | awk '{print $1}'
    fi
}

# $1 = path to check, $2 = file name as recorded in the baseline
verify_file() {
    want=$(awk -v f="$2" '$2 == f { print $1; exit }' "$CHECKSUMS")
    if [ -z "$want" ]; then
        echo "error: $CHECKSUMS has no entry for $2" >&2
        echo "       run build/update-checksums.sh $FFMPEG_KIT_VERSION to record it" >&2
        exit 1
    fi
    got=$(sha256_of "$1")
    if [ "$got" != "$want" ]; then
        echo "error: SHA-256 mismatch for $2" >&2
        echo "       expected $want" >&2
        echo "       got      $got" >&2
        exit 1
    fi
}

# Everything one FFmpegKit line consists of: the eight variant .aars plus the two
# smart-exception jars FFmpegKitConfig's static initialiser needs at runtime.
expected_files() {
    for variant in audio full full-gpl https https-gpl min min-gpl video; do
        echo "ffmpeg-kit-$variant-$FFMPEG_KIT_VERSION.aar"
    done
    for artifact in common java; do
        echo "smart-exception-$artifact-$SMART_EXCEPTION_VERSION.jar"
    done
}

if [ "$VERIFY_ONLY" = 1 ]; then
    for file in $(expected_files); do
        if [ ! -f "$file" ]; then
            echo "error: $file is missing - run FetchJars.sh to download it" >&2
            exit 1
        fi
        verify_file "$file" "$file"
    done
    echo "==> verified $(expected_files | wc -l | tr -d ' ') files against $CHECKSUMS"
    exit 0
fi

echo "==> fetching FFmpegKit $FFMPEG_KIT_VERSION"

# Downloaded into a staging directory and moved into place only once every file has arrived and
# verified. Deleting first and downloading second leaves the working copy with no binaries at all
# when a version turns out not to exist, which costs a 200 MB re-download to recover from.
STAGING=$(mktemp -d)
trap 'rm -rf "$STAGING"' EXIT

# FFmpegKitConfig's static initialiser calls com.arthenica.smartexception.java.Exceptions, which
# is not bundled in the .aar and is not declared in its .pom. Without these two jars every
# FFmpeg call fails at runtime with NoClassDefFoundError.
for artifact in common java; do
    curl -fL --retry 3 --retry-delay 2 --output-dir "$STAGING" -O \
        "https://github.com/tanersener/smart-exception/releases/download/v$SMART_EXCEPTION_VERSION/smart-exception-$artifact-$SMART_EXCEPTION_VERSION.jar"
done

for variant in audio full full-gpl https https-gpl min min-gpl video; do
    curl -fL --retry 3 --retry-delay 2 --output-dir "$STAGING" -O \
        "$MAVEN_BASE/ffmpeg-kit-$variant/$FFMPEG_KIT_VERSION/ffmpeg-kit-$variant-$FFMPEG_KIT_VERSION.aar"
done

for file in $(expected_files); do
    verify_file "$STAGING/$file" "$file"
done

rm -f ./*.jar
rm -f ./*.aar
mv "$STAGING"/*.jar "$STAGING"/*.aar .

echo "==> fetched and verified $(ls ./*.aar | wc -l | tr -d ' ') .aar and $(ls ./*.jar | wc -l | tr -d ' ') .jar files"
