using System;
using System.Text;
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
                    case "-writeoutput":
                        WriteStandardOutput();
                        break;
                    case "-readinput":
                        ReadStandardInput(args);
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

        static void WriteStandardOutput()
        {
            // write a string of characters (bytes), this closest resembles a binary
            // write the string "test" with an accent over the e
            byte[] testbytes = new byte[] { 116, 233, 115, 116 };
            foreach(byte b in testbytes)
            {
                Console.Write((char)b);
            }
        }

        static void ReadStandardInput(string[] args)
        {
            Encoding encodingToUse = null;
            if (args.Length > 1)
            {
                // the encoding page to get could be provided as an argument;
                int result;
                if ( int.TryParse(args[1], out result))
                {
                    try
                    {
                        encodingToUse = Encoding.GetEncoding(result);
                    }
                    catch
                    {
                        ;
                    }
                }
            }
            if ( encodingToUse == null )
            {
                encodingToUse = Encoding.GetEncoding(28591);
            }
            Console.InputEncoding = encodingToUse;
            string pipedText = Console.In.ReadToEnd();
            foreach(char c in pipedText.ToCharArray())
            {
                Console.WriteLine((byte)c);
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
