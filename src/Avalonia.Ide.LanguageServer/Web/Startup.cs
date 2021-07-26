using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Ide.LanguageServer.Editor;
using Avalonia.Ide.LanguageServer.ProjectModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.Ide.LanguageServer.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDeveloperExceptionPage();
            app.UseWebSockets();
            app.UseStaticFiles();
            app.UseMiddleware<PreviewerMiddleware>();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });
        }


        public static string Start(EditorSessionManager mgr, CancellationToken cancel, int port = 0)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://localhost:{port}/")
                .UseStartup<Startup>()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureServices(sc => sc.AddSingleton(mgr))
                .Build();
            host.Start();
            var addr = host.ServerFeatures.Get<IServerAddressesFeature>();
            Log.Message($"Web listening on {string.Join(", ", addr.Addresses)}");
            cancel.Register(() => { host.StopAsync().Wait(); });
            return addr.Addresses.First();
        }
    }
}