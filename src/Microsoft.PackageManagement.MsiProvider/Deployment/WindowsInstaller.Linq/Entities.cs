//---------------------------------------------------------------------
// <copyright file="Entities.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller.Linq
{
    // Silence warnings about style and doc-comments
    #if !CODE_ANALYSIS
    #pragma warning disable 1591
    #region Generated code

    internal class Component_ : QRecord
    {
        public string Component   { get { return this[0]; } set { this[0] = value; } }
        public string ComponentId { get { return this[1]; } set { this[1] = value; } }
        public string Directory_  { get { return this[2]; } set { this[2] = value; } }
        public string Condition   { get { return this[4]; } set { this[4] = value; } }
        public string KeyPath     { get { return this[5]; } set { this[5] = value; } }
        public ComponentAttributes Attributes
        { get { return (ComponentAttributes) this.I(3); } set { this[3] = ((int) value).ToString(); } }
    }

    internal class CreateFolder_ : QRecord
    {
        public string Directory_ { get { return this[0]; } set { this[0] = value; } }
        public string Component_ { get { return this[1]; } set { this[1] = value; } }
    }

    internal class CustomAction_ : QRecord
    {
        public string Action { get { return this[0]; } set { this[0] = value; } }
        public string Source { get { return this[2]; } set { this[2] = value; } }
        public string Target { get { return this[3]; } set { this[3] = value; } }
        public CustomActionTypes Type
        { get { return (CustomActionTypes) this.I(1); } set { this[1] = ((int) value).ToString(); } }
    }

    internal class Directory_ : QRecord
    {
        public string Directory        { get { return this[0]; } set { this[0] = value; } }
        public string Directory_Parent { get { return this[1]; } set { this[1] = value; } }
        public string DefaultDir       { get { return this[2]; } set { this[2] = value; } }
    }

    internal class DuplicateFile_ : QRecord
    {
        public string FileKey    { get { return this[0]; } set { this[0] = value; } }
        public string Component_ { get { return this[1]; } set { this[1] = value; } }
        public string File_      { get { return this[2]; } set { this[2] = value; } }
        public string DestName   { get { return this[4]; } set { this[4] = value; } }
        public string DestFolder { get { return this[5]; } set { this[5] = value; } }
    }

    internal class Feature_ : QRecord
    {
        public string Feature        { get { return this[0];    } set { this[0] = value; } }
        public string Feature_Parent { get { return this[1];    } set { this[1] = value; } }
        public string Title          { get { return this[2];    } set { this[2] = value; } }
        public string Description    { get { return this[3];    } set { this[3] = value; } }
        public int?   Display        { get { return this.NI(4); } set { this[4] = value.ToString(); } }
        public int    Level          { get { return this.I(5);  } set { this[5] = value.ToString(); } }
        public string Directory_     { get { return this[6];    } set { this[6] = value; } }
        public FeatureAttributes Attributes
        { get { return (FeatureAttributes) this.I(7); } set { this[7] = ((int) value).ToString(); } }
    }

    [DatabaseTable("FeatureComponents")]
    internal class FeatureComponent_ : QRecord
    {
        public string Feature_   { get { return this[0]; } set { this[0] = value; } }
        public string Component_ { get { return this[1]; } set { this[1] = value; } }
    }

    internal class File_ : QRecord
    {
        public string File       { get { return this[0];   } set { this[0] = value; } }
        public string Component_ { get { return this[1];   } set { this[1] = value; } }
        public string FileName   { get { return this[2];   } set { this[2] = value; } }
        public int    FileSize   { get { return this.I(3); } set { this[3] = value.ToString(); } }
        public string Version    { get { return this[4];   } set { this[4] = value; } }
        public string Language   { get { return this[5];   } set { this[5] = value; } }
        public int    Sequence   { get { return this.I(7); } set { this[7] = value.ToString(); } }
        public FileAttributes Attributes
        { get { return (FileAttributes) this.I(6); } set { this[6] = ((int) value).ToString(); } }
    }

    [DatabaseTable("MsiFileHash")]
    internal class FileHash_ : QRecord
    {
        public string File_     { get { return this[0];   } set { this[0] = value; } }
        public int    Options   { get { return this.I(1); } set { this[1] = value.ToString(); } }
        public int    HashPart1 { get { return this.I(2); } set { this[2] = value.ToString(); } }
        public int    HashPart2 { get { return this.I(3); } set { this[3] = value.ToString(); } }
        public int    HashPart3 { get { return this.I(4); } set { this[4] = value.ToString(); } }
        public int    HashPart4 { get { return this.I(5); } set { this[5] = value.ToString(); } }
    }

    [DatabaseTable("InstallExecuteSequence")]
    internal class InstallSequence_ : QRecord
    {
        public string Action    { get { return this[0];   } set { this[0] = value; } }
        public string Condition { get { return this[1];   } set { this[1] = value; } }
        public int    Sequence  { get { return this.I(2); } set { this[2] = value.ToString(); } }
    }

    internal class LaunchCondition_ : QRecord
    {
        public string Condition   { get { return this[0]; } set { this[0] = value; } }
        public string Description { get { return this[1]; } set { this[1] = value; } }
    }

    internal class Media_ : QRecord
    {
        public int    DiskId       { get { return this.I(0); } set { this[0] = value.ToString(); } }
        public int    LastSequence { get { return this.I(1); } set { this[1] = value.ToString(); } }
        public string DiskPrompt   { get { return this[2];   } set { this[2] = value; } }
        public string Cabinet      { get { return this[3];   } set { this[3] = value; } }
        public string VolumeLabel  { get { return this[4];   } set { this[4] = value; } }
        public string Source       { get { return this[5];   } set { this[5] = value; } }
    }

    internal class Property_ : QRecord
    {
        public string Property { get { return this[0]; } set { this[0] = value; } }
        public string Value    { get { return this[1]; } set { this[1] = value; } }
    }

    internal class Registry_ : QRecord
    {
        public string Registry   { get { return this[0]; } set { this[0] = value; } }
        public string Key        { get { return this[2]; } set { this[2] = value; } }
        public string Name       { get { return this[3]; } set { this[3] = value; } }
        public string Value      { get { return this[4]; } set { this[4] = value; } }
        public string Component_ { get { return this[5]; } set { this[5] = value; } }
        public RegistryRoot Root
        { get { return (RegistryRoot) this.I(1); } set { this[0] = ((int) value).ToString(); } }
    }

    internal class RemoveFile_ : QRecord
    {
        public string FileKey     { get { return this[0]; } set { this[0] = value; } }
        public string Component_  { get { return this[2]; } set { this[2] = value; } }
        public string FileName    { get { return this[3]; } set { this[3] = value; } }
        public string DirProperty { get { return this[4]; } set { this[4] = value; } }
        public RemoveFileModes InstallMode
        { get { return (RemoveFileModes) this.I(5); } set { this[5] = ((int) value).ToString(); } }
    }

    #endregion // Generated code
    #pragma warning restore 1591
    #endif // !CODE_ANALYSIS
}
