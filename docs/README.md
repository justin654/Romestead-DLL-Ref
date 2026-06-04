# Romestead Assembly Reference

Generated static reference for Romestead modding. This site indexes scanned game assemblies so mod authors can find types, inspect members, copy Harmony target strings, and follow best-effort call relationships.

Community links:

- Romestead Wiki: <https://romestead.wiki.gg/>
- Official Romestead Discord: <https://discord.gg/q7DP3GGrgZ>

## Entry Points

- `index.html` - main reference home and search
- `guide.html` - modder-oriented usage guide
- `commands.html` - terminal dot-command reference
- `commands.json` - machine-readable terminal command catalog
- `namespaces.html` - namespace browser
- `topics.html` - heuristic topic browser
- `diff.html` - latest patch diff
- `snapshot.json` - complete catalog snapshot
- `site-manifest.json` - generated site metadata

## Current Snapshot

- Build metadata: `site-manifest.json`
- Assemblies: `4`
- Types: `4794`
- Methods: `16159`
- Properties: `1565`
- Fields: `22394`
- Scanned assemblies: `CandideCreator.Shared`, `CandideServer`, `Romestead`, `Shared`

## Latest Diff

- Types: `+1 / -1 / ~80`
- Methods: `+28 / -7 / ~111`
- Fields: `+54 / -9 / ~0`

## Hosting On GitHub Pages

This folder is ready to publish as a static site. Keep `.nojekyll` in the published root so GitHub Pages serves generated files directly without Jekyll processing.

Recommended layout:

- Publish this generated folder as the Pages root for a docs-only repository.
- Or copy it under a versioned folder such as `latest/` or `builds/YYYY-MM-DD/` and link to those versions from a small repository home page.

## Notes For Modders

- Public members are usually safer references, but this catalog does not guarantee API stability.
- Private and internal members are included for Harmony patch authors. Treat them as patch targets, not stable contracts.
- Caller/callee relationships are best-effort and based on IL method references.
- Topic pages are heuristic groupings, not curated official API categories.
