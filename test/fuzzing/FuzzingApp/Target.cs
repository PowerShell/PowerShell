// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using SharpFuzz;
using System.Management.Automation.Remoting;

namespace FuzzTests
{
    public static class Target
    {
        public static void ExtractToken(ReadOnlySpan<byte> tokenResponse)
        {
            RemoteSessionHyperVSocketClient.ExtractToken(tokenResponse);
        }
    }
}

