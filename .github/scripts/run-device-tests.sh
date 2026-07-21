#!/usr/bin/env bash
set -euo pipefail

# Installs the device test app against a packed FFmpegKit package and runs its smoke tests on the
# emulator that the calling workflow step has already booted. Results are reported to logcat under
# the FFmpegKitE2E tag; this script turns them into an exit code.

VARIANT="${1:-Video}"
VERSION="${2:?a package version is required}"

PACKAGE_NAME="com.sbokatuk.ffmpegkit.devicetests"
LOG_FILE="device-tests-logcat.txt"
POLL_ATTEMPTS=60
POLL_INTERVAL=5

echo "==> installing device tests (variant=${VARIANT}, version=${VERSION})"
dotnet build tests/FFmpegKit.Android.DeviceTests/FFmpegKit.Android.DeviceTests.csproj \
    --configuration Release \
    -p:FFmpegKitVariant="${VARIANT}" \
    -p:FFmpegKitPackageVersion="${VERSION}" \
    -p:RuntimeIdentifier=android-x64 \
    -t:Install

echo "==> launching"
adb logcat -c
adb shell am start -n "${PACKAGE_NAME}/.MainActivity"

echo "==> waiting for results"
for _ in $(seq 1 "${POLL_ATTEMPTS}"); do
    if adb logcat -d -s "FFmpegKitE2E:*" | grep -q "FFMPEGKIT_E2E_DONE"; then
        break
    fi
    sleep "${POLL_INTERVAL}"
done

adb logcat -d -s "FFmpegKitE2E:*" | tee "${LOG_FILE}"

if ! grep -q "FFMPEGKIT_E2E_DONE PASS" "${LOG_FILE}"; then
    # No verdict usually means the app died before reporting, so keep the crash trace.
    echo "==> no passing verdict; capturing crash output"
    adb logcat -d -s AndroidRuntime:E DEBUG:F "${PACKAGE_NAME}:*" | tee -a "${LOG_FILE}"
    echo "::error::FFmpegKit device smoke tests failed or timed out"
    exit 1
fi

echo "==> device smoke tests passed"
