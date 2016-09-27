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
            var directory = Path.GetDirectoryName(assemblyFilename);
            var assemblyName = Path.GetFileNameWithoutExtension(assemblyFilename);

            var pathWithAssemblyName = Path.Combine(directory, assemblyName);

            // See if there's a directory with the assm name. this might be the case for appx
            if (Directory.Exists(pathWithAssemblyName))
            {
                if (File.Exists(Path.Combine(pathWithAssemblyName, $"{assemblyName}.xunit.runner.json")))
                    return File.OpenRead(Path.Combine(pathWithAssemblyName, $"{assemblyName}.xunit.runner.json"));

                if (File.Exists(Path.Combine(pathWithAssemblyName, "xunit.runner.json")))
                    return File.OpenRead(Path.Combine(pathWithAssemblyName, "xunit.runner.json"));
            }

            // Fallback to directory with assembly
            if (File.Exists(Path.Combine(directory, $"{assemblyName}.xunit.runner.json")))
                return File.OpenRead(Path.Combine(directory, $"{assemblyName}.xunit.runner.json"));

            if (File.Exists(Path.Combine(directory, "xunit.runner.json")))
                return File.OpenRead(Path.Combine(directory, "xunit.runner.json"));

            return null;
        }
    }
}
