using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Unbound.App.ViewModels;

namespace Unbound.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnBrowse(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select your .minecraft folder",
            AllowMultiple = false,
        });

        if (folders.Count > 0 && DataContext is MainWindowViewModel vm)
        {
            var path = folders[0].TryGetLocalPath();
            if (!string.IsNullOrEmpty(path)) vm.SetPath(path);
        }
    }

    private void OnClose(object? sender, RoutedEventArgs e) => Close();
}
