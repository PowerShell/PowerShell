// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Remoting;
using Xunit;

namespace PSTests.Parallel
{
    public class NamedPipeTests
    {        
        [Fact]
        public static void TestCustomPipeNameCreation()
        {
            string pipeName1 = Path.GetRandomFileName();
            string pipeName2 = Path.GetRandomFileName();

            RemoteSessionNamedPipeServer.CreateCustomNamedPipeServer(pipeName1);
            Console.WriteLine(GetPipePath(pipeName1));
            Assert.True(File.Exists(GetPipePath(pipeName1)));

            // The second call to this method would override the first named pipe.
            RemoteSessionNamedPipeServer.CreateCustomNamedPipeServer(pipeName2);
            Assert.True(File.Exists(GetPipePath(pipeName2)));

            // Previous pipe should have been cleaned up.
            Assert.True(!File.Exists(GetPipePath(pipeName1)));
        }

        [Fact]
        public static void TestCustomPipeNameCreationTooLongOnNonWindows()
        {
            var longPipeName = "DoggoipsumwaggywagssmolborkingdoggowithalongsnootforpatsdoingmeafrightenporgoYapperporgolongwatershoobcloudsbigolpupperlengthboy";

            if (!Platform.IsWindows)
            {
                Assert.Throws<InvalidOperationException>(() => 
                    RemoteSessionNamedPipeServer.CreateCustomNamedPipeServer(longPipeName));
            }
            else
            {
                RemoteSessionNamedPipeServer.CreateCustomNamedPipeServer(longPipeName);
                Assert.True(File.Exists(GetPipePath(longPipeName)));
            }
        }

        private static string GetPipePath(string pipeName)
        {
            if (Platform.IsWindows)
            {
                return $@"\\.\pipe\{pipeName}";
            }
            return $@"{Path.GetTempPath()}CoreFxPipe_{pipeName}";
        }
    }
}
