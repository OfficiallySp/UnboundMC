using Unbound.Core;
using Unbound.Core.Models;

// Unbound CLI — drives the Core install engine headlessly (for development and power users).

string? path = null;
string? mc = null;
bool offline = false, uninstall = false, list = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--path": path = ArgValue(args, ref i); break;
        case "--mc": mc = ArgValue(args, ref i); break;
        case "--offline": offline = true; break;
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
        new InstallOptions { MinecraftPath = path, MinecraftVersion = mc, ForceOffline = offline },
        progress);

    Console.WriteLine();
    Console.WriteLine($"Installed Unbound for Minecraft {result.MinecraftVersion} (Fabric {result.LoaderVersion}).");
    foreach (var m in result.Mods)
        Console.WriteLine($"   - {m.Name}  [{m.Source}]  {m.FileName}");
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
        """);
}
