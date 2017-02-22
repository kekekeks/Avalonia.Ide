using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Ide.CompletionEngine.AssemblyMetadata;
using Avalonia.Ide.CompletionEngine.DnlibMetadataProvider;
using Avalonia.Ide.CompletionEngine.SrmMetadataProvider;
using Microsoft.Win32;

namespace Editor.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static string GetPath()
        {
            var dlg = new OpenFileDialog();
            if (dlg.ShowDialog() != true)
                Environment.Exit(0);
            return dlg.FileName;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var args = Environment.GetCommandLineArgs();
            var metadatapath = args.Length > 1 ? args[1] : GetPath();

            base.OnStartup(e);
            new MainWindow() {DataContext = new MainWindowModel(new MetadataReader(new SrmMetadataProvider()).GetForTargetAssembly(metadatapath), null)}
                .ShowDialog();
        }
    }
}
