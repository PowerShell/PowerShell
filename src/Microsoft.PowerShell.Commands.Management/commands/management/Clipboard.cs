// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.PowerShell.Commands.Internal
{
    internal static partial class Clipboard
    {
        private static bool? _clipboardSupported;

        // Used if an external clipboard is not available, e.g. if xclip is missing.
        // This is useful for testing in CI as well.
        private static string _internalClipboard;

        // DONT KEEP THIS MERGE LATER
        // TODO: maybe convert to take scriptblock ???? that could be cool!!
        // TODO: ref https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.scriptblock?view=powershellsdk-7.4.0
        //           https://stackoverflow.com/questions/75260697/how-does-get-set-in-a-scriptblock
        //           https://learn.microsoft.com/en-us/dotnet/api/system.management.automation.scriptblock.invoke?view=powershellsdk-7.4.0
        //           [ScriptBlock]::Create('write-host "qqq $_ bbb"').InvokeWithContext($null, [PSVariable]::new('_', 'aaa'))

        private static string StartProcess(
            string tool,
            Object[] args,
            string stdin = "",
            bool readStdout = true)
        {
            ProcessStartInfo startInfo = new();
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.FileName = tool;

            if (args != null)
            {
                foreach (Object arg in args)
                {
                    startInfo.ArgumentList.Add(Convert.ToString(arg));
                }
            }

            string stdout = string.Empty;

            using (Process process = new())
            {
                process.StartInfo = startInfo;
                try
                {
                    process.Start();
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    _clipboardSupported = false;
                    return string.Empty;
                }

                process.StandardInput.Write(stdin);
                process.StandardInput.Close();

                if (readStdout)
                {
                    stdout = process.StandardOutput.ReadToEnd();
                }

                process.WaitForExit(250);
                _clipboardSupported = process.ExitCode == 0;
            }

            return stdout;
        }

        public static string GetText(Hashtable clipboardActions)
        {
            // need errors for null clipboardActions
            // need errors for null clipboardActions["Paste"]
            
            Hashtable pasteAction = (Hashtable)clipboardActions["Paste"];

            if (pasteAction.ContainsKey("UseWindowsClipboard") && ((bool)pasteAction["UseWindowsClipboard"]))
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new PlatformNotSupportedException("1REPLACEME Can't run Windows clipboard methods on non-Windows platforms!");
                }
                else
                {
                    string clipboardText = string.Empty;
                    ExecuteOnStaThread(() => GetTextImpl(out clipboardText));
                    return clipboardText;
                }
            }
            else if (pasteAction.ContainsKey("Command"))
            {
                string tool = (string)pasteAction["Command"];
                Object[] args = (Object[])pasteAction["Arguments"];

                return StartProcess(tool, args);
            }
            else
            {
                // write-warning "PSClipboardActions.Paste.Command is not set" ???
                return _internalClipboard ?? string.Empty;
            }
        }

        public static void SetText(Hashtable clipboardActions, string text)
        {
            // need error for null clipboardActions
            // need error for null clipboardActions["Clip"]

            Hashtable clipAction = (Hashtable)clipboardActions["Clip"];

            if (clipAction.ContainsKey("UseWindowsClipboard") && (bool)clipAction["UseWindowsClipboard"])
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new PlatformNotSupportedException("1REPLACEME Can't run Windows clipboard methods on non-Windows platforms!");
                }
                else
                {
                    ExecuteOnStaThread(() => SetClipboardData(Tuple.Create(text, CF_UNICODETEXT)));
                }
            }
            else if (clipAction.ContainsKey("Command"))
            {
                string tool = (string)clipAction["Command"];
                Object[] args = (Object[])clipAction["Arguments"];

                StartProcess(tool, args, text, readStdout: false);
            }
            else
            {
                // write-warning "PSClipboardActions.Clip.Command is not set" ???
                _internalClipboard = text;
            }
        }

        public static void SetRtf(string plainText, string rtfText)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            if (s_CF_RTF == 0)
            {
                s_CF_RTF = RegisterClipboardFormat("Rich Text Format");
            }

            ExecuteOnStaThread(() => SetClipboardData(
                Tuple.Create(plainText, CF_UNICODETEXT),
                Tuple.Create(rtfText, s_CF_RTF)));
        }

        private const uint GMEM_MOVEABLE = 0x0002;
        private const uint GMEM_ZEROINIT = 0x0040;
        private const uint GHND = GMEM_MOVEABLE | GMEM_ZEROINIT;

        [LibraryImport("kernel32.dll")]
        private static partial IntPtr GlobalAlloc(uint flags, UIntPtr dwBytes);

        [LibraryImport("kernel32.dll")]
        private static partial IntPtr GlobalFree(IntPtr hMem);

        [LibraryImport("kernel32.dll")]
        private static partial IntPtr GlobalLock(IntPtr hMem);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GlobalUnlock(IntPtr hMem);

        [LibraryImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static partial void CopyMemory(IntPtr dest, IntPtr src, uint count);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsClipboardFormatAvailable(uint format);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool OpenClipboard(IntPtr hWndNewOwner);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool CloseClipboard();

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool EmptyClipboard();

        [LibraryImport("user32.dll")]
        private static partial IntPtr GetClipboardData(uint format);

        [LibraryImport("user32.dll")]
        private static partial IntPtr SetClipboardData(uint format, IntPtr data);

        [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
        private static partial uint RegisterClipboardFormat(string lpszFormat);

        private const uint CF_TEXT = 1;
        private const uint CF_UNICODETEXT = 13;

        private static uint s_CF_RTF;

        private static bool GetTextImpl(out string text)
        {
            try
            {
                if (IsClipboardFormatAvailable(CF_UNICODETEXT))
                {
                    if (OpenClipboard(IntPtr.Zero))
                    {
                        var data = GetClipboardData(CF_UNICODETEXT);
                        if (data != IntPtr.Zero)
                        {
                            data = GlobalLock(data);
                            text = Marshal.PtrToStringUni(data);
                            GlobalUnlock(data);
                            return true;
                        }
                    }
                }
                else if (IsClipboardFormatAvailable(CF_TEXT))
                {
                    if (OpenClipboard(IntPtr.Zero))
                    {
                        var data = GetClipboardData(CF_TEXT);
                        if (data != IntPtr.Zero)
                        {
                            data = GlobalLock(data);
                            text = Marshal.PtrToStringAnsi(data);
                            GlobalUnlock(data);
                            return true;
                        }
                    }
                }
            }
            catch
            {
                // Ignore exceptions
            }
            finally
            {
                CloseClipboard();
            }

            text = string.Empty;
            return false;
        }

        private static bool SetClipboardData(params Tuple<string, uint>[] data)
        {
            try
            {
                if (!OpenClipboard(IntPtr.Zero))
                {
                    return false;
                }

                EmptyClipboard();

                foreach (var d in data)
                {
                    if (!SetSingleClipboardData(d.Item1, d.Item2))
                    {
                        return false;
                    }
                }
            }
            finally
            {
                CloseClipboard();
            }

            return true;
        }

        private static bool SetSingleClipboardData(string text, uint format)
        {
            IntPtr hGlobal = IntPtr.Zero;
            IntPtr data = IntPtr.Zero;

            try
            {
                uint bytes;
                if (format == s_CF_RTF || format == CF_TEXT)
                {
                    bytes = (uint)(text.Length + 1);
                    data = Marshal.StringToHGlobalAnsi(text);
                }
                else if (format == CF_UNICODETEXT)
                {
                    bytes = (uint)((text.Length + 1) * 2);
                    data = Marshal.StringToHGlobalUni(text);
                }
                else
                {
                    // Not yet supported format.
                    return false;
                }

                if (data == IntPtr.Zero)
                {
                    return false;
                }

                hGlobal = GlobalAlloc(GHND, (UIntPtr)bytes);
                if (hGlobal == IntPtr.Zero)
                {
                    return false;
                }

                IntPtr dataCopy = GlobalLock(hGlobal);
                if (dataCopy == IntPtr.Zero)
                {
                    return false;
                }

                CopyMemory(dataCopy, data, bytes);
                GlobalUnlock(hGlobal);

                if (SetClipboardData(format, hGlobal) != IntPtr.Zero)
                {
                    // The clipboard owns this memory now, so don't free it.
                    hGlobal = IntPtr.Zero;
                }
            }
            catch
            {
                // Ignore failures
            }
            finally
            {
                if (data != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(data);
                }

                if (hGlobal != IntPtr.Zero)
                {
                    GlobalFree(hGlobal);
                }
            }

            return true;
        }

        private static void ExecuteOnStaThread(Func<bool> action)
        {
            const int RetryCount = 5;
            int tries = 0;

            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
            {
                while (tries++ < RetryCount && !action())
                {
                    // wait until RetryCount or action
                }

                return;
            }

            Exception exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    while (tries++ < RetryCount && !action())
                    {
                        // wait until RetryCount or action
                    }
                }
                catch (Exception e)
                {
                    exception = e;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (exception != null)
            {
                throw exception;
            }
        }
    }
}
