#!/bin/sh

# NOTE: the original arthenica/ffmpeg-kit repo is archived and its v5.1.LTS release
# assets have been deleted from GitHub. Its successor, ffmpeg-kit-next, is source-only
# (no prebuilt binaries since v6.1.0). The .aar files below instead come from the
# community-maintained Maven Central mirror at dev.ffmpegkit-maintained.

FFMPEG_KIT_VERSION="8.1.7"
MAVEN_BASE="https://repo1.maven.org/maven2/dev/ffmpegkit-maintained"

rm -f *.jar
rm -f *.aar

curl -L -O "https://github.com/tanersener/smart-exception/releases/download/v0.2.1/smart-exception-common-0.2.1.jar"
curl -L -O "https://github.com/tanersener/smart-exception/releases/download/v0.2.1/smart-exception-java-0.2.1.jar"
for variant in audio full full-gpl https https-gpl min min-gpl video; do
    curl -L -O "$MAVEN_BASE/ffmpeg-kit-$variant/$FFMPEG_KIT_VERSION/ffmpeg-kit-$variant-$FFMPEG_KIT_VERSION.aar"
done

