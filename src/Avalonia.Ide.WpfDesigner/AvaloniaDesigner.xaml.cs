using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Avalonia.Designer.AppHost;
using Avalonia.Designer.Comm;

namespace Avalonia.Designer
{
    /// <summary>
    /// Interaction logic for PerpexDesigner.xaml
    /// </summary>
    public partial class AvaloniaDesigner
    {
        public static readonly DependencyProperty TargetExeProperty = DependencyProperty.Register(
            "TargetExe", typeof (string), typeof (AvaloniaDesigner), new FrameworkPropertyMetadata(TargetExeChanged));

        private static void TargetExeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AvaloniaDesigner) d).RestartProcess();
        }
        public string TargetExe
        {
            get { return (string) GetValue(TargetExeProperty); }
            set { SetValue(TargetExeProperty, value); }
        }

        public static readonly DependencyProperty XamlProperty = DependencyProperty.Register(
            "Xaml", typeof (string), typeof (AvaloniaDesigner), new FrameworkPropertyMetadata(XamlChanged));

        private static void XamlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((AvaloniaDesigner) d).OnXamlChanged();
        }

        public string Xaml
        {
            get { return (string) GetValue(XamlProperty); }
            set { SetValue(XamlProperty, value); }
        }

        public static readonly DependencyProperty SourceAssemblyProperty = DependencyProperty.Register(
            "SourceAssembly", typeof (string), typeof (AvaloniaDesigner), new FrameworkPropertyMetadata(XamlChanged));

        public string SourceAssembly
        {
            get { return (string) GetValue(SourceAssemblyProperty); }
            set { SetValue(SourceAssemblyProperty, value); }
        }

        private readonly ProcessHost _host;

        public AvaloniaDesigner():this(new DesignerConfiguration
        {
            NetCoreAppHostPath = @"C:\Users\keks\Projects\GitHub\Perspex\src\tools\Avalonia.Designer.HostApp\bin\Debug\netcoreapp2.0\Avalonia.Designer.HostApp.dll",
            NetFxAppHostPath = @"C:\Users\keks\Projects\GitHub\Perspex\src\tools\Avalonia.Designer.HostApp.NetFX\bin\Debug\Avalonia.Designer.HostApp.exe"
        })
        {
            
        }

        public AvaloniaDesigner(DesignerConfiguration config)
        {
            _host = new ProcessHost(config);
            InitializeComponent();
            BindingOperations.SetBinding(State, TextBox.TextProperty,
                new Binding(nameof(ProcessHost.State)) {Source = _host, Mode = BindingMode.OneWay});

            _host.PropertyChanged += _host_PropertyChanged;
            DesignerView.DataContext = new HostedAppModel(_host);
        }

        private void _host_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProcessHost.WindowHandle))
                DesignerView.Visibility = _host.WindowHandle == IntPtr.Zero ? Visibility.Hidden : Visibility.Visible;
        }



        public void KillProcess()
        {
            _host.Kill();
        }

        bool CheckTargetExeOrSetError()
        {
            if (string.IsNullOrEmpty(TargetExe))
            {
                _host.State = "No target exe found";
                return false;
            }

            if (File.Exists(TargetExe ?? ""))
                return true;
            _host.State = "No target binary found, build your project";
            return false;
        }

        public void RestartProcess()
        {
            KillProcess();
            if(!CheckTargetExeOrSetError())
                return;
            if(string.IsNullOrEmpty(Xaml))
                return;
            if (Xaml != null)
                _host.UpdateXaml(Xaml, SourceAssembly);
            _host.Start(TargetExe, Xaml, SourceAssembly);
        }

        private void OnXamlChanged()
        {
            if (!CheckTargetExeOrSetError())
                return;
            if (!_host.IsAlive)
                _host.Start(TargetExe, Xaml, SourceAssembly);
            else
                _host.UpdateXaml(Xaml ?? "", SourceAssembly);
        }

    }
}
