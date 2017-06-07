using System;
using System.IO;

namespace Xunit.Runner.DotNet
{
    class XunitProjectAssembly2 : XunitProjectAssembly
    {
        public TestAssemblyConfiguration ConfigurationStream
            => LoadConfiguration(AssemblyFilename);

        static TestAssemblyConfiguration LoadConfiguration(string assemblyName)
        {
            var stream = GetConfigurationStreamForAssembly(assemblyName);
            return stream == null ? new TestAssemblyConfiguration() : ConfigReader.Load(stream);
        }

        static Stream GetConfigurationStreamForAssembly(string assemblyFilename)
        {
            // get parent directory
            if (string.IsNullOrEmpty(assemblyFilename))
                return null;

            var directory = Path.GetDirectoryName(assemblyFilename);
            var assemblyName = Path.GetFileNameWithoutExtension(assemblyFilename);

            var pathWithAssemblyName = Path.Combine(directory, assemblyName);

            // See if there's a directory with the assembly name. this might be the case for appx
            if (Directory.Exists(pathWithAssemblyName))
            {
                var runnerJsonPathAssemblyName = Path.Combine(pathWithAssemblyName, $"{assemblyName}.xunit.runner.json");
                var runnerJsonPath = Path.Combine(pathWithAssemblyName, "xunit.runner.json");
                try
                {
                    if (File.Exists(runnerJsonPathAssemblyName))
                        return File.OpenRead(runnerJsonPathAssemblyName);

                    if (File.Exists(runnerJsonPath))
                        return File.OpenRead(runnerJsonPath);
                }
                // if I/O exception is occured, dismiss the exception, any ohter exception will be thrown as is
                catch (IOException ex) { }

            }

            // Fallback to directory with assembly
            var runnerJsonDirectoryAssemblyName = Path.Combine(directory, $"{assemblyName}.xunit.runner.json");
            var runnerJsonDirectory = Path.Combine(directory, "xunit.runner.json");
            try
            {
                if (File.Exists(runnerJsonDirectoryAssemblyName))
                    return File.OpenRead(runnerJsonDirectoryAssemblyName);

                if (File.Exists(runnerJsonDirectory))
                    return File.OpenRead(runnerJsonDirectory);
            }
            // if I/O exception is occured, dismiss the exception, any ohter exception will be thrown as is
            catch (IOException ex) { }

            return null;
        }
    }
}
