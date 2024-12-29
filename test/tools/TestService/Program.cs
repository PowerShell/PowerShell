// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ServiceProcess;

namespace TestService
{
    internal static class Program
    {
       private static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
