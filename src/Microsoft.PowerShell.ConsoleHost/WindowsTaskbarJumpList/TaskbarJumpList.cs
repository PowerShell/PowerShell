using System;
using System.Reflection;
using static Microsoft.PowerShell.ComInterfaces;

namespace Microsoft.PowerShell
{
    internal static class TaskbarJumpList
    {
        internal static void CreateElevatedEntry(string title)
        {
            // check if the current shell owns a window
            GetStartupInfo(out StartUpInfo startupInfo);
            var STARTF_TITLEISLINKNAME = 0x00000800;
            if (startupInfo.lpTitle == null || (startupInfo.dwFlags & STARTF_TITLEISLINKNAME) != STARTF_TITLEISLINKNAME)
            {

                string cmdPath = Assembly.GetEntryAssembly().Location.Replace(".dll", ".exe"); // TODO: think of a better solution

                // Check for maximum available slots in JumpList and start creating the custom Destination List
                var CLSID_DestinationList = new Guid(@"77f10cf0-3db5-4966-b520-b7c54fd35ed6");
                const uint CLSCTX_INPROC_SERVER = 1;
                var IID_IUnknown = new Guid("00000000-0000-0000-C000-000000000046");
                CheckAndReturn(CoCreateInstance(ref CLSID_DestinationList, null, CLSCTX_INPROC_SERVER, ref IID_IUnknown, out object pCustDestListobj));
                var pCustDestList = (ICustomDestinationList)pCustDestListobj;
                CheckAndReturn(pCustDestList.BeginList(out uint uMaxSlots, new Guid(@"92CA9DCD-5622-4BBA-A805-5E9F541BD8C9"), out object pRemovedItems));

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
                    CheckAndReturn(nativePropertyStore.SetValue(ref PKEY_TITLE, new PropVariant(title)));
                    CheckAndReturn(nativePropertyStore.Commit());

                    // Create collection and add JumpListLink
                    var CLSID_EnumerableObjectCollection = new Guid(@"2d3468c1-36a7-43b6-ac24-d3f02fd9607a");
                    const uint CLSCTX_INPROC_HANDLER = 2;
                    const uint CLSCTX_INPROC = CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER;
                    var ComSvrInterface_GUID = new Guid(@"555E2D2B-EE00-47AA-AB2B-39F953F6B339");
                    CheckAndReturn(CoCreateInstance(ref CLSID_EnumerableObjectCollection, null, CLSCTX_INPROC, ref IID_IUnknown, out object instance));
                    var pShortCutCollection = (IObjectCollection)instance;
                    pShortCutCollection.AddObject((IShellLinkW)nativePropertyStore);

                    // Add collection to custom destination list and commit the result
                    CheckAndReturn(pCustDestList.AddUserTasks((IObjectArray)pShortCutCollection));
                    pCustDestList.CommitList();
                }
            }
        }

        private static void CheckAndReturn(HResult hResult)
        {
            if (hResult < 0)
            {
                throw new Exception($"HResult from COM call was negative, which indicates a failure. Was: '{hResult}'");
            }
        }
    }
}
