using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Avalonia.Designer.AppHost;
using Avalonia.Designer.Comm;

namespace Avalonia.Designer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            var app =  new App();
            const string targetExe = "--exe=";
            const string xaml = "--xaml=";
            const string source = "--source=";

            app.Run(new DemoWindow(
                args.Where(a => a.StartsWith(targetExe)).Select(a => a.Substring(targetExe.Length)).FirstOrDefault(),
                args.Where(a => a.StartsWith(xaml)).Select(a => a.Substring(xaml.Length)).FirstOrDefault(),
                args.Where(a => a.StartsWith(source)).Select(a => a.Substring(source.Length)).FirstOrDefault()));
        }
    }
}
