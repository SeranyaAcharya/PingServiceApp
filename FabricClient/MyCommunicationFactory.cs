using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace FabricClient
{
    public class MyCommunicationClientFactory : CommunicationClientFactoryBase<MyCommunicationClient>
    {
        protected override void AbortClient(MyCommunicationClient client)
        {
        }

        protected override Task<MyCommunicationClient> CreateClientAsync(string endpoint, CancellationToken cancellationToken)
        {
            return Task.FromResult(new MyCommunicationClient()

            {

                endpointStr = endpoint

            });
        }

        protected override bool ValidateClient(MyCommunicationClient clientChannel)
        {
            return true;
        }

        protected override bool ValidateClient(string endpoint, MyCommunicationClient client)
        {
            return true;
        }
    }
}
