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
           // LoadPortMapFromFile();
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new ServiceReplicaListener[]
            {
                new ServiceReplicaListener(serviceContext =>
                    new KestrelCommunicationListener(serviceContext, (tempUrl, listener) =>
                    {
                        key= serviceContext.PartitionId.ToString();

                     /*   lock (portMapLock)
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
                        }*/

                        ServiceEventSource.Current.ServiceMessage(serviceContext, $"Starting Kestrel on {url}");

                        var builder = WebApplication.CreateBuilder();

                        builder.Services
                                    .AddSingleton<StatefulServiceContext>(serviceContext)
                                    .AddSingleton<IReliableStateManager>(this.StateManager);
                        builder.WebHost
                                    .UseKestrel()
                                    .UseContentRoot(Directory.GetCurrentDirectory())
                                    .UseUrls(SavePortMapToFile($"{serviceContext.PartitionId.ToString()}_{serviceContext.ReplicaId.ToString()}"));

                        
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

        private string SavePortMapToFile(string key)
        {
            int port = 49152;
            lock(portMapLock)
            {
                using (FileStream fileStream = new FileStream(portMapFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    {
                        fileStream.Lock(0, fileStream.Length);
                        // Acquire an exclusive lock on the file
                        // Read the contents of the file
                        string contents = File.ReadAllText(portMapFilePath);
                        PortMapFileData fileData = JsonConvert.DeserializeObject<PortMapFileData>(contents);
                        if (fileData.PortMap.ContainsKey(key))
                        {
                            port = fileData.PortMap[key];
                        }
                        else
                        {
                            fileData.PortMap[key] = fileData.MaxPortNumber + 1;
                            fileData.MaxPortNumber = fileData.MaxPortNumber + 1;
                            port = fileData.PortMap[key];
                        }

                        File.WriteAllText(portMapFilePath, JsonConvert.SerializeObject(fileData));


                    }

                }

            }
           

          return $"http://+:{port}";
          
        }
        private class PortMapFileData
        {
            public Dictionary<string, int> PortMap { get; set; }
            public int MaxPortNumber { get; set; } = 49152;
        }
    }

}
