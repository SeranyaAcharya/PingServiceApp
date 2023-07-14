using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Health;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Owin.BuilderProperties;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json.Linq;
using PingService.Controllers;
using static System.Net.WebRequestMethods;

namespace PingClient
{

    internal sealed class PingClient : StatelessService
    {
        public PingClient(StatelessServiceContext context)
            : base(context)
        { }

        private Random random = new Random();
        private static ServicePartitionResolver servicePartitionResolver = ServicePartitionResolver.GetDefault();
        private static List<ResolvedServiceEndpoint> resolvedEndpoints = new List<ResolvedServiceEndpoint>();

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var client = new System.Fabric.FabricClient();
            //loop chalao

            int pingFrequency = GetPingFrequencyFromConfig();
                TimeSpan delayBetweenPings = TimeSpan.FromSeconds(1.0 / pingFrequency);
                
                HttpClient httpClient = new HttpClient();
            
                while (!cancellationToken.IsCancellationRequested)
                {
                long i = random.Next(0, 100);
                /*if (i == 100) break;*/
                Uri myServiceUri = new Uri("fabric:/PingServiceApp/PingService"+i);
                /*i++;*/
                    long currPartitionKey = GetRandomPartitionKey();
                    ResolvedServicePartition partition = await servicePartitionResolver.ResolveAsync(myServiceUri, new ServicePartitionKey(currPartitionKey),
                            CancellationToken.None);//does it give you empty list
                    resolvedEndpoints = partition.Endpoints.ToList();
                    bool allEndpointsStale = true;
                    foreach (var resolvedEndpoint in resolvedEndpoints)
                    {
                        string httpEndpoint = "";
                        try
                        {
                            // Parse the JSON string
                            var jsonDocument = JsonDocument.Parse(resolvedEndpoint.Address);

                            // Get the "Endpoints" property
                            if (jsonDocument.RootElement.TryGetProperty("Endpoints", out var endpointsProperty))
                            {
                                // Get the inner JSON object with the empty key
                                if (endpointsProperty.TryGetProperty("", out var endpointProperty))
                                {
                                    // Get the HTTP endpoint value
                                    httpEndpoint = endpointProperty.GetString();

                                    // Use the httpEndpoint value as needed

                                }
                            }
                        }
                        catch (JsonException)
                        {
                            // Handle JSON parsing error
                        }

                        try
                        {
                            //what if null
                            string s = httpEndpoint;
                            HttpResponseMessage response = await httpClient.GetAsync(httpEndpoint);

                            if (response.IsSuccessStatusCode)
                            {
                                allEndpointsStale = false;
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            //which exception
                            continue;
                        }

                    }
                    Guid currGuid = partition.Info.Id;
                    PartitionHealth partitionHealth = client.HealthManager.GetPartitionHealthAsync(currGuid).Result;
                    //if partitions are getting destabilised then also forced refresh
                    if (allEndpointsStale && partitionHealth.AggregatedHealthState == System.Fabric.Health.HealthState.Ok)
                    {
                        partition = await servicePartitionResolver.ResolveAsync(partition,
                                CancellationToken.None);

                        resolvedEndpoints.Clear();
                        resolvedEndpoints = partition.Endpoints.ToList();
                    }
                    await Task.Delay(delayBetweenPings, cancellationToken);

                }
                httpClient.Dispose();

                /*client.ServiceManager.UnregisterServiceNotificationFilterAsync(filterId).Wait();*/
            

        }


        private long GetRandomPartitionKey()
        {

            long randomPartitionIndex = random.NextInt64(-9223372036854775808, 9223372036854775807);

            return randomPartitionIndex;
        }

        private static int GetPingFrequencyFromConfig()
        {
            CodePackageActivationContext context = FabricRuntime.GetActivationContext();
            var configSettings = context.GetConfigurationPackageObject("Config").Settings;
            var data = configSettings.Sections["PingClientConfig"];
            string pingFrequencyValue = data.Parameters["PingFrequencyInSeconds"].Value;

            if (int.TryParse(pingFrequencyValue, out int pingFrequency)) { return pingFrequency; }

            else return 10;

        }

    }

}
/*var filter = new ServiceNotificationFilterDescription()
            {
                Name = myServiceUri,
                MatchNamePrefix = true,
            };
            client.ServiceManager.ServiceNotificationFilterMatched += async (s, e) =>
            {
                var castedEventArgs = (System.Fabric.FabricClient.ServiceManagementClient.ServiceNotificationEventArgs)e;

                var notification = castedEventArgs.Notification;
                Console.WriteLine(
                    "[{0}] received notification for service '{1}'",
                    DateTime.UtcNow,
                    notification.ServiceName);

            };
            var filterId = client.ServiceManager.RegisterServiceNotificationFilterAsync(filter).Result;*/