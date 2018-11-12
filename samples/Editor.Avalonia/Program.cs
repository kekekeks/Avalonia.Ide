using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;
using Avalonia.Ide.CompletionEngine.SrmMetadataProvider;
using Avalonia.Threading;
using Editor;
using Editor.Avalonia;

class Program
{
    static void Main(string[] args)
    {
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .SetupWithoutStarting();
        var window = new MainWindow();
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            window.DataContext = await GetViewModel();
            window.Show();
        });
        Application.Current.Run(window);
    }

    static async Task<MainWindowModel> GetViewModel()
    {
        var args = Environment.GetCommandLineArgs();
        string path;
        if (args.Length > 1)
        {
            path = args[1];
            
        }
        else
        {
            var dlg = new OpenFileDialog();
            var results = await dlg.ShowAsync();
            if (results?.Length > 0)
                path = results[0];
            else
                path = "--self";
        }
        if (path == "--self")
            path = typeof(Program).GetTypeInfo().Assembly.GetModules()[0].FullyQualifiedName;
        return new MainWindowModel(new MetadataReader(new SrmMetadataProvider()).GetForTargetAssembly(path),
            null, Path.GetFileNameWithoutExtension(path));
    }
}