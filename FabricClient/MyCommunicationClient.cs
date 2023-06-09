using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FabricClient
{
    public class MyCommunicationClient : ICommunicationClient
    {
        public string endpointStr { get; set; }
        
        public ResolvedServiceEndpoint Endpoint { get; set; }

        public string ListenerName { get; set; }

        public ResolvedServicePartition ResolvedServicePartition { get; set; }
    }
}
