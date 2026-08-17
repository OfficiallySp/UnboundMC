# Unbound: one-click Minecraft chat fix (No Chat Restrictions installer)

**Unbound is a free, open-source, one-click installer that restores Minecraft chat and multiplayer by setting up [No Chat Restrictions](https://modrinth.com/mod/no-chat-restrictions) on a stock (vanilla) Minecraft install. No modding knowledge, no command line, and no Java required.**

If Minecraft shows **"Chat disabled by account settings"** or **"Multiplayer is disabled. Please check your Microsoft account settings,"** Unbound gets your chat and multiplayer access back in a few clicks. Double-click, pick your Minecraft version, done.

This affects far more than flagged or child accounts. Players who rely on **accessibility tools and AAC devices** lose the chat their setups are built around, adult accounts get locked out until they verify their age with a photo ID or a face scan, and plenty of people are blocked with no explanation at all. Unbound restores chat for all of them.

> Note: this is mainly for less tech-savvy users. People who already know modding and use custom launchers do not need this.

## What "Chat disabled by account settings" is, and how Unbound fixes it

Since 1.16.4, Minecraft can **lock accounts out of chat and the multiplayer menu** based on Microsoft/Mojang account flags. That is the **"Chat disabled by account settings"** block that flagged and child accounts hit. The community mod **No Chat Restrictions** (by Aizistral) reverts that behaviour client-side, restoring chat and multiplayer access the way it worked before 1.16.4. Unbound makes it trivial for non-technical players: double-click, pick your Minecraft version, done. No command line, no Java knowledge, and it works with the **official launcher and vanilla client**.

> Unbound is an *installer*. It is not the mod and is **not affiliated** with Aizistral. It
> installs the official No Chat Restrictions + Fabric API builds. All credit for the mod goes to
> [Aizistral](https://github.com/Aizistral-Studios/No-Chat-Restrictions).

## What it does

1. Finds your `.minecraft` (Windows `%APPDATA%\.minecraft`, macOS
  `~/Library/Application Support/minecraft`, Linux `~/.minecraft`).
2. Installs the **Fabric** loader for your chosen Minecraft version by writing the files
  directly no Fabric installer, **no Java needed to run Unbound** (the launcher uses its own
  bundled Java to run the game).
3. Downloads **No Chat Restrictions** + its dependency **Fabric API** from Modrinth (SHA-512
  verified), matched to that version and falls back to **bundled copies** baked into Unbound
  if you're offline.
4. Adds an **"Unbound"** profile to the official launcher. Open it, pick the profile, play.

No Chat Restrictions is **client-side only**, so nothing needs to change on the servers you join.
Re-running is safe (idempotent), and `uninstall` reverts everything (profile, the mods it added, and
any config it wrote).

## Telemetry & profanity filter

No Chat Restrictions turns off Mojang's chat telemetry and the chat profanity filter by default.
Recent versions let you opt back into either through the mod's own config file
(`config/NoChatRestrictions.json`). Unbound exposes both:

- In the app, tick **Re-enable Mojang telemetry** or **Re-enable the chat profanity filter** before
  installing.
- On the CLI, pass `--allow-telemetry` and/or `--allow-profanity-filter`.

Both are off by default, which keeps chat fully unrestricted. Unbound only writes
`config/NoChatRestrictions.json` when you opt into one of them, backing up any existing config to
`NoChatRestrictions.json.unbound-bak` first, and `uninstall` restores it.

## Frequently asked questions

### Does Unbound fix "Chat disabled by account settings"?
Yes. That message appears when Minecraft locks a flagged or child account out of chat. Unbound installs No Chat Restrictions, which restores chat on the client side, so the message goes away and you can type in chat again.

### Does it fix "Multiplayer is disabled. Please check your Microsoft account settings"?
Yes, the same way. No Chat Restrictions restores access to the multiplayer menu and to servers on the client side.

### I got blocked after an update and I am not a child account. What happened?
Minecraft has been rolling out **account age verification**. Accounts that have not verified lose chat, and on some versions the multiplayer menu as well, and the game shows the same "Chat disabled by account settings" message. Verifying means proving your age to a third-party verification provider, typically with a photo ID or a face scan. This is not limited to one country: players in the US and elsewhere report being blocked too, including long-standing adult accounts. Unbound restores chat on the client side, so you get your chat back without verifying and without handing your ID or biometric data to anyone.

### I use an accessibility tool, screen reader, or AAC device and chat stopped working. Does this help?
Yes. Assistive setups that read or send Minecraft chat, including AAC devices and custom accessibility interfaces, stop working when the account-level chat block kicks in, because the client itself refuses to send or show chat. No Chat Restrictions restores normal chat behaviour, which restores what those tools hook into. Unbound installs it with no modding, no Java, and no command line, which matters when the person doing the install cannot easily use a terminal.

### Does this work with Lunar Client, CurseForge, Prism, or other custom launchers?
Unbound targets the **official Minecraft launcher with a stock vanilla install**, which is what most affected players are running. If you already use Lunar, CurseForge, Prism, MultiMC, or the Modrinth App, you do not need Unbound: those launchers have their own mod support, so install [No Chat Restrictions](https://modrinth.com/mod/no-chat-restrictions) directly through them. Unbound is for people who have never installed a mod and do not want to start now.

### Do I need Java or a custom launcher?
No. Unbound writes the Fabric files directly, and the official Minecraft launcher runs the game with its own bundled Java. You never install Java yourself.

### Which Minecraft versions are supported?
Any version No Chat Restrictions publishes a Fabric build for. Run `unbound list` to see the current list. Recent versions such as 1.20.x and 1.21.x are supported, and 1.20.1, 1.21.1, 1.21.4, and 1.21.8 also work fully offline from bundled jars.

### Is it safe? Is it a virus?
Unbound is open source (read the code in this repo), only ever downloads official builds from Modrinth, and verifies every jar with a SHA-512 hash before installing. Because it is an unsigned installer that edits game files, Windows SmartScreen or macOS Gatekeeper may warn you the first time; see Distribution and trust below.

### Will this unban me, or fix "Incompatible mod found"?
See the Troubleshooting section below.

## How it works under the hood

- **Fabric** is installed by querying the [Fabric Meta API](https://meta.fabricmc.net): we pull
 the launcher-ready profile JSON into `.minecraft/versions/`, download the loader libraries
 into `.minecraft/libraries/`, and merge a profile into `launcher_profiles.json` (the original
 is backed up to `launcher_profiles.json.unbound-bak` first).
- **Mods** come from the [Modrinth API](https://docs.modrinth.com/api/) newest compatible
 release of No Chat Restrictions (`z440MEwJ`) + Fabric API (`P7dR8mSH`) for your version and the
 Fabric loader, verified by SHA-512.

## Build & run (CLI)
**This project can also be run on the command line, see below**
Requires the **.NET 10 SDK**.

```bash
dotnet build Unbound.slnx
dotnet test Unbound.slnx

# launch the GUI wizard (the double-click app)
dotnet run --project src/Unbound.App

# or use the CLI install for a specific version
dotnet run --project src/Unbound.Cli -- --mc 1.21.1

# auto-detect a version, list supported versions, or revert
dotnet run --project src/Unbound.Cli
dotnet run --project src/Unbound.Cli -- list
dotnet run --project src/Unbound.Cli -- uninstall

# options: --path <.minecraft>  --offline (use bundled jars only)
#          --allow-telemetry  --allow-profanity-filter  (see Telemetry & profanity filter below)
```

## Packaging

Build self-contained, single-file installers (the user needs no .NET runtime and no Java):

```bash
pwsh build/publish.ps1      # all platforms -> artifacts/<rid>/Unbound(.exe)
pwsh build/publish.ps1 win-x64  # just one platform
build/publish.sh         # macOS/Linux dev (same output)
```

Targets: `win-x64`, `osx-x64`, `osx-arm64`, `linux-x64`. Each build is ~50 MB (it bundles the
.NET runtime and Avalonia's native libraries into one file). The output is **unsigned** see
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

**Releases & signing setup:** the [release workflow](.github/workflows/release.yml) builds all four
platforms on every run and publishes a GitHub Release when you push a `v*` tag. Signing turns on
automatically once the certs are added as repo secrets see **[SIGNING.md](SIGNING.md)** for the
Windows (Azure Trusted Signing) and macOS (Developer ID + notarization) setup, plus the
"Run anyway" / Gatekeeper instructions to give users while builds are unsigned.

## Offline fallback jars

`src/Unbound.Core/assets/bundled/` holds the embedded fallback jars and a `manifest.json` mapping
`(mod, Minecraft version) → file`. They are exact-version matches. Regenerate/expand the set with
the CLI's maintenance command (it reuses the SHA-verified Modrinth engine):

```bash
dotnet run --project src/Unbound.Cli -- bundle 1.20.1 1.21.1 1.21.4 1.21.8
```

Currently bundled: **1.20.1, 1.21.1, 1.21.4, 1.21.8** (~8.6 MB; Fabric API is the bulk No Chat
Restrictions itself is ~9 KB and one jar spans the whole 1.21.x range). (Note: full no-internet
support also needs the Fabric loader libraries bundled `--offline` currently covers the mods,
while Fabric's libraries are still fetched from the network.)

## Troubleshooting

**"Incompatible mod found" when Minecraft starts.**
This is the Fabric loader refusing to boot because *another* mod in your `mods/` folder is
incompatible with this Minecraft version, usually a leftover jar from an earlier manual attempt.
It is **not** caused by the mods Unbound installs (those are matched to your version). As of v1.1,
Unbound automatically moves any other jars in `mods/` into **`mods/.unbound-disabled/`** before
installing, so this shouldn't happen. Just re-run Unbound. If you still hit it, open your `mods/`
folder and move everything except the two jars Unbound added out of it. (Power users with a curated
mod set can keep their mods in place with the CLI's `--keep-other-mods`.)

**"I got chat-banned / muted on a server, will this fix it?"**
No. Unbound reverts the **client-side account/version chat block** ("Chat disabled by account
settings") that Minecraft applies to flagged accounts. It does **not** undo a **ban or mute issued
by a server's moderators**. That lives on the server, and no client-side mod can change it.

## License & attribution

- **No Chat Restrictions** is by **Aizistral**, licensed **WTFPL** 
 <https://github.com/Aizistral-Studios/No-Chat-Restrictions>. Unbound ships/downloads the
 official builds and does not reuse the mod's name or icon as its own.
- Unbound's own installer code is licensed **MIT** see [LICENSE](LICENSE).
