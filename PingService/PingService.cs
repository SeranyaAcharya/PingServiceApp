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
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Newtonsoft.Json;

namespace PingService
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    
    internal sealed class PingService : StatefulService
    {
        private IReliableStateManager stateManager;
        private int maxPortNum;
        private static Dictionary<string, int> portMap;
        // Get the temporary path using the environmental variable TEMP
        private static string tempPath = Path.GetTempPath();
        private static string key;
        private static string url;
        // Combine the temporary path with the desired filename
        private static string portMapFilePath = Path.Combine(tempPath, "portMap.json");
        private static readonly object portMapLock = new object();



        public PingService(StatefulServiceContext context)
            : base(context)
        {
            stateManager = this.StateManager;
            // Load the port map from the file if it exists, otherwise create a new dictionary
            /*if (File.Exists(portMapFilePath))
            {
                string json = File.ReadAllText(portMapFilePath);
                PortMapFileData fileData = JsonConvert.DeserializeObject<PortMapFileData>(json);
                portMap = fileData.PortMap;

            }
            else
            {
                maxPortNum = 49152;
                portMap = new Dictionary<string, int>();
            }*/
            LoadPortMapFromFile();
        }


        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[]
            {
                new ServiceReplicaListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, (_, listener) =>
                    {

                        key= serviceContext.PartitionId.ToString();

                        lock (portMapLock)
                        {
                            if (portMap.ContainsKey(key))
                            {
                                int portNumber = portMap[key];
                                url = $"http://+:{portNumber}";
                            }
                            else
                            {
                                SavePortMapToFile();
                            }
                        }


                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        var builder = WebApplication.CreateBuilder();

                        builder.Services
                                    .AddSingleton<StatefulServiceContext>(serviceContext)
                                    .AddSingleton<IReliableStateManager>(this.StateManager);
                        builder.WebHost
                                    .UseKestrel()
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

        private void LoadPortMapFromFile()
        {
            lock (portMapLock)
            {
                if (File.Exists(portMapFilePath))
                {
                    string json;
                    using (var fileStream = new FileStream(portMapFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        using (var reader = new StreamReader(fileStream))
                        {
                            json = reader.ReadToEnd();
                        }
                    }
                    PortMapFileData fileData = JsonConvert.DeserializeObject<PortMapFileData>(json);
                    portMap = fileData.PortMap;
                    maxPortNum = fileData.MaxPortNumber;
                }
                else
                {
                    maxPortNum = 49152;
                    portMap = new Dictionary<string, int>();
                }
            }
        }

        private void SavePortMapToFile()
        {
            lock (portMapLock)
            {
                if (portMap.Count == 0)
                {
                    maxPortNum = 49152;
                }
                else
                {
                    maxPortNum = portMap.Values.Max() + 1;
                }

                int portNumber = maxPortNum;

                portMap.Add(key, portNumber);

                var fileData = new PortMapFileData { PortMap = portMap, MaxPortNumber = maxPortNum };
                string json = JsonConvert.SerializeObject(fileData);

                using (var fileStream = new FileStream(portMapFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (var writer = new StreamWriter(fileStream))
                    {
                        writer.Write(json);
                    }
                }

                url = $"http://+:{portNumber}";
            }
        }
        private class PortMapFileData
        {
            public Dictionary<string, int> PortMap { get; set; }
            public int MaxPortNumber { get; set; }
        }




    }

}
/*string nodeName = serviceContext.NodeContext.NodeName;
int nodeNum = 0;
for (int i = 5; i < nodeName.Length; i++)
{
    nodeNum = 10 * nodeNum + (nodeName[i] - '0');
}*/