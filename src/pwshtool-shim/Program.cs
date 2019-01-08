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

            if (isWindows)
            {
                var winPath = Path.Combine(currentPath, "win", "pwsh.dll");
                Console.WriteLine ($"Attempting to start {winPath}");

                if (File.Exists(winPath))
                {
                    System.Diagnostics.Process.Start("dotnet", $"{winPath}").WaitForExit();
                }
                else
                {
                    throw new FileNotFoundException(winPath);
                }
            }
            else
            {
                var unixPath = Path.Combine(currentPath, "unix", "pwsh.dll");
                Console.WriteLine ($"Attempting to start {unixPath}");

                if (File.Exists(unixPath))
                {
                    System.Diagnostics.Process.Start("dotnet", $"{unixPath}").WaitForExit();
                }
                else
                {
                    throw new FileNotFoundException(unixPath);
                }
            }
        }
    }
}
