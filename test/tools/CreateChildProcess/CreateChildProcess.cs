using System;
using System.Diagnostics;
using System.Threading;

namespace CreateChildProcess
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                uint num = UInt32.Parse(args[0]);
                for (uint i = 0; i < num; i++)
                {
                    Process child = new Process();
                    child.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                    child.Start();
                }
            }
            // sleep is needed so the process doesn't exit before the test case kill it
            Thread.Sleep(100000);
        }
    }
}
