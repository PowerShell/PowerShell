using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Microsoft.PowerShell;

namespace TestPSReadLine
{
    class Program
    {
        static void CauseCrash(ConsoleKeyInfo? key = null, object arg = null)
        {
            throw new Exception("intentional crash for test purposes");
        }

        [STAThread]
        static void Main()
        {
            //Box(new List<string> {"abc", "  def", "this is something coo"});

            var iss = InitialSessionState.CreateDefault2();
            var rs = RunspaceFactory.CreateRunspace(iss);
            rs.Open();
            Runspace.DefaultRunspace = rs;

            PSConsoleReadLine.SetOptions(new SetPSReadlineOption
            {
                EditMode = EditMode.Emacs,
                HistoryNoDuplicates = true,
            });
            PSConsoleReadLine.SetKeyHandler(new[] {"Ctrl+LeftArrow"}, PSConsoleReadLine.ShellBackwardWord, "", "");
            PSConsoleReadLine.SetKeyHandler(new[] {"Ctrl+RightArrow"}, PSConsoleReadLine.ShellNextWord, "", "");
            PSConsoleReadLine.SetKeyHandler(new[] {"F4"}, PSConsoleReadLine.HistorySearchBackward, "", "");
            PSConsoleReadLine.SetKeyHandler(new[] {"F5"}, PSConsoleReadLine.HistorySearchForward, "", "");
            //PSConsoleReadLine.SetKeyHandler(new[] {"Ctrl+D,Ctrl+E"}, PSConsoleReadLine.EnableDemoMode, "", "");
            //PSConsoleReadLine.SetKeyHandler(new[] {"Ctrl+D,Ctrl+D"}, PSConsoleReadLine.DisableDemoMode, "", "");
            // PSConsoleReadLine.SetKeyHandler(new[] {"Ctrl+D,Ctrl+C"}, PSConsoleReadLine.CaptureScreen, "", "");
            PSConsoleReadLine.SetKeyHandler(new[] {"Ctrl+D,Ctrl+P"}, PSConsoleReadLine.InvokePrompt, "", "");
            PSConsoleReadLine.SetKeyHandler(new[] {"Ctrl+D,Ctrl+X"}, CauseCrash, "", "");
            PSConsoleReadLine.SetKeyHandler(new[] {"F6"}, PSConsoleReadLine.PreviousLine, "", "");
            PSConsoleReadLine.SetKeyHandler(new[] {"F7"}, PSConsoleReadLine.NextLine, "", "");
            PSConsoleReadLine.SetKeyHandler(new[] {"F2"}, PSConsoleReadLine.ValidateAndAcceptLine, "", "");
            PSConsoleReadLine.SetKeyHandler(new[] {"Enter"}, PSConsoleReadLine.AcceptLine, "", "");


            EngineIntrinsics executionContext;
            using (var ps = PowerShell.Create(RunspaceMode.CurrentRunspace))
            {
                executionContext =
                    ps.AddScript("$ExecutionContext").Invoke<EngineIntrinsics>().FirstOrDefault();

                // This is a workaround to ensure the command analysis cache has been created before
                // we enter into ReadLine.  It's a little slow and infrequently needed, so just
                // uncomment host stops responding, run it once, then comment it out again.
                //ps.Commands.Clear();
                //ps.AddCommand("Get-Command").Invoke();
            }

            while (true)
            {
                Console.Write("TestHostPS> ");

                var line = PSConsoleReadLine.ReadLine(null, executionContext);
                Console.WriteLine(line);
                line = line.Trim();
                if (line.Equals("exit"))
                    Environment.Exit(0);
                if (line.Equals("cmd"))
                    PSConsoleReadLine.SetOptions(new SetPSReadlineOption {EditMode = EditMode.Windows});
                if (line.Equals("emacs"))
                    PSConsoleReadLine.SetOptions(new SetPSReadlineOption {EditMode = EditMode.Emacs});
                if (line.Equals("vi"))
                    PSConsoleReadLine.SetOptions(new SetPSReadlineOption {EditMode = EditMode.Vi});
                if (line.Equals("nodupes"))
                    PSConsoleReadLine.SetOptions(new SetPSReadlineOption {HistoryNoDuplicates = true});
            }
        }
    }
}
