---
applyTo: ".github/workflows/*.yml"
---

# Workflows

- Everything runs on **ubuntu-latest** (unlike the iOS/Mac siblings). Do not move jobs to macOS runners; no native code is compiled here.
- `pr.yml` and `release.yml` are thin callers of the reusable `build.yml`. Put shared build logic in `build.yml`, not in the callers.
- **`verify` input**: `true` (default) runs package validation, the sample matrix and the emulator smoke tests; releases pass `false` because the tagged commit was already verified on its pull request. Keep the input's name and meaning identical across the sibling repositories.
- **Trusted publishing**: publishing jobs declare `environment: nuget.org` and `permissions: id-token: write`, authenticate with `NuGet/login@v1` using only `secrets.NUGET_USER`, and push immediately afterwards (the key lasts an hour and each OIDC token is exchangeable once). Never add an API-key secret. Forked pull requests get no OIDC token, so their publish job stays skipped via the `head.repo.full_name == github.repository` condition.
- **Do not weaken the release `guard` job.** It proves the tagged commit is an ancestor of the default branch; without it a tag from an unmerged branch would ship having been verified by nothing.
- Versions: `pr.yml` reads `FFmpegVersion` (not `FFmpegKitNativeVersion`) for `<version>-beta.<pr>.<run>`; `release.yml` derives the FFmpeg version from the tag and resolves the `.aar` version through `build/resolve-versions.sh`. An unmapped version must keep failing the run.
- `auto-release.yml` tags only release notes **added** (`--diff-filter=A`) by a push to `main`, only four-part versions, and dispatches `release.yml` at the tag — a tag pushed with `GITHUB_TOKEN` does not trigger `on: push: tags`, so the dispatch is required, as is `release.yml`'s `workflow_dispatch` trigger.
- The pack job caches `src/FFmpegKit.Android/Jars` keyed on the native version, and re-runs `FetchJars.sh --verify` even on a cache hit. Keep that verification step.
- Emulator jobs must stay `x86_64` (the `.aar` ships arm64-v8a and x86_64 only), keep KVM enabled and keep the disk-cleanup step; the AVD needs ~7.2 GB free.
- The `e2e` matrix runs every variant, one job each: each is a different `.aar`, and nothing else loads its native code. Do not narrow it to save time, and keep the Audio leg on API 31: it is the only one that can see an x86_64-specific load failure, which API 35 hides. `sample-run` deploys the sample because the device checks once passed on a package that froze it; keep it a gate on publishing betas.
- `older-tracks` in `pr.yml` is what makes the release's `verify: false` honest for a track that is not the default: merging a note is how a track ships, and `tracks-to-verify.sh` runs the track on the pull request that adds it (a tag pushed by hand skips this). Keep that trigger, keep both API levels (the failure it exists for only shows on API 31), and keep `publish` from taking its packages: the `artifact-suffix` input is what separates them. `publish` needs `older-tracks` and must keep accepting `skipped`, or every pull request that runs no track stops publishing. `tracks-to-verify.sh` lists the checks whose changes run every track; a new script the track legs depend on belongs in that list. New scripts need `git update-index --chmod=+x`, because the working volume does not track file modes.
- `upstream-drift.yml` only orchestrates: what is watched belongs in `build/upstream.tsv`, how it is checked in `build/check-upstream.sh`.
- Keep the explanatory comments — they record failures that have already happened once (NETSDK1112, emulator boot timeouts, silent OIDC failures).
