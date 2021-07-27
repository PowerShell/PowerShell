// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace TestExe
{
    internal class TestExe
    {
        private static int Main(string[] args)
        {
            if (args.Length > 0)
            {
                switch (args[0].ToLowerInvariant())
                {
                    case "-echoargs":
                        EchoArgs(args);
                        break;
                    case "-createchildprocess":
                        CreateChildProcess(args);
                        break;
                    case "-returncode":
                        // Used to test functionality depending on $LASTEXITCODE, like &&/|| operators
                        Console.WriteLine(args[1]);
                        return int.Parse(args[1]);
                    case "-stderr":
                        Console.Error.WriteLine(args[1]);
                        break;
                    case "-writetofile":
                        string fileName = args[1];
                        WriteInputToFile(fileName);
                        break;
                    case "-writebytes":
                        string hexStr = args[1];
                        WriteBytesFromHex(hexStr);
                        break;
                    default:
                        Console.WriteLine("Unknown test {0}", args[0]);
                        break;
                }

                return 0;
            }

            Console.WriteLine("Test not specified");
            return 0;
        }

        private static void WriteInputToFile(string path)
        {
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(Environment.CurrentDirectory, path);
            }

            path = Path.GetFullPath(path);

            using (Stream inStream = Console.OpenStandardInput())
            using (FileStream outFileStream = File.OpenWrite(path))
            {
                inStream.CopyTo(outFileStream);
            }
        }

        private static void WriteBytesFromHex(string hexStr)
        {
            using (Stream outStream = Console.OpenStandardOutput())
            {
                for (int i = 0; i < hexStr.Length; i += 2)
                {
                    byte value = Convert.ToByte(hexStr.Substring(i, 2), fromBase: 16);
                    outStream.WriteByte(value);
                }
            }
        }

        // <Summary>
        // Echos back to stdout the arguments passed in
        // </Summary>
        private static void EchoArgs(string[] args)
        {
            for (int i = 1; i < args.Length; i++)
            {
                Console.WriteLine("Arg {0} is <{1}>", i - 1, args[i]);
            }
        }

        // <Summary>
        // First argument is the number of child processes to create which are instances of itself
        // Processes automatically exit after 100 seconds
        // </Summary>
        private static void CreateChildProcess(string[] args)
        {
            if (args.Length > 1)
            {
                uint num = UInt32.Parse(args[1]);
                for (uint i = 0; i < num; i++)
                {
                    Process child = new Process();
                    child.StartInfo.FileName = Environment.ProcessPath;
                    child.StartInfo.Arguments = "-createchildprocess";
                    child.Start();
                }
            }
            // sleep is needed so the process doesn't exit before the test case kill it
            Thread.Sleep(100000);
        }
    }
}
