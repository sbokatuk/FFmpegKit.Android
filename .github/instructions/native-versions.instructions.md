---
applyTo: "build/native-versions.tsv, build/checksums/**, src/FFmpegKit.Android/Jars/**"
---

# Adopting a new upstream line

The `.aar` files come from Maven Central (`dev.ffmpegkit-maintained`) and are fetched at build time; nothing binary is committed. Follow this order — each step depends on the previous one.

1. **Establish the FFmpeg version the release actually contains.** Read it out of the shipped `libavcodec.so` (`jni/arm64-v8a/` inside the `.aar`), the way `.github/scripts/native-build-info.sh` does. Upstream release prose mentions other FFmpeg versions in passing and must not be used as the source.
2. **Record the checksum baseline:** `./build/update-checksums.sh <ffmpegkit version>` writes `build/checksums/<ffmpegkit>.sha256` (Maven Central `.sha256` sidecars where published, download-and-hash otherwise). Review the diff and commit it. This is deliberately never run in CI — CI verifies against a baseline a human recorded.
3. **Add the mapping row** to `build/native-versions.tsv`: `<ffmpeg>\t<ffmpegkit>`, tab-separated, keeping the existing comment header. There is one row per FFmpeg version, because the lookup stops at the first match and a test requires both columns to be unique. A new FFmpeg gets a new row. A respin of the same FFmpeg replaces that row: note the old pair in the header comment, keep its checksum baseline, and say in the release note that the old FFmpegKit release can no longer be built from `main`. Existing tags are unaffected, because a release reads the file as it stood at its tag. Never drop a track.
4. **Then**, if this is the default line, bump `FFmpegVersion` and `FFmpegKitNativeVersion` in `Directory.Build.props` (both, together — they are different schemes). Older lines leave the props alone. Either way, fetch: `./src/FFmpegKit.Android/Jars/FetchJars.sh <ffmpegkit version>`.
5. Verify and pack: `./src/FFmpegKit.Android/Jars/FetchJars.sh --verify`, then `./src/FFmpegKit.Android/BuildNugets.sh`, then `dotnet test tests/FFmpegKit.Android.PackageTests` (it asserts the mapping and the props agree).

## Rules

- Never commit `.aar` or `.jar` files; only `AboutJars.txt` and `FetchJars.sh` are tracked under `Jars/`.
- Never fetch a line with no committed baseline, and never remove, relax or short-circuit the SHA-256 checks — they are the only thing standing between a substituted native artifact and a shipped package.
- A version pair that is not a row in `build/native-versions.tsv` cannot be built or released; add the row rather than special-casing a script.
- `FetchJars.sh` and `update-checksums.sh` read the versions from `Directory.Build.props`; keep it that way instead of hard-coding a second copy.
- All eight variants plus both `smart-exception` jars are fetched and verified as one set — do not narrow the list.
