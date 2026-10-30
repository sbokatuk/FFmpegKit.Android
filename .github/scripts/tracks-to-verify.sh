#!/usr/bin/env bash
set -euo pipefail

# Works out which older tracks a pull request has to run on a device, and prints them as a JSON array
# for a workflow matrix: [{"ffmpeg":"7.1.5","ffmpegkit":"7.1.7"}]. Most pull requests print [].
#
#   tracks-to-verify.sh <base commit> <head commit>
#
# release.yml packs a tag and verifies nothing, on the grounds that its commit was verified on its
# pull request. That only holds for a track the pull request built, and it used to build the default
# track alone: 7.1.5.4 reached nuget.org with a freeze on x86_64, and Audio 7.1.5.4 could not load on
# API 31. So a track runs here when one of these is true:
#
#   - a release note for it is added, because merging that note is what releases it;
#   - its row in native-versions.tsv is added or replaced, because that changes the .aar it binds;
#   - the checks below change, because nothing else would try the change on the older tracks. This
#     runs every track that still ships.
#
# Not when the shared binding code changes, which is the price of running this on so few pull
# requests: a change that breaks an older track shows up on that track's next release note, still
# before it ships, rather than on the pull request that caused it.
#
# The default track is left out: pr.yml always runs it, on more variants than an older track gets.

BASE="${1:?a base commit is required}"
HEAD="${2:?a head commit is required}"

cd "$(dirname "$0")/../.."

CHECKS=(
    .github/scripts/run-device-tests.sh
    .github/scripts/run-sample-test.sh
    .github/scripts/tracks-to-verify.sh
    .github/workflows/build.yml
    .github/workflows/pr.yml
)

# Checked first because every question below is a git diff: one that cannot read its commits would
# otherwise look like a pull request with nothing to verify, and the run would be green for it.
for commit in "${BASE}" "${HEAD}"; do
    if ! git rev-parse --verify --quiet "${commit}^{commit}" > /dev/null; then
        echo "::error::'${commit}' is not a commit in this checkout" >&2
        exit 1
    fi
done

# Four parts only, the same rule auto-release.yml applies before it tags a note. Added only, too: an
# edit corrects a release that already happened.
note_tracks() {
    git diff --name-only --diff-filter=A "${BASE}" "${HEAD}" -- 'docs/release-notes/*.md' \
        | sed -nE 's:^docs/release-notes/([0-9]+\.[0-9]+\.[0-9]+)\.[0-9]+(-[0-9A-Za-z.-]+)?\.md$:\1:p'
}

# Rows start with a digit, which also skips the diff's own +++ line and any change to a comment.
row_tracks() {
    git diff --unified=0 "${BASE}" "${HEAD}" -- build/native-versions.tsv \
        | awk '/^\+[0-9]/ { print substr($1, 2) }'
}

# The newest FFmpeg of each major.minor. An older one stays in the mapping so that it can be rebuilt,
# as 8.1.2 did when 8.1.3 replaced it, but a row like that is not a track.
shipping_tracks() {
    awk '/^[[:space:]]*#/ { next } NF >= 2 { print $1 }' build/native-versions.tsv \
        | sort -t. -k1,1n -k2,2n -k3,3n \
        | awk -F. '{ newest[$1 "." $2] = $0 } END { for (track in newest) print newest[track] }'
}

notes="$(note_tracks)"
rows="$(row_tracks)"
shipping=""
if ! git diff --quiet "${BASE}" "${HEAD}" -- "${CHECKS[@]}"; then
    shipping="$(shipping_tracks)"
fi

echo "==> release notes added for:" ${notes:-nothing} >&2
echo "==> mapping rows changed for:" ${rows:-nothing} >&2
echo "==> checks changed, so every shipping track:" ${shipping:-no} >&2

default_ffmpeg="$(sed -n 's:.*<FFmpegVersion>\(.*\)</FFmpegVersion>.*:\1:p' Directory.Build.props | head -1)"

tracks=""
for ffmpeg in $(printf '%s\n' "${notes}" "${rows}" "${shipping}" | sort -u); do
    [ "${ffmpeg}" != "${default_ffmpeg}" ] || continue

    # An unmapped version fails here rather than at the release, which would already have been
    # tagged by the time it found out.
    if ! resolved="$(./build/resolve-versions.sh --ffmpeg "${ffmpeg}" 2>&1)"; then
        echo "::error::${resolved}" >&2
        exit 1
    fi
    eval "${resolved}"

    tracks="${tracks:+${tracks},}{\"ffmpeg\":\"${ffmpeg}\",\"ffmpegkit\":\"${ffmpegkit}\"}"
done

echo "[${tracks}]"
