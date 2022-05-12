// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Reflection;
using System.Threading;

using static Microsoft.PowerShell.ComInterfaces;

namespace Microsoft.PowerShell
{
    internal static class TaskbarJumpList
    {
        // Creating a JumpList entry takes around 55ms when the PowerShell process is interactive and
        // owns the current window (otherwise it does a fast exit anyway). Since there is no 'GET' like API,
        // we always have to execute this call because we do not know if it has been created yet.
        // The JumpList does persist as long as the filepath of the executable does not change but there
        // could be disruptions to it like e.g. the bi-annual Windows update, we decided to
        // not over-optimize this and always create the JumpList as a non-blocking background STA thread instead.
        internal static void CreateRunAsAdministratorJumpList()
        {
            // The STA apartment state is not supported on NanoServer and Windows IoT.
            // Plus, there is not need to create jump list in those environment anyways.
            if (!Platform.IsWindowsDesktop)
            {
                return;
            }

            // Some COM APIs are implicitly STA only, therefore the executing thread must run in STA.
            var thread = new Thread(() =>
            {
                try
                {
                    TaskbarJumpList.CreateElevatedEntry(ConsoleHostStrings.RunAsAdministrator);
                }
                catch (Exception exception)
                {
                    // Due to COM threading complexity there might still be sporadic failures but they can be
                    // ignored as creating the JumpList is not critical and persists after its first creation.
                    Debug.Fail($"Creating 'Run as Administrator' JumpList failed. {exception}");
                }
            });

            try
            {
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
            catch (System.Threading.ThreadStartException)
            {
                // STA may not be supported on some platforms
            }
        }

        private static void CreateElevatedEntry(string title)
        {
            // Check startupInfo first to know if the current shell is interactive and owns a window before proceeding
            // This check is fast (less than 1ms) and allows for quick-exit
            GetStartupInfo(out StartUpInfo startupInfo);
            const uint STARTF_USESHOWWINDOW = 0x00000001;
            const ushort SW_HIDE = 0;
            if (((startupInfo.dwFlags & STARTF_USESHOWWINDOW) == 1) && (startupInfo.wShowWindow != SW_HIDE))
            {
                string cmdPath = Assembly.GetEntryAssembly().Location.Replace(".dll", ".exe");

                // Check for maximum available slots in JumpList and start creating the custom Destination List
                var CLSID_DestinationList = new Guid(@"77f10cf0-3db5-4966-b520-b7c54fd35ed6");
                const uint CLSCTX_INPROC_SERVER = 1;
                var IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
                var hResult = CoCreateInstance(ref CLSID_DestinationList, null, CLSCTX_INPROC_SERVER, ref IID_IUnknown, out object pCustDestListobj);
                if (hResult < 0)
                {
                    Debug.Fail($"Creating ICustomDestinationList failed with HResult '{hResult}'.");
                    return;
                }

                var pCustDestList = (ICustomDestinationList)pCustDestListobj;
                hResult = pCustDestList.BeginList(out uint uMaxSlots, new Guid(@"92CA9DCD-5622-4BBA-A805-5E9F541BD8C9"), out object pRemovedItems);
                if (hResult < 0)
                {
                    Debug.Fail($"BeginList on ICustomDestinationList failed with HResult '{hResult}'.");
                    return;
                }

                if (uMaxSlots >= 1)
                {
                    // Create JumpListLink
                    var nativeShellLink = (IShellLinkW)new CShellLink();
                    var nativePropertyStore = (IPropertyStore)nativeShellLink;
                    nativeShellLink.SetPath(cmdPath);
                    nativeShellLink.SetShowCmd(0);
                    var shellLinkDataList = (IShellLinkDataListW)nativeShellLink;
                    shellLinkDataList.GetFlags(out uint flags);
                    flags |= 0x00800000; // SLDF_ALLOW_LINK_TO_LINK
                    flags |= 0x00002000; // SLDF_RUNAS_USER
                    shellLinkDataList.SetFlags(flags);
                    var PKEY_TITLE = new PropertyKey(new Guid("{F29F85E0-4FF9-1068-AB91-08002B27B3D9}"), 2);
                    hResult = nativePropertyStore.SetValue(in PKEY_TITLE, new PropVariant(title));
                    if (hResult < 0)
                    {
                        pCustDestList.AbortList();
                        Debug.Fail($"SetValue on IPropertyStore with title '{title}' failed with HResult '{hResult}'.");
                        return;
                    }

                    hResult = nativePropertyStore.Commit();
                    if (hResult < 0)
                    {
                        pCustDestList.AbortList();
                        Debug.Fail($"Commit on IPropertyStore failed with HResult '{hResult}'.");
                        return;
                    }

                    // Create collection and add JumpListLink
                    var CLSID_EnumerableObjectCollection = new Guid(@"2d3468c1-36a7-43b6-ac24-d3f02fd9607a");
                    const uint CLSCTX_INPROC_HANDLER = 2;
                    const uint CLSCTX_INPROC = CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER;
                    var ComSvrInterface_GUID = new Guid(@"555E2D2B-EE00-47AA-AB2B-39F953F6B339");
                    hResult = CoCreateInstance(ref CLSID_EnumerableObjectCollection, null, CLSCTX_INPROC, ref IID_IUnknown, out object instance);
                    if (hResult < 0)
                    {
                        pCustDestList.AbortList();
                        Debug.Fail($"Creating IObjectCollection failed with HResult '{hResult}'.");
                        return;
                    }

                    var pShortCutCollection = (IObjectCollection)instance;
                    pShortCutCollection.AddObject((IShellLinkW)nativePropertyStore);

                    // Add collection to custom destination list and commit the result
                    hResult = pCustDestList.AddUserTasks((IObjectArray)pShortCutCollection);
                    if (hResult < 0)
                    {
                        pCustDestList.AbortList();
                        Debug.Fail($"AddUserTasks on ICustomDestinationList failed with HResult '{hResult}'.");
                        return;
                    }

                    pCustDestList.CommitList();
                }
            }
        }
    }
}
