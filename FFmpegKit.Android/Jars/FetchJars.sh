#!/bin/sh

rm -f *.jar
rm -f *.aar

curl -L -O "https://github.com/tanersener/smart-exception/releases/download/v0.2.1/smart-exception-common-0.2.1.jar"
curl -L -O "https://github.com/tanersener/smart-exception/releases/download/v0.2.1/smart-exception-java-0.2.1.jar"
curl -L -O "https://github.com/arthenica/ffmpeg-kit/releases/download/v5.1.LTS/ffmpeg-kit-audio-5.1.LTS.aar"
curl -L -O "https://github.com/arthenica/ffmpeg-kit/releases/download/v5.1.LTS/ffmpeg-kit-full-5.1.LTS.aar"
curl -L -O "https://github.com/arthenica/ffmpeg-kit/releases/download/v5.1.LTS/ffmpeg-kit-full-gpl-5.1.LTS.aar"
curl -L -O "https://github.com/arthenica/ffmpeg-kit/releases/download/v5.1.LTS/ffmpeg-kit-https-5.1.LTS.aar"
curl -L -O "https://github.com/arthenica/ffmpeg-kit/releases/download/v5.1.LTS/ffmpeg-kit-https-gpl-5.1.LTS.aar"
curl -L -O "https://github.com/arthenica/ffmpeg-kit/releases/download/v5.1.LTS/ffmpeg-kit-min-5.1.LTS.aar"
curl -L -O "https://github.com/arthenica/ffmpeg-kit/releases/download/v5.1.LTS/ffmpeg-kit-min-gpl-5.1.LTS.aar"
curl -L -O "https://github.com/arthenica/ffmpeg-kit/releases/download/v5.1.LTS/ffmpeg-kit-video-5.1.LTS.aar"

