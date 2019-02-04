using System;
using System.IO;

namespace pwshtool_shim
{
    class Program
    {
        static void Main(string[] args)
        {
            var currentPath = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory.FullName;
            var isWindows = Environment.OSVersion.Platform == System.PlatformID.Win32NT;

            string platformFolder = isWindows ? "win" : "unix";

            string argsString = args.Length > 0 ? string.Join(" ", args) : null;
            var pwshPath = Path.Combine(currentPath, platformFolder, "pwsh.dll");
            string processArgs = string.IsNullOrEmpty(argsString) ? $"{pwshPath}" : $"{pwshPath} -c {argsString}";

            Console.WriteLine($"Attempting to start {winPath}");

            if (File.Exists(pwshPath))
            {
                System.Diagnostics.Process.Start("dotnet", processArgs).WaitForExit();
            }
            else
            {
                throw new FileNotFoundException(pwshPath);
            }
        }
    }
}
