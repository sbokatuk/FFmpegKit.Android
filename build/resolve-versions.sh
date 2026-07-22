#!/bin/sh

set -e

# Resolves the pair of versions a build needs, from either half of the pair.
#
#   resolve-versions.sh --ffmpeg 8.1.2      -> ffmpeg=8.1.2
#                                              ffmpegkit=8.1.7
#   resolve-versions.sh --ffmpegkit 8.1.7   -> same
#
# Output is shell-eval friendly, so callers can do:  eval "$(resolve-versions.sh --ffmpeg 8.1.2)"
#
# Package versions are based on the FFmpeg version; the FFmpegKit version names the .aar to
# download. build/native-versions.tsv is the single source for the mapping.

MAP="$(cd "$(dirname "$0")" && pwd)/native-versions.tsv"

usage() {
    echo "usage: resolve-versions.sh --ffmpeg <version> | --ffmpegkit <version>" >&2
    exit 2
}

[ $# -eq 2 ] || usage
[ -f "$MAP" ] || { echo "error: mapping file not found: $MAP" >&2; exit 1; }

case "$2" in
    ''|*[!0-9.]*) echo "error: '$2' is not a version" >&2; exit 1 ;;
esac

case "$1" in
    --ffmpeg)    column=1 ;;
    --ffmpegkit) column=2 ;;
    *) usage ;;
esac

row=$(awk -v want="$2" -v col="$column" '
    /^[[:space:]]*#/ { next }
    NF < 2 { next }
    $col == want { print $1 "\t" $2; exit }
' "$MAP")

if [ -z "$row" ]; then
    echo "error: no mapping for $1 $2 in $(basename "$MAP"). Known builds:" >&2
    awk '/^[[:space:]]*#/ { next } NF >= 2 { printf "  FFmpeg %-8s <- FFmpegKit %s\n", $1, $2 }' "$MAP" >&2
    exit 1
fi

echo "ffmpeg=$(printf '%s' "$row" | cut -f1)"
echo "ffmpegkit=$(printf '%s' "$row" | cut -f2)"
