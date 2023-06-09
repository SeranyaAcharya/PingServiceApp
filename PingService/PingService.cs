using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ServiceFabric.Services.Communication.AspNetCore;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Data;
using System.Fabric.Description;
using System.Globalization;
using Microsoft.Owin.Hosting;
using Microsoft.Owin;
/*using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;*/

namespace PingService
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    internal sealed class PingService : StatefulService
    {
        

        public PingService(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[]
            {
                new ServiceReplicaListener(serviceContext =>
                    new HttpSysCommunicationListener(serviceContext,"ServiceEndpoint", (_, listener) =>
                    {
                        string nodeName = serviceContext.NodeContext.NodeName;
                        int nodeNum=0;
                        for(int i = 5; i < nodeName.Length; i++)
                        {
                            nodeNum=10*nodeNum+(nodeName[i]-'0');
                        }
                        string port = 20000 + nodeNum + "";
                        
                        string url = $"http://+:{port}";

                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        var builder = WebApplication.CreateBuilder();

                        builder.Services
                                    .AddSingleton<StatefulServiceContext>(serviceContext)
                                    .AddSingleton<IReliableStateManager>(this.StateManager);
                        builder.WebHost
                                    .UseHttpSys()
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseServiceFabricIntegration(listener, ServiceFabricIntegrationOptions.UseUniqueServiceUrl)
                                    .UseUrls(url);
                        
                        // Add services to the container.
                        builder.Services.AddControllersWithViews();

                        var app = builder.Build();
                        
                        // Configure the HTTP request pipeline.
                        if (!app.Environment.IsDevelopment())
                        {
                        app.UseExceptionHandler("/Home/Error");
                        }
                        app.UseStaticFiles();

                        app.UseRouting();

                        app.UseAuthorization();

                        app.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");

                        return app;

                    }), listenOnSecondary: true)
            };
        }

        
    }
}