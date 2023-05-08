// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    #region Type Info Database

    internal sealed partial class TypeInfoDataBase
    {
    }
    #endregion

    #region View Definitions: common data
    internal sealed partial class AppliesTo
    {
#if false
        internal void AddAppliesToTypeGroup (string typeGroupName)
        {
            TypeGroupReference tgr = new TypeGroupReference ();

            tgr.name = typeGroupName;
            this.referenceList.Add (tgr);
        }
#endif
        internal void AddAppliesToType(string typeName)
        {
            TypeReference tr = new TypeReference();

            tr.name = typeName;
            this.referenceList.Add(tr);
        }
    }

    #endregion
}
