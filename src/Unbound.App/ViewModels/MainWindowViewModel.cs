using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Unbound.Core;
using Unbound.Core.Models;

namespace Unbound.App.ViewModels;

public enum WizardStep { Setup, Installing, Done, Error }

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSetup), nameof(IsInstalling), nameof(IsDone), nameof(IsError))]
    private WizardStep _step = WizardStep.Setup;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private string _minecraftPath = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private bool _pathValid;

    [ObservableProperty] private string _pathHint = "";
    [ObservableProperty] private bool _loadingVersions;
    [ObservableProperty] private bool _offlineNote;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private string? _selectedVersion;

    [ObservableProperty] private string _progressText = "";
    [ObservableProperty] private string _resultBody = "";
    [ObservableProperty] private string _errorText = "";
    [ObservableProperty] private string _uninstallStatus = "";

    public ObservableCollection<string> Versions { get; } = new();

    public bool IsSetup => Step == WizardStep.Setup;
    public bool IsInstalling => Step == WizardStep.Installing;
    public bool IsDone => Step == WizardStep.Done;
    public bool IsError => Step == WizardStep.Error;

    public MainWindowViewModel()
    {
        DetectPath();
        _ = LoadVersionsAsync();
    }

    /// <summary>Called from the view when the user browses to a folder.</summary>
    public void SetPath(string path)
    {
        UninstallStatus = "";
        MinecraftPath = path;
        Validate();
        _ = LoadVersionsAsync();
    }

    private void DetectPath()
    {
        MinecraftPath = MinecraftLocator.GetDefaultPath();
        Validate();
    }

    private void Validate()
    {
        PathValid = MinecraftLocator.LooksValid(MinecraftPath);
        PathHint = PathValid
            ? "Found your Minecraft installation."
            : "Not found — run the official launcher once, or browse to your .minecraft folder.";
    }

    private async Task LoadVersionsAsync()
    {
        LoadingVersions = true;
        OfflineNote = false;
        try
        {
            using var http = UnboundHttp.Create();
            var modrinth = new ModrinthClient(http);
            var supported = await modrinth.GetSupportedGameVersionsAsync(ModrinthClient.NoChatRestrictionsId);
            var installed = PathValid ? MinecraftLocator.GetInstalledVersionIds(MinecraftPath) : Array.Empty<string>();
            ApplyVersions(supported, installed);
        }
        catch
        {
            // Offline: only the versions we can install from bundled jars are guaranteed to work.
            OfflineNote = true;
            ApplyVersions(BundledMods.AvailableVersions, Array.Empty<string>());
        }
        finally
        {
            LoadingVersions = false;
        }
    }

    private void ApplyVersions(IReadOnlyList<string> supported, IReadOnlyList<string> installed)
    {
        Versions.Clear();
        foreach (var v in supported) Versions.Add(v);
        // Default to the newest version the player already has installed, else the newest supported.
        SelectedVersion = supported.FirstOrDefault(installed.Contains) ?? supported.FirstOrDefault();
    }

    private bool CanInstall() => PathValid && !string.IsNullOrEmpty(SelectedVersion);

    [RelayCommand(CanExecute = nameof(CanInstall))]
    private async Task InstallAsync()
    {
        Step = WizardStep.Installing;
        ProgressText = "Starting…";

        // Progress is constructed on the UI thread, so callbacks marshal back automatically.
        var progress = new Progress<InstallProgress>(p => ProgressText = p.Message);
        try
        {
            var orchestrator = new InstallOrchestrator();
            var result = await Task.Run(() => orchestrator.InstallAsync(
                new InstallOptions { MinecraftPath = MinecraftPath, MinecraftVersion = SelectedVersion! },
                progress));

            var mods = string.Join("\n", result.Mods.Select(m => $"   •  {m.Name}  ({m.Source})"));
            ResultBody =
                $"Minecraft {result.MinecraftVersion}  ·  Fabric {result.LoaderVersion}\n\n{mods}\n\n" +
                "Open the Minecraft launcher, choose the “Unbound” profile, and play.";
            Step = WizardStep.Done;
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
            Step = WizardStep.Error;
        }
    }

    [RelayCommand]
    private void Uninstall()
    {
        try
        {
            var removed = UnboundUninstaller.Uninstall(MinecraftPath);
            UninstallStatus = removed ? "Removed Unbound's profile and mods." : "Nothing to remove.";
        }
        catch (Exception ex)
        {
            UninstallStatus = "Couldn't remove: " + ex.Message;
        }
    }

    [RelayCommand]
    private void Restart()
    {
        UninstallStatus = "";
        Step = WizardStep.Setup;
    }

    [RelayCommand]
    private void BackToSetup()
    {
        ErrorText = "";
        Step = WizardStep.Setup;
    }
}
