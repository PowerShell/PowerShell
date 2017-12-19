/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

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
