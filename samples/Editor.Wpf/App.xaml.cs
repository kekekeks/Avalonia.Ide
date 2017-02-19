using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Avalonia.Ide.CompletionEngine.DnlibMetadataProvider;

namespace Editor.Wpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            var metadatapath = Environment.GetCommandLineArgs()[1];

            base.OnStartup(e);
            new MainWindow() {DataContext = new MainWindowModel(new DnlibMetadataProvider(metadatapath), null)}
                .ShowDialog();
        }
    }
}
