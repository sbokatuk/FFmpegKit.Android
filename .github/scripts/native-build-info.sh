#!/usr/bin/env bash
set -uo pipefail

# Emits a markdown "Native build" section for the release notes, describing the FFmpegKit build
# the packages were bound against.
#
# Usage: native-build-info.sh <ffmpegkit-version> <artifacts-dir>
#
# Two facts are gathered, from two different sources on purpose:
#
#   * The FFmpeg version, read out of the shipped libavcodec.so. FFmpegKit's own version is not
#     FFmpeg's - FFmpegKit 8.1.7 packages FFmpeg n8.1.2 - and the upstream notes mention other
#     FFmpeg versions in passing ("never backported past FFmpeg 8.0"), so prose cannot be trusted
#     for this. The binary can.
#   * The upstream release summary, from the fork's GitHub release. Only the lead section, before
#     the first horizontal rule, is quoted: the rest of the body is the vendor's tier and pricing
#     table, which would read as though these packages were something you have to buy.
#
# Everything here is best-effort. A release must not fail because an upstream API call did.

FFMPEGKIT_VERSION="${1:?an FFmpegKit version is required}"
ARTIFACTS_DIR="${2:-artifacts}"

UPSTREAM_REPO="ffmpegkit-maintained/ffmpeg"
UPSTREAM_TAG="v${FFMPEGKIT_VERSION}-lts-android"

work=$(mktemp -d)
trap 'rm -rf "${work}"' EXIT

# --- FFmpeg version, from the binary we are actually shipping -------------------------------
ffmpeg_version=""
nupkg=$(find "${ARTIFACTS_DIR}" -name 'FFmpegKit.Net.Video.Android.*.nupkg' 2>/dev/null | head -1)

if [ -n "${nupkg}" ]; then
    unzip -q -o "${nupkg}" 'lib/*/ffmpeg-kit-*.aar' -d "${work}" 2>/dev/null
    aar=$(find "${work}/lib" -name 'ffmpeg-kit-*.aar' 2>/dev/null | head -1)

    if [ -n "${aar}" ]; then
        unzip -q -o "${aar}" 'jni/arm64-v8a/libavcodec.so' -d "${work}" 2>/dev/null
        so="${work}/jni/arm64-v8a/libavcodec.so"

        if [ -f "${so}" ]; then
            ffmpeg_version=$(strings -a "${so}" 2>/dev/null \
                | grep -m1 -oE 'FFmpeg version n?[0-9][0-9.]*' \
                | sed 's/^FFmpeg version //')
        fi
    fi
fi

# --- upstream release summary ----------------------------------------------------------------
upstream_json="${work}/upstream.json"
curl -fsSL --max-time 30 \
    -H 'Accept: application/vnd.github+json' \
    ${GITHUB_TOKEN:+-H "Authorization: Bearer ${GITHUB_TOKEN}"} \
    "https://api.github.com/repos/${UPSTREAM_REPO}/releases/tags/${UPSTREAM_TAG}" \
    -o "${upstream_json}" 2>/dev/null || true

echo "## Native build"
echo

if [ -n "${ffmpeg_version}" ]; then
    echo "FFmpegKit \`${FFMPEGKIT_VERSION}\`, which packages **FFmpeg \`${ffmpeg_version}\`** — the two version"
    echo "numbers are not the same thing. Read from the shipped \`libavcodec.so\`."
else
    echo "FFmpegKit \`${FFMPEGKIT_VERSION}\`. Note that this is FFmpegKit's own version, not FFmpeg's."
fi

echo

if [ -s "${upstream_json}" ]; then
    python3 - "${upstream_json}" "${UPSTREAM_TAG}" <<'PY'
import json, sys

path, tag = sys.argv[1], sys.argv[2]

try:
    release = json.load(open(path))
except Exception:
    sys.exit(0)

if not isinstance(release, dict) or 'html_url' not in release:
    sys.exit(0)

name = (release.get('name') or tag).strip()
url = release['html_url']
published = (release.get('published_at') or '')[:10]

# Lead section only - everything before the first horizontal rule. See the header comment.
body = (release.get('body') or '').replace('\r\n', '\n')
lead = body.split('\n---')[0].strip()

print(f"Built from the upstream release [{name}]({url})"
      + (f", published {published}." if published else "."))

if lead:
    print()
    print("Upstream's summary of that build:")
    print()
    for line in lead.splitlines():
        print(f"> {line}" if line.strip() else ">")
PY
else
    echo "Upstream release: <https://github.com/${UPSTREAM_REPO}/releases/tag/${UPSTREAM_TAG}>"
fi
