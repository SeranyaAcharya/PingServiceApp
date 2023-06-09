using Microsoft.ServiceFabric.Services.Communication.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FabricClient
{
    class MyExceptionHandler : IExceptionHandler
    {
        public bool TryHandleException(ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, out ExceptionHandlingResult? result)
        {

            // if exceptionInformation.Exception is unknown (let the next IExceptionHandler attempt to handle it)
            result = null;
            return false;
        }
    }
}
