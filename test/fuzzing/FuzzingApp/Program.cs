// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using SharpFuzz;
using System.Management.Automation.Remoting;

public class Target
{
    public static void ExtractToken(ReadOnlySpan<byte> tokenResponse)
    {
        RemoteSessionHyperVSocketClient.ExtractToken(tokenResponse);
    }
}

public static class Program
{
    public static void Main(string[] args)
    {
        FuzzTargetMethod(args);
    }

    public static void FuzzTargetMethod(string[] args)
    {
        if (args == null)
        {
            Console.WriteLine("args was null");
            args = new string[0];
        }

        try
        {
            Fuzzer.LibFuzzer.Run(Target.ExtractToken);
        }
        catch (System.ArgumentNullException nex)
        {
            Console.WriteLine($"ArgumentNullException in main: {nex.Message}");
            Console.WriteLine($"Stack Trace: {nex.StackTrace}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in main: {ex.Message}");
            Console.WriteLine($"Exception type: {ex.GetType()}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }
}
