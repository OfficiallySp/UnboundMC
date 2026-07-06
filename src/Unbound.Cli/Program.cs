using System.Text.Json;
using Unbound.Core;
using Unbound.Core.Models;

// Unbound CLI — drives the Core install engine headlessly (for development and power users).

// Maintenance command: refresh the embedded offline fallback jars. Doesn't touch .minecraft.
if (args.Length > 0 && args[0] == "bundle")
    return await RunBundleAsync(args[1..]);

string? path = null;
string? mc = null;
bool offline = false, uninstall = false, list = false, keepOtherMods = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--path": path = ArgValue(args, ref i); break;
        case "--mc": mc = ArgValue(args, ref i); break;
        case "--offline": offline = true; break;
        case "--keep-other-mods": keepOtherMods = true; break;
        case "uninstall": uninstall = true; break;
        case "list": list = true; break;
        case "-h" or "--help" or "help":
            PrintHelp();
            return 0;
    }
}

path ??= MinecraftLocator.GetDefaultPath();
if (!MinecraftLocator.LooksValid(path))
{
    Console.Error.WriteLine($"Couldn't find a Minecraft installation at: {path}");
    Console.Error.WriteLine("Run the official launcher at least once, or pass --path <.minecraft>.");
    return 2;
}
Console.WriteLine($"Minecraft folder: {path}");

if (uninstall)
{
    var removed = UnboundUninstaller.Uninstall(path);
    Console.WriteLine(removed ? "Unbound uninstalled (profile + mods removed)." : "Nothing to uninstall.");
    return 0;
}

using var http = UnboundHttp.Create();
var modrinth = new ModrinthClient(http);

if (list)
{
    var versions = await modrinth.GetSupportedGameVersionsAsync(ModrinthClient.NoChatRestrictionsId);
    Console.WriteLine("No Chat Restrictions supports these Minecraft versions (Fabric):");
    foreach (var v in versions) Console.WriteLine("  " + v);
    return 0;
}

if (mc is null)
{
    var supported = await modrinth.GetSupportedGameVersionsAsync(ModrinthClient.NoChatRestrictionsId);
    var installed = MinecraftLocator.GetInstalledVersionIds(path);
    var pick = supported.FirstOrDefault(installed.Contains) ?? supported.FirstOrDefault();
    if (pick is null)
    {
        Console.Error.WriteLine("Could not determine a Minecraft version to install for.");
        return 3;
    }
    Console.WriteLine($"No --mc given; using {pick}. (See 'unbound list' for all supported versions.)");
    mc = pick;
}

var orchestrator = new InstallOrchestrator(http);
var progress = new Progress<InstallProgress>(p => Console.WriteLine($"  [{p.Stage}] {p.Message}"));

try
{
    var result = await orchestrator.InstallAsync(
        new InstallOptions
        {
            MinecraftPath = path,
            MinecraftVersion = mc,
            ForceOffline = offline,
            KeepOtherMods = keepOtherMods,
        },
        progress);

    Console.WriteLine();
    Console.WriteLine($"Installed Unbound for Minecraft {result.MinecraftVersion} (Fabric {result.LoaderVersion}).");
    foreach (var m in result.Mods)
        Console.WriteLine($"   - {m.Name}  [{m.Source}]  {m.FileName}");

    if (result.SetAsideMods.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"Moved {result.SetAsideMods.Count} other mod(s) into mods/{ModsFolderGuard.DisabledDirName} so the game starts cleanly:");
        foreach (var f in result.SetAsideMods)
            Console.WriteLine($"   - {f}");
        Console.WriteLine("   (want them back? move them out of that folder. Or re-run with --keep-other-mods.)");
    }

    Console.WriteLine();
    Console.WriteLine("Open the Minecraft launcher, pick the \"Unbound\" profile, and play.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("Install failed: " + ex.Message);
    return 1;
}

static string ArgValue(string[] args, ref int i)
{
    if (i + 1 >= args.Length)
        throw new ArgumentException($"Missing value after {args[i]}");
    return args[++i];
}

// Downloads No Chat Restrictions + Fabric API for the given Minecraft versions and rewrites the
// embedded fallback set. A version is only kept if BOTH mods are available (offline needs both).
static async Task<int> RunBundleAsync(string[] a)
{
    var versions = new List<string>();
    var outDir = Path.Combine("src", "Unbound.Core", "assets", "bundled");
    for (int i = 0; i < a.Length; i++)
    {
        if (a[i] == "--out") outDir = a[++i];
        else versions.Add(a[i]);
    }
    if (versions.Count == 0)
    {
        Console.Error.WriteLine("Usage: unbound bundle <mcVersion>... [--out <dir>]");
        return 2;
    }

    Directory.CreateDirectory(outDir);
    foreach (var f in Directory.GetFiles(outDir, "*.jar")) File.Delete(f);

    using var http = UnboundHttp.Create();
    var modrinth = new ModrinthClient(http);
    (string Key, string Id, string Name)[] mods =
    [
        ("fabric-api", ModrinthClient.FabricApiId, "Fabric API"),
        ("no-chat-restrictions", ModrinthClient.NoChatRestrictionsId, "No Chat Restrictions"),
    ];

    var manifest = new List<object>();
    int kept = 0;
    foreach (var ver in versions)
    {
        Console.WriteLine($"== {ver} ==");
        var staged = new List<(string FileName, byte[] Bytes, string Key)>();
        bool complete = true;
        foreach (var (key, id, name) in mods)
        {
            var v = await modrinth.GetBestVersionAsync(id, ver, "fabric");
            if (v is null) { Console.Error.WriteLine($"   ! no {name} fabric build for {ver}"); complete = false; break; }
            var file = ModrinthClient.PrimaryFile(v);
            var bytes = await modrinth.DownloadAsync(file);
            staged.Add(($"{key}-fabric-{ver}.jar", bytes, key));
            Console.WriteLine($"   + {name}: {file.Filename} ({bytes.Length / 1024} KB)");
        }
        if (!complete) { Console.Error.WriteLine($"   skipped {ver} — not fully available"); continue; }

        foreach (var (fileName, bytes, key) in staged)
        {
            await File.WriteAllBytesAsync(Path.Combine(outDir, fileName), bytes);
            manifest.Add(new { ModKey = key, MinecraftVersion = ver, FileName = fileName });
        }
        kept++;
    }

    var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
    await File.WriteAllTextAsync(Path.Combine(outDir, "manifest.json"), json);
    Console.WriteLine($"\nBundled {kept} version(s) into {outDir}");
    return 0;
}

static void PrintHelp()
{
    Console.WriteLine(
        """
        Unbound — one-click No Chat Restrictions installer

        Usage:
          unbound [--mc <version>] [--path <.minecraft>] [--offline]
          unbound list                 list the Minecraft versions No Chat Restrictions supports
          unbound uninstall            remove Unbound's launcher profile and mods

        Options:
          --mc <version>   Minecraft version to install for (e.g. 1.21.1). Auto-picked if omitted.
          --path <dir>     path to .minecraft (default: auto-detect for this OS)
          --offline        skip downloads and install from bundled jars only
          --keep-other-mods  don't move other jars in mods/ aside (advanced; may cause
                             "Incompatible mod found" if a leftover mod is incompatible)

        Maintenance:
          unbound bundle <version>... [--out <dir>]
                           refresh the embedded offline fallback jars for the given versions
        """);
}
