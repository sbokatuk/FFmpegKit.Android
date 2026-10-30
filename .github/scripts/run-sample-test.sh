#!/usr/bin/env bash
set -euo pipefail

# Installs the MAUI sample against a packed FFmpegKit package and uses it the way a person would, on
# the emulator that the calling workflow step has already booted: launch it, tap the convert button
# and wait for the success text. Published packages have frozen this app on its first conversion
# while every device check passed, so this runs the app itself rather than another check beside it.

VERSION="${1:?a package version is required}"
TARGET_FRAMEWORK="${2:-net10.0-android36.0}"

PACKAGE_NAME="com.sbokatuk.ffmpegkit.example"
LOG_FILE="sample-test-logcat.txt"
SCREENSHOT_FILE="sample-test-screen.png"
# CI emulators are x86_64; override when running this against a local arm64 emulator or device.
DEVICE_RID="${FFMPEGKIT_DEVICE_RID:-android-x64}"
# Seconds. Generous: a first launch on a CI emulator renders in software.
LAUNCH_TIMEOUT=180
CONVERSION_TIMEOUT=120
# An ANR needs an input event the app has not taken for 5 seconds, so this has to outlast that.
ANR_WATCH=15
# Must be a power of two; see where it is used.
GC_NURSERY="${FFMPEGKIT_GC_NURSERY:-64k}"

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PROJECT="${REPO_ROOT}/samples/FFmpegKit.Android.Example/FFmpegKit.Android.Example.csproj"
UI_DUMP="$(mktemp)"

# The same SDK band selection as run-device-tests.sh; see the comment there. The sample also needs
# the band named, because it builds exactly one target framework per band.
case "${TARGET_FRAMEWORK}" in
    net10.0-*) sdk_major=10 ;;
    net8.0-*)  sdk_major=8 ;;
    *)         sdk_major=9 ;;
esac

sdk_version="$(dotnet --list-sdks | grep "^${sdk_major}\." | tail -1 | cut -d' ' -f1)"
if [ -z "${sdk_version}" ]; then
    echo "::error::no .NET ${sdk_major} SDK installed, cannot build ${TARGET_FRAMEWORK}"
    exit 1
fi

SDK_DIR="$(mktemp -d)"
printf '{ "sdk": { "version": "%s", "rollForward": "latestFeature" } }\n' "${sdk_version}" \
    > "${SDK_DIR}/global.json"

# A system dialog over the sample would swallow the taps, and a slow emulator can raise "System UI
# isn't responding" by itself. With dialogs hidden the system still logs every ANR, and kills the
# app that caused it, so nothing below loses sight of one. Put back afterwards because this can be
# a local device.
previous_hide_dialogs="$(adb shell settings get global hide_error_dialogs)"
adb shell settings put global hide_error_dialogs 1

# Kept however the run ends: what the screen showed, and the whole log rather than a filtered one,
# because what kills the app is rarely under the app's own tag. An ANR's stack traces go to dropbox
# rather than logcat, and they are what names the stuck thread.
finish() {
    adb exec-out screencap -p > "${SCREENSHOT_FILE}" || true
    {
        adb logcat -d
        echo "==> ANR traces"
        adb shell dumpsys dropbox --print data_app_anr
    } > "${LOG_FILE}" || true

    rm -rf "${SDK_DIR}" "${UI_DUMP}"
    if [ "${previous_hide_dialogs}" = "null" ]; then
        adb shell settings delete global hide_error_dialogs > /dev/null || true
    else
        adb shell settings put global hide_error_dialogs "${previous_hide_dialogs}" || true
    fi
}
trap finish EXIT

fail() {
    echo "::error::FFmpegKit sample: $1"
    exit 1
}

# The checks every wait repeats, so that a dead app fails at once rather than at the timeout. grep
# without -q: under pipefail its early exit would kill adb and turn a match into a failure.
require_running() {
    if [ -z "$(adb shell pidof "${PACKAGE_NAME}")" ]; then
        fail "the app died $1"
    fi
}

require_healthy() {
    # Matches WindowManager's "ANR in Window{... <package>...}" too, which comes about ten seconds
    # before ActivityManager's "ANR in <package>".
    if adb logcat -d | grep -E "ANR in .*${PACKAGE_NAME}" > /dev/null; then
        fail "ANR $1"
    fi
    require_running "$1"
}

# uiautomator refuses while the UI is busy ("could not get idle state"), and the spinner during a
# conversion makes that common, so a failed dump is not a verdict: the caller just asks again. It
# can also hang on a frozen app, hence the timeout.
dump_ui() {
    adb shell "timeout 30 uiautomator dump /data/local/tmp/sample-ui.xml" > /dev/null 2>&1 \
        && adb exec-out cat /data/local/tmp/sample-ui.xml > "${UI_DUMP}"
}

screen_shows() {
    dump_ui && grep -q "$1" "${UI_DUMP}"
}

# Takes the bounds from the dump instead of fixed coordinates, so that a different screen or font
# cannot move a tap off its target. Reads the latest dump, so it follows a screen_shows.
tap_node() {
    local left top right bottom
    # bounds="[left,top][right,bottom]"
    read -r left top right bottom < <(grep -o "<node [^>]*$1[^>]*>" "${UI_DUMP}" \
        | sed -n 's/.*bounds="\[\([0-9]*\),\([0-9]*\)\]\[\([0-9]*\),\([0-9]*\)\]".*/\1 \2 \3 \4/p') \
        || fail "$1 is not on screen"
    adb shell input tap "$(( (left + right) / 2 ))" "$(( (top + bottom) / 2 ))"
}

tap_text() {
    tap_node "text=\"$1\""
}

# hide_error_dialogs stops new dialogs but does not close one that is already up, and uiautomator
# only reads the focused window. A launcher that stalled during boot left the sample fully loaded
# behind its dialog, and this script timed out waiting for text it could not see. By resource id
# because the label is translated. An ANR of the sample's own would still be in the log, so closing
# its dialog here would not hide it from require_healthy.
close_leftover_dialog() {
    if grep 'resource-id="android:id/aerr_wait"' "${UI_DUMP}" > /dev/null; then
        echo "==> closing a system dialog that was already up"
        tap_node 'resource-id="android:id/aerr_wait"'
    fi
}

# The freeze needs a garbage collection after FFmpeg has run, and with Mono's default 4 MB nursery
# this small app can finish a conversion and sit idle without one: the published 8.1.2.5, which does
# freeze, passed this script three times out of three until this was added. A tiny nursery makes
# collections near constant while changing nothing else about the Release build. It stays out of
# the sample's own project because consumers copy that project.
printf 'MONO_GC_PARAMS=nursery-size=%s\n' "${GC_NURSERY}" > "${SDK_DIR}/gc-stress.env"
cat > "${SDK_DIR}/gc-stress.targets" <<EOF
<Project>
  <ItemGroup>
    <AndroidEnvironment Include="${SDK_DIR}/gc-stress.env" />
  </ItemGroup>
</Project>
EOF

echo "==> installing the sample (version=${VERSION}, tfm=${TARGET_FRAMEWORK}, sdk=${sdk_version})"
# A failed build leaves any earlier APK in place, and that would then report a verdict of its own.
adb uninstall "${PACKAGE_NAME}" > /dev/null 2>&1 || true
( cd "${SDK_DIR}" && dotnet build "${PROJECT}" \
    --configuration Release \
    -f "${TARGET_FRAMEWORK}" \
    -p:SampleSdkBand="net${sdk_major}" \
    -p:FFmpegKitVersion="${VERSION}" \
    -p:RuntimeIdentifier="${DEVICE_RID}" \
    -p:CustomAfterMicrosoftCommonTargets="${SDK_DIR}/gc-stress.targets" \
    -t:Install )

echo "==> launching"
adb logcat -c
adb shell input keyevent KEYCODE_WAKEUP
# Resolved rather than named because the sample's activity has a generated crc64 class name.
activity="$(adb shell cmd package resolve-activity --brief "${PACKAGE_NAME}" | tail -1)"
adb shell am start -W -n "${activity}"

# The button ignores taps until OnAppearing has copied the asset and probed it, which is what
# putting "Source:" on screen means. Tapping any earlier would be lost, silently.
echo "==> waiting for the sample to load"
deadline=$(( SECONDS + LAUNCH_TIMEOUT ))
until screen_shows 'text="Source: '; do
    [ "${SECONDS}" -lt "${deadline}" ] || fail "the sample did not load within ${LAUNCH_TIMEOUT}s"
    # Nothing has been tapped yet, so a slow first frame cannot be an ANR.
    require_running "while loading"
    close_leftover_dialog
    sleep 2
done

echo "==> running one conversion"
tap_text "Convert sample video"

# Tapping the headline throughout is deliberate. It does nothing, but a freeze or a blocked UI
# thread only becomes an ANR with an input event waiting, and a person would be tapping too.
deadline=$(( SECONDS + CONVERSION_TIMEOUT ))
until screen_shows 'text="Success!'; do
    if grep -E 'text="(Conversion failed|Unexpected error)' "${UI_DUMP}" > /dev/null; then
        fail "the conversion failed: $(grep -oE 'text="(Conversion failed|Unexpected error)[^"]*"' "${UI_DUMP}")"
    fi
    [ "${SECONDS}" -lt "${deadline}" ] || fail "no success text within ${CONVERSION_TIMEOUT}s"
    require_healthy "during the conversion"
    tap_text "FFmpegKit.Android Example"
    sleep 2
done

# The freeze this exists for hits at the first garbage collection after a conversion, and that can
# land after the success text. So the run only counts if the app still takes input afterwards.
echo "==> checking the app stays responsive"
deadline=$(( SECONDS + ANR_WATCH ))
while [ "${SECONDS}" -lt "${deadline}" ]; do
    require_healthy "after the conversion"
    tap_text "FFmpegKit.Android Example"
    sleep 2
done
require_healthy "after the conversion"

echo "==> sample run passed"
