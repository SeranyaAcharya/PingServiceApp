using System;
using System.Diagnostics;
using System.Fabric;
using System.Fabric.Description;
using System.Threading;
using System.Threading.Tasks;
using FabricClient;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Runtime;

namespace FabricClient
{
    internal static class Program
    {
        private static ServicePartitionResolver servicePartitionResolver = ServicePartitionResolver.GetDefault();
        private static List<ResolvedServiceEndpoint> resolvedEndpoints = new List<ResolvedServiceEndpoint>();
        private static void Main()
        {
            var client = new System.Fabric.FabricClient(new string[] { "[cluster_endpoint]:[client_port]" });

            var filter = new ServiceNotificationFilterDescription()
            {
                Name = new Uri("fabric:/my_application"),
                MatchNamePrefix = true,
            };

            client.ServiceManager.ServiceNotificationFilterMatched += (s, e) => OnNotification(e);

            var filterId = client.ServiceManager.RegisterServiceNotificationFilterAsync(filter).Result;

            client.ServiceManager.UnregisterServiceNotificationFilterAsync(filterId).Wait();

        }


        private static void OnNotification(EventArgs e)
        {
            var castedEventArgs = (System.Fabric.FabricClient.ServiceManagementClient.ServiceNotificationEventArgs)e;

            var notification = castedEventArgs.Notification;

            ResolvedServicePartition partition = servicePartitionResolver.ResolveAsync(new Uri("fabric:/PingServiceApp/PingService"), new ServicePartitionKey(), CancellationToken.None).GetAwaiter().GetResult();
            resolvedEndpoints = partition.Endpoints.ToList();
        }

    }
    
}

/*try
{
    // The ServiceManifest.XML file defines one or more service type names.
    // Registering a service maps a service type name to a .NET type.
    // When Service Fabric creates an instance of this service type,
    // an instance of the class is created in this host process.

    ServiceRuntime.RegisterServiceAsync("FabricClientType",
        context => new FabricClient(context)).GetAwaiter().GetResult();

    ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(FabricClient).Name);

    // Prevents this host process from terminating so services keep running.
    Thread.Sleep(Timeout.Infinite);
}
catch (Exception e)
{
    ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
    throw;
}
*/



