//---------------------------------------------------------------------
// Author: Keith Hill
// Source: https://github.com/Pscx/Pscx/blob/master/Src/EchoArgs/EchoArgs.cs
//
// Description: Very simple little console class that you can use to see
//              how PowerShell is passing parameters to legacy console
//              apps.
//
// Creation Date: March 06, 2006
//---------------------------------------------------------------------
using System;

namespace Pscx.Applications
{
    class EchoArgs
    {
        static void Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine("Arg {0} is <{1}>", i, args[i]);
            }
        }
    }
}
