using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;



namespace FabricClient
{

    internal sealed class FabricClient : StatelessService
    {

        public FabricClient(StatelessServiceContext context)
            : base(context)
        { }

        private Random random = new Random();
        private static ServicePartitionResolver servicePartitionResolver = ServicePartitionResolver.GetDefault();
        private static List<ResolvedServiceEndpoint> resolvedEndpoints = new List<ResolvedServiceEndpoint>();

        


        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var filter = new ServiceNotificationFilterDescription()
            {
                Name = new Uri("fabric:/PingServiceApp/PingService"),
                MatchNamePrefix = true,
            };
            MyCommunicationClientFactory myCommunicationClientFactory = new MyCommunicationClientFactory();
            Uri myServiceUri = new Uri("fabric:/PingServiceApp/PingService");
            var myServicePartitionClient = new ServicePartitionClient<MyCommunicationClient>(
                myCommunicationClientFactory,
                myServiceUri,
                new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(GetRandomPartitionKey())); // Use random partition key


            while (!cancellationToken.IsCancellationRequested)
            {
                ResolvedServicePartition partition = await servicePartitionResolver.ResolveAsync(myServiceUri, new ServicePartitionKey(), CancellationToken.None);

                resolvedEndpoints.Clear();
                resolvedEndpoints = partition.Endpoints.ToList();
                bool allEndpointsStale = true;
                HttpClient httpClient = new HttpClient();
                foreach (var resolvedEndpoint in resolvedEndpoints)
                {
                    httpClient.BaseAddress = new Uri(resolvedEndpoint.Address);
                    HttpResponseMessage response = await httpClient.GetAsync("api/endpoint");

                    if (response.IsSuccessStatusCode)
                    {
                        allEndpointsStale = false;
                        break; 
                    }
                }
                httpClient.Dispose();
                if (allEndpointsStale)
                {
                    partition = await servicePartitionResolver.ResolveAsync(myServiceUri, new ServicePartitionKey(), CancellationToken.None);

                    resolvedEndpoints.Clear();
                    resolvedEndpoints = partition.Endpoints.ToList();
                }
                int pingFrequency = GetPingFrequencyFromConfig();
                await Task.Delay(TimeSpan.FromSeconds(pingFrequency), cancellationToken);
            }
        }

        
        private long GetRandomPartitionKey()
        {
            int totalPartitions = GetTotalPartitionsFromConfig();
            int randomPartitionIndex = random.Next(0, totalPartitions);

            return randomPartitionIndex;
        }

        private int GetTotalPartitionsFromConfig()
        {
            CodePackageActivationContext context = FabricRuntime.GetActivationContext();
            var configSettings = context.GetConfigurationPackageObject("Config").Settings;
            var data = configSettings.Sections["PartitionCountConfig"];
            string partitionCountValue = data.Parameters["NumOfPartitions"].Value;

            if (int.TryParse(partitionCountValue, out int partitionCount)) { return partitionCount; }

            else return 10;
        }
        private static int GetPingFrequencyFromConfig()
        {
            CodePackageActivationContext context = FabricRuntime.GetActivationContext();
            var configSettings = context.GetConfigurationPackageObject("Config").Settings;
            var data = configSettings.Sections["PingServiceConfig"];
            string pingFrequencyValue = data.Parameters["PingFrequencyInSeconds"].Value;

            if (int.TryParse(pingFrequencyValue, out int pingFrequency)) { return pingFrequency; }

            else return 5;

        }

    }

}
/*private int GetPingFrequencyFromConfig()
    {
        // Read ping frequency from the configuration file
        // Modify this section based on your specific configuration structure and logic
        // Example for reading from settings.xml:
        XDocument configXml = XDocument.Load("settings.xml");
        XNamespace ns = "http://schemas.microsoft.com/2011/01/fabric";
        XElement pingFrequencyElement = configXml.Root?.Element(ns + "Section")?
            .Elements(ns + "Parameter")
            .FirstOrDefault(p => p.Attribute("Name")?.Value == "PingFrequencyInSeconds");

        if (pingFrequencyElement != null && int.TryParse(pingFrequencyElement.Attribute("Value")?.Value, out int pingFrequency))
        {
            return pingFrequency;
        }
        else
        {
            // Handle invalid or missing configuration value
            // Return a default value or throw an exception
            *//*return DefaultPingFrequency;*//*
            return 5;
        }

    }*/

/*protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            
            //loop mein to hit the target service(ping frequency)
            MyCommunicationClientFactory myCommunicationClientFactory;
            myCommunicationClientFactory = new MyCommunicationClientFactory();
            Uri myServiceUri = new Uri("fabric:/PingServiceApp/PingService");


            var myServicePartitionClient = new ServicePartitionClient<MyCommunicationClient>(
                myCommunicationClientFactory,
                myServiceUri,
                new Microsoft.ServiceFabric.Services.Client.ServicePartitionKey(0L));//use rndom

            var result = await myServicePartitionClient.InvokeWithRetryAsync(async (client) =>
            {
                // Communicate with the service using the client.//http client object and invoke an api to connect
            },
               CancellationToken.None);

            //to read ping frequency
            //settings.xml -->config driven model read--> for flexibility 

        }*/
//Doubt: var result kaha aayega
//client kisse connection banaayega
//ping service ka doubt
//logical
