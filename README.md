# Romestead Assembly Reference

`Romestead Assembly Reference` is a standalone CLI and static-site generator for scanning the Romestead game assemblies, writing stable JSON snapshots, and producing patch diffs plus a searchable HTML catalog.

This is an unofficial community tool. It does not ship game assemblies or generated catalog output.

## What it does

- Scans one or more game assemblies with `Mono.Cecil`
- Writes a stable `snapshot.json`
- Diffs snapshots across patches
- Generates `diff.json`, `diff.md`, and `diff.html`
- Builds a browseable HTML reference site for assemblies, types, and members
- Produces an API opportunity report aimed at mod-loader surface planning
- Extracts terminal dot commands on demand without regenerating the full patch diff

## Requirements

- .NET 8 SDK
- Windows for the included Steam helper script and examples
- A local Romestead install

## Repo Layout

- `src/RomesteadRef/` - the CLI source
- `scripts/scan_steam_build.bat` - helper script for the default Steam install path
- `output/` - generated snapshots, diffs, and HTML site output

`output/` is generated locally and ignored by git.

## Quick Start

From the repo root:

```powershell
dotnet build .\RomesteadReference.sln -c Release
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- help
```

To scan the default Steam install directly:

```powershell
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- scan `
  --root "C:\Program Files (x86)\Steam\steamapps\common\romestead" `
  --assembly-exact Romestead `
  --assembly-exact Shared `
  --assembly-exact CandideServer `
  --assembly-exact CandideCreator.Shared
```

To stamp the diff with a patch label that will appear on `diff.html` and removed-member pages:

```powershell
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- scan `
  --root "C:\Program Files (x86)\Steam\steamapps\common\romestead" `
  --baseline .\output\latest\history\snapshot-20260527-231110-basegame.json `
  --patch-label "0.25.1_5 + 0.25.1_6" `
  --assembly-exact Romestead `
  --assembly-exact Shared `
  --assembly-exact CandideServer `
  --assembly-exact CandideCreator.Shared `
  --allow-modded
```

To require a known clean Steam manifest before scanning:

```powershell
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- scan `
  --root "C:\Program Files (x86)\Steam\steamapps\content\app_1805320\depot_1805321" `
  --require-known-clean `
  --expected-manifest 4886639939922115546 `
  --assembly-exact Romestead `
  --assembly-exact Shared `
  --assembly-exact CandideServer `
  --assembly-exact CandideCreator.Shared
```

Or use the helper script:

```powershell
.\scripts\scan_steam_build.bat
```

## Baseline Workflow

The normal patch workflow is:

1. Scan a known-good base game build.
2. Keep that snapshot in `output/latest/history/` and rename it with a `-basegame.json` suffix.
3. After a patch lands, rerun the scan against the updated game folder.
4. Open `output/latest/diff.html` for the patch report.

Example baseline rename:

```powershell
Rename-Item .\output\latest\history\snapshot-20260527-231110.json snapshot-20260527-231110-basegame.json
```

The helper script automatically looks for the newest `*-basegame.json` file and uses it as the baseline.

If `--baseline` is omitted and `output/latest/snapshot.json` already exists, `scan` automatically diffs against the previous run.

## Known-Clean Verification

Steam depot manifests include file sizes and SHA1 hashes. SteamDB is useful for finding app, depot, and manifest IDs; full hashes may require signing in or reading the downloaded Steam manifest. Store trusted entries in `known-clean-romestead.json`.

Use `--require-known-clean` to load `known-clean-romestead.json` from the repo root and refuse to scan if `Romestead.dll` does not match any listed size/SHA1 pair. Use `--expected-manifest <id>` to require one exact manifest.

The first included entry is for app `1805320`, depot `1805321`, manifest `4886639939922115546`.

## Modded Install Safety

By default the scanner refuses to process `Romestead.dll` if it detects mod-loader markers or if the DLL does not match `Romestead.dll.modloader-backup`.

That is useful for publishing clean reference output, but local patch investigation against a modded install may require:

```powershell
.\scripts\scan_steam_build.bat --allow-modded
```

## Terminal Commands Only

Use `commands` when you only want to refresh the terminal dot-command reference. It writes `commands.html`, `commands.json`, `commands.md`, optional `commands-diff.json`, and `history/commands-*.json`; it does not touch `snapshot.json`, `diff.html`, or the full generated catalog.

```powershell
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- commands `
  --root "C:\Program Files (x86)\Steam\steamapps\common\romestead" `
  --out .\output\commands
```

To compare against a published command catalog explicitly:

```powershell
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- commands `
  --root "C:\Program Files (x86)\Steam\steamapps\common\romestead" `
  --old .\docs\commands.json `
  --out .\output\commands
```

## Output Files

After a scan, the default output location is `.\output\latest\`.

Important files:

- `index.html` - entry page for the catalog
- `diff.html` - latest patch diff
- `snapshot.json` - latest machine-readable snapshot
- `diff.json` - latest machine-readable diff
- `api-opportunities.html` - suggested mod-loader API surface report

When a baseline is provided, the site also preserves removed type pages and marks removed methods, properties, and fields on surviving type pages.

## Publishing to GitHub Pages

GitHub Actions does not scan the game. The expected flow is to generate the reference locally, copy the generated site and diff files into `docs/`, then commit and push those published files.

1. Run a scan locally.
2. Copy `output/latest/` into the committed `docs/` folder.
3. Commit and push `docs/`, including `diff.html`, `diff.json`, `diff.md`, `snapshot.json`, and any generated history files.
4. GitHub Actions deploys `docs/` to GitHub Pages.

Commands:

```powershell
.\scripts\scan_steam_build.bat --allow-modded
.\scripts\publish_latest_to_docs.bat
git add docs
git commit -m "Publish latest reference site"
git push
```

In the GitHub repository settings, go to **Settings -> Pages** and set **Build and deployment** to **GitHub Actions**. The included workflow uploads `docs/` with `actions/upload-pages-artifact` and deploys it with `actions/deploy-pages`.

## Direct Commands

```powershell
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- scan
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- diff --old .\output\latest\history\old.json --new .\output\latest\snapshot.json
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- commands --root "C:\Program Files (x86)\Steam\steamapps\common\romestead" --out .\output\commands
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- find RevealAll
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- inspect Candide.Program
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- calls RevealAll
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- xref RevealAll
dotnet run --project .\src\RomesteadRef\RomesteadRef.csproj -- relate RevealAll
```

## Notes

- The generated site branding is intentionally Romestead-specific.
- The code is organized as a standalone repo now, but the analysis logic still assumes Romestead naming and clean-DLL heuristics.

## License

MIT. See `LICENSE`.
