# Unbound

**A one-click installer that sets up [No Chat Restrictions](https://modrinth.com/mod/no-chat-restrictions) on a stock Minecraft install.**

Since 1.16.4, Minecraft can **lock accounts out of chat and the multiplayer menu** based on
Microsoft/Mojang account flags (the "Chat disabled by account settings" block — it hits flagged
and child accounts). The community mod **No Chat Restrictions** (by Aizistral) reverts that
behaviour client-side, restoring chat and multiplayer access the way it worked before 1.16.4.
Unbound makes it trivial for non-technical players to get: double-click, pick your Minecraft
version, done — no command line, no Java knowledge, works with the **official launcher and
vanilla client**.

> Unbound is an *installer*. It is not the mod and is **not affiliated** with Aizistral. It
> installs the official No Chat Restrictions + Fabric API builds. All credit for the mod goes to
> [Aizistral](https://github.com/Aizistral-Studios/No-Chat-Restrictions).

## What it does

1. Finds your `.minecraft` (Windows `%APPDATA%\.minecraft`, macOS
   `~/Library/Application Support/minecraft`, Linux `~/.minecraft`).
2. Installs the **Fabric** loader for your chosen Minecraft version by writing the files
   directly — no Fabric installer, **no Java needed to run Unbound** (the launcher uses its own
   bundled Java to run the game).
3. Downloads **No Chat Restrictions** + its dependency **Fabric API** from Modrinth (SHA-512
   verified), matched to that version — and falls back to **bundled copies** baked into Unbound
   if you're offline.
4. Adds an **"Unbound"** profile to the official launcher. Open it, pick the profile, play.

No Chat Restrictions is **client-side only**, so nothing needs to change on the servers you join.
Re-running is safe (idempotent), and `uninstall` reverts everything (profile + the mods it added).

## How it works under the hood

- **Fabric** is installed by querying the [Fabric Meta API](https://meta.fabricmc.net): we pull
  the launcher-ready profile JSON into `.minecraft/versions/`, download the loader libraries
  into `.minecraft/libraries/`, and merge a profile into `launcher_profiles.json` (the original
  is backed up to `launcher_profiles.json.unbound-bak` first).
- **Mods** come from the [Modrinth API](https://docs.modrinth.com/api/) — newest compatible
  release of No Chat Restrictions (`z440MEwJ`) + Fabric API (`P7dR8mSH`) for your version and the
  Fabric loader, verified by SHA-512.

## Status

| Milestone | State |
|---|---|
| Core engine + CLI + tests | ✅ done, verified end-to-end (online + offline) |
| Avalonia GUI (the double-click experience) | ✅ done (`Unbound.App`), dark wizard |
| Per-OS self-contained packaging | ✅ scripts + verified (win-x64, linux-x64) |
| Code signing / notarization | ⏳ next (see notes below) |

## Project layout

```
Unbound.slnx
src/
  Unbound.Core/    all install logic (platform-agnostic, fully unit-tested)
  Unbound.Cli/     console front-end that drives Core (dev + power users)
  Unbound.App/     Avalonia GUI wizard (the double-click installer)
tests/
  Unbound.Core.Tests/   xunit
```

## Build & run (CLI)

Requires the **.NET 10 SDK**.

```bash
dotnet build Unbound.slnx
dotnet test  Unbound.slnx

# launch the GUI wizard (the double-click app)
dotnet run --project src/Unbound.App

# or use the CLI — install for a specific version
dotnet run --project src/Unbound.Cli -- --mc 1.21.1

# auto-detect a version, list supported versions, or revert
dotnet run --project src/Unbound.Cli
dotnet run --project src/Unbound.Cli -- list
dotnet run --project src/Unbound.Cli -- uninstall

# options: --path <.minecraft>   --offline (use bundled jars only)
```

## Packaging

Build self-contained, single-file installers (the user needs no .NET runtime and no Java):

```bash
pwsh build/publish.ps1            # all platforms -> artifacts/<rid>/Unbound(.exe)
pwsh build/publish.ps1 win-x64    # just one platform
build/publish.sh                  # macOS/Linux dev (same output)
```

Targets: `win-x64`, `osx-x64`, `osx-arm64`, `linux-x64`. Each build is ~50 MB (it bundles the
.NET runtime and Avalonia's native libraries into one file). The output is **unsigned** — see
Distribution & trust below.

## Distribution & trust (read before shipping binaries)

A downloaded installer that modifies game files trips OS safety systems:

- **Windows SmartScreen** warns on unsigned `.exe`s until they build reputation. Mitigation:
  code-sign (Azure Trusted Signing or a Certum open-source cert), or document
  *More info → Run anyway*.
- **macOS Gatekeeper** requires **notarization** (Apple Developer ~$99/yr) for a clean launch;
  otherwise users must right-click → Open.
- **Linux**: no signing required.

Build trust by keeping this **open source**, verifying every downloaded jar (we do), only ever
pulling from Modrinth, and clearly attributing the mod author.

## Offline fallback jars

`src/Unbound.Core/assets/bundled/` holds the embedded fallback jars and a `manifest.json` mapping
`(mod, Minecraft version) → file`. They are exact-version matches. Regenerate/expand the set with
the CLI's maintenance command (it reuses the SHA-verified Modrinth engine):

```bash
dotnet run --project src/Unbound.Cli -- bundle 1.20.1 1.21.1 1.21.4 1.21.8
```

Currently bundled: **1.20.1, 1.21.1, 1.21.4, 1.21.8** (~8.6 MB; Fabric API is the bulk — No Chat
Restrictions itself is ~9 KB and one jar spans the whole 1.21.x range). (Note: full no-internet
support also needs the Fabric loader libraries bundled — `--offline` currently covers the mods,
while Fabric's libraries are still fetched from the network.)

## License & attribution

- **No Chat Restrictions** is by **Aizistral**, licensed **WTFPL** —
  <https://github.com/Aizistral-Studios/No-Chat-Restrictions>. Unbound ships/downloads the
  official builds and does not reuse the mod's name or icon as its own.
- Unbound's own installer code is licensed **MIT** — see [LICENSE](LICENSE).
