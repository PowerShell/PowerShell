// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using static TestRawService.NativeMethods;

namespace TestRawService
{
    public class Service
    {
        private const string ServiceName = nameof(Service);

        private static readonly ServiceMainCallback s_mainCallback = ServiceMain;
        private static readonly ServiceControlCallbackEx s_controlCallback = ServiceControl;

        private static IntPtr s_statusHandle;
        private static SERVICE_STATUS s_status;

        private static void Main()
        {
            Span<SERVICE_TABLE_ENTRY> entries = stackalloc SERVICE_TABLE_ENTRY[2];
            entries.Clear();
            try
            {
                entries[0] = new SERVICE_TABLE_ENTRY
                {
                    name = Marshal.StringToHGlobalUni(ServiceName),
                    callback = Marshal.GetFunctionPointerForDelegate(s_mainCallback),
                };

                if (!StartServiceCtrlDispatcher(ref entries.GetPinnableReference()))
                {
                    throw new Win32Exception();
                }
            }
            finally
            {
                Marshal.FreeHGlobal(entries[0].name);
            }
        }

        private static void ServiceMain(int argc, IntPtr argv)
        {
            s_statusHandle = RegisterServiceCtrlHandlerEx(ServiceName, s_controlCallback);

            if (s_statusHandle == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            s_status.serviceType = TYPE_WIN32_OWN_PROCESS;
            s_status.controlsAccepted = ACCEPT_STOP;

            s_status.currentState = STATE_RUNNING;

            if (!SetServiceStatus(s_statusHandle, s_status))
            {
                throw new Win32Exception();
            }
        }

        private static int ServiceControl(int control, int eventType, IntPtr eventData, IntPtr eventContext)
        {
            throw new Exception();
        }
    }

    internal static class NativeMethods
    {
        public const int ACCEPT_STOP = 0x00000001;

        public const int CONTROL_CONTINUE = 0x00000003;
        public const int CONTROL_PAUSE = 0x00000002;
        public const int CONTROL_STOP = 0x00000001;

        public const int STATE_PAUSED = 0x00000007;
        public const int STATE_RUNNING = 0x00000004;
        public const int STATE_STOPPED = 0x00000001;

        public const int TYPE_WIN32_OWN_PROCESS = 0x00000010;

        private const string Advapi32 = "advapi32.dll";

        [DllImport(Advapi32, CharSet = CharSet.Unicode, EntryPoint = "RegisterServiceCtrlHandlerExW")]
        public static extern IntPtr RegisterServiceCtrlHandlerEx(string serviceName, ServiceControlCallbackEx callback, IntPtr userData = default);

        [DllImport(Advapi32)]
        public static extern bool SetServiceStatus(IntPtr serviceStatusHandle, in SERVICE_STATUS status);

        [DllImport(Advapi32, CharSet = CharSet.Unicode, EntryPoint = "StartServiceCtrlDispatcherW")]
        public static extern bool StartServiceCtrlDispatcher(ref SERVICE_TABLE_ENTRY entries);

        public delegate int ServiceControlCallbackEx(int control, int eventType, IntPtr eventData, IntPtr eventContext);

        public delegate void ServiceMainCallback(int argc, IntPtr argv);

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_STATUS
        {
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_TABLE_ENTRY
        {
            public IntPtr name;
            public IntPtr callback;
        }
    }
}
