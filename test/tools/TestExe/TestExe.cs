using System;
using System.Threading;
using System.Diagnostics;

namespace TestExe
{
    class TestExe
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                switch(args[0].ToLowerInvariant())
                {
                    case "-echoargs":
                        EchoArgs(args);
                        break;
                    case "-createchildprocess":
                        CreateChildProcess(args);
                        break;
                    default:
                        Console.WriteLine("Unknown test {0}", args[0]);
                        break;
                }
            }
            else
            {
                Console.WriteLine("Test not specified");
            }
        }

        // <Summary>
        // Echos back to stdout the arguments passed in
        // </Summary>
        static void EchoArgs(string[] args)
        {
            for (int i = 1; i < args.Length; i++)
            {
                Console.WriteLine("Arg {0} is <{1}>", i-1, args[i]);
            }
        }

        // <Summary>
        // First argument is the number of child processes to create which are instances of itself
        // Processes automatically exit after 100 seconds
        // </Summary>
        static void CreateChildProcess(string[] args)
        {
            if (args.Length > 1)
            {
                uint num = UInt32.Parse(args[1]);
                for (uint i = 0; i < num; i++)
                {
                    Process child = new Process();
                    child.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                    child.StartInfo.Arguments = "-createchildprocess";
                    child.Start();
                }
            }
            // sleep is needed so the process doesn't exit before the test case kill it
            Thread.Sleep(100000);
        }
    }
}
