#!/bin/sh

set -e

# NOTE: the original arthenica/ffmpeg-kit repo is archived and its v5.1.LTS release
# assets have been deleted from GitHub. Its successor, ffmpeg-kit-next, is source-only
# (no prebuilt binaries since v6.1.0). The .aar files below instead come from the
# community-maintained Maven Central mirror at dev.ffmpegkit-maintained.
#
# Keep FFMPEG_KIT_VERSION in sync with FFmpegKitNativeVersion in Directory.Build.props.

FFMPEG_KIT_VERSION="8.1.7"
MAVEN_BASE="https://repo1.maven.org/maven2/dev/ffmpegkit-maintained"

cd "$(dirname "$0")"

rm -f ./*.jar
rm -f ./*.aar

# FFmpegKitConfig's static initialiser calls com.arthenica.smartexception.java.Exceptions, which
# is not bundled in the .aar and is not declared in its .pom. Without these two jars every
# FFmpeg call fails at runtime with NoClassDefFoundError.
curl -fL -O "https://github.com/tanersener/smart-exception/releases/download/v0.2.1/smart-exception-common-0.2.1.jar"
curl -fL -O "https://github.com/tanersener/smart-exception/releases/download/v0.2.1/smart-exception-java-0.2.1.jar"

for variant in audio full full-gpl https https-gpl min min-gpl video; do
    curl -fL -O "$MAVEN_BASE/ffmpeg-kit-$variant/$FFMPEG_KIT_VERSION/ffmpeg-kit-$variant-$FFMPEG_KIT_VERSION.aar"
done
