//---------------------------------------------------------------------
// <copyright file="NativeMethods.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using System.Security.Permissions;
    using System.Text;
    using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
    using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

    [Guid("0000000b-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IStorage
{
    [return: MarshalAs(UnmanagedType.Interface)]
    IStream CreateStream([MarshalAs(UnmanagedType.LPWStr)] string wcsName, uint grfMode, uint reserved1, uint reserved2);
    [return: MarshalAs(UnmanagedType.Interface)]
    IStream OpenStream([MarshalAs(UnmanagedType.LPWStr)] string wcsName, IntPtr reserved1, uint grfMode, uint reserved2);
    [return: MarshalAs(UnmanagedType.Interface)]
    IStorage CreateStorage([MarshalAs(UnmanagedType.LPWStr)] string wcsName, uint grfMode, uint reserved1, uint reserved2);
    [return: MarshalAs(UnmanagedType.Interface)]
    IStorage OpenStorage([MarshalAs(UnmanagedType.LPWStr)] string wcsName, IntPtr stgPriority, uint grfMode, IntPtr snbExclude, uint reserved);
    void CopyTo(uint ciidExclude, IntPtr rgiidExclude, IntPtr snbExclude, [MarshalAs(UnmanagedType.Interface)] IStorage stgDest);
    void MoveElementTo([MarshalAs(UnmanagedType.LPWStr)] string wcsName, [MarshalAs(UnmanagedType.Interface)] IStorage stgDest, [MarshalAs(UnmanagedType.LPWStr)] string wcsNewName, uint grfFlags);
    void Commit(uint grfCommitFlags);
    void Revert();
    IntPtr EnumElements(uint reserved1, IntPtr reserved2, uint reserved3);
    void DestroyElement([MarshalAs(UnmanagedType.LPWStr)] string wcsName);
    void RenameElement([MarshalAs(UnmanagedType.LPWStr)] string wcsOldName, [MarshalAs(UnmanagedType.LPWStr)] string wcsNewName);
    void SetElementTimes([MarshalAs(UnmanagedType.LPWStr)] string wcsName, ref FILETIME ctime, ref FILETIME atime, ref FILETIME mtime);
    void SetClass(ref Guid clsid);
    void SetStateBits(uint grfStateBits, uint grfMask);
    void Stat(ref STATSTG statstg, uint grfStatFlag);
}

internal static class NativeMethods
{
    internal enum Error : uint
    {
        SUCCESS = 0,
        FILE_NOT_FOUND = 2,
        PATH_NOT_FOUND = 3,
        ACCESS_DENIED = 5,
        INVALID_HANDLE = 6,
        INVALID_DATA = 13,
        INVALID_PARAMETER = 87,
        OPEN_FAILED = 110,
        DISK_FULL = 112,
        CALL_NOT_IMPLEMENTED = 120,
        BAD_PATHNAME = 161,
        NO_DATA = 232,
        MORE_DATA = 234,
        NO_MORE_ITEMS = 259,
        DIRECTORY = 267,
        INSTALL_USEREXIT = 1602,
        INSTALL_FAILURE = 1603,
        FILE_INVALID = 1006,
        UNKNOWN_PRODUCT = 1605,
        UNKNOWN_FEATURE = 1606,
        UNKNOWN_COMPONENT = 1607,
        UNKNOWN_PROPERTY = 1608,
        INVALID_HANDLE_STATE = 1609,
        INSTALL_SOURCE_ABSENT = 1612,
        BAD_QUERY_SYNTAX = 1615,
        INSTALL_PACKAGE_INVALID = 1620,
        FUNCTION_FAILED = 1627,
        INVALID_TABLE = 1628,
        DATATYPE_MISMATCH = 1629,
        CREATE_FAILED = 1631,
        SUCCESS_REBOOT_INITIATED = 1641,
        SUCCESS_REBOOT_REQUIRED = 3010,
    }

    internal enum SourceType : int
    {
        Unknown = 0,
        Network = 1,
        Url = 2,
        Media = 3,
    }

    [Flags]
    internal enum STGM : uint
    {
        DIRECT           = 0x00000000,
        TRANSACTED       = 0x00010000,
        SIMPLE           = 0x08000000,

        READ             = 0x00000000,
        WRITE            = 0x00000001,
        READWRITE        = 0x00000002,

        SHARE_DENY_NONE  = 0x00000040,
        SHARE_DENY_READ  = 0x00000030,
        SHARE_DENY_WRITE = 0x00000020,
        SHARE_EXCLUSIVE  = 0x00000010,

        PRIORITY         = 0x00040000,
        DELETEONRELEASE  = 0x04000000,
        NOSCRATCH        = 0x00100000,

        CREATE           = 0x00001000,
        CONVERT          = 0x00020000,
        FAILIFTHERE      = 0x00000000,

        NOSNAPSHOT       = 0x00200000,
        DIRECT_SWMR      = 0x00400000,
    }

[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int DllGetVersion(uint[] dvi);

[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSetInternalUI(uint dwUILevel, ref IntPtr phWnd);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSetInternalUI(uint dwUILevel, IntPtr phWnd);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern NativeExternalUIHandler MsiSetExternalUI([MarshalAs(UnmanagedType.FunctionPtr)] NativeExternalUIHandler puiHandler, uint dwMessageFilter, IntPtr pvContext);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEnableLog(uint dwLogMode, string szLogFile, uint dwLogAttributes);
//[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEnumProducts(uint iProductIndex, StringBuilder lpProductBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetProductInfo(string szProduct, string szProperty, StringBuilder lpValueBuf, ref uint pcchValueBuf);
//[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEnumPatches(string szProduct, uint iPatchIndex, StringBuilder lpPatchBuf, StringBuilder lpTransformsBuf, ref uint pcchTransformsBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetPatchInfo(string szPatch, string szAttribute, StringBuilder lpValueBuf, ref uint pcchValueBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEnumFeatures(string szProduct, uint iFeatureIndex, StringBuilder lpFeatureBuf, StringBuilder lpParentBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int MsiQueryFeatureState(string szProduct, string szFeature);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int MsiUseFeatureEx(string szProduct, string szFeature, uint dwInstallMode, uint dwReserved);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int MsiQueryProductState(string szProduct);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetShortcutTarget(string szShortcut, StringBuilder szProductCode, StringBuilder szFeatureId, StringBuilder szComponentCode);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiProvideComponent(string szProduct, string szFeature, string szComponent, uint dwInstallMode, StringBuilder lpPathBuf, ref uint cchPathBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiProvideQualifiedComponentEx(string szComponent, string szQualifier, uint dwInstallMode, string szProduct, uint dwUnused1, uint dwUnused2, StringBuilder lpPathBuf, ref uint cchPathBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiReinstallFeature(string szFeature, string szProduct, uint dwReinstallMode);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiReinstallProduct(string szProduct, uint dwReinstallMode);
//[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSourceListAddSource(string szProduct, string szUserName, uint dwReserved, string szSource);
//[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSourceListClearAll(string szProduct, string szUserName, uint dwReserved);
//[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSourceListForceResolution(string szProduct, string szUserName, uint dwReserved);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiCollectUserInfo(string szProduct);
//[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int  MsiGetUserInfo(string szProduct, StringBuilder lpUserNameBuf, ref uint cchUserNameBuf, StringBuilder lpOrgNameBuf, ref uint cchOrgNameBuf, StringBuilder lpSerialBuf, ref uint cchSerialBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiOpenPackageEx(string szPackagePath, uint dwOptions, out int hProduct);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiOpenProduct(string szProduct, out int hProduct);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiInstallProduct(string szPackagePath, string szCommandLine);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiConfigureProductEx(string szProduct, int iInstallLevel, int eInstallState, string szCommandLine);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiConfigureFeature(string szProduct, string szFeature, int eInstallState);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiApplyPatch(string szPatchPackage, string szInstallPackage, int eInstallType, string szCommandLine);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiOpenDatabase(string szDatabasePath, IntPtr uiOpenMode, out int hDatabase);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiOpenDatabase(string szDatabasePath, string szPersist, out int hDatabase);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int MsiGetDatabaseState(int hDatabase);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiDatabaseOpenView(int hDatabase, string szQuery, out int hView);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiDatabaseMerge(int hDatabase, int hDatabaseMerge, string szTableName);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiDatabaseCommit(int hDatabase);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiDatabaseGetPrimaryKeys(int hDatabase, string szTableName, out int hRecord);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiDatabaseIsTablePersistent(int hDatabase, string szTableName);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiDatabaseExport(int hDatabase, string szTableName, string szFolderPath, string szFileName);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiDatabaseImport(int hDatabase, string szFolderPath, string szFileName);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiDatabaseGenerateTransform(int hDatabase, int hDatabaseReference, string szTransformFile, int iReserved1, int iReserved2);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiCreateTransformSummaryInfo(int hDatabase, int hDatabaseReference, string szTransformFile, int iErrorConditions, int iValidation);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiDatabaseApplyTransform(int hDatabase, string szTransformFile, int iErrorConditions);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiViewExecute(int hView, int hRecord);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiViewFetch(int hView, out int hRecord);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiViewModify(int hView, int iModifyMode, int hRecord);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int MsiViewGetError(int hView, StringBuilder szColumnNameBuffer, ref uint cchBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiViewGetColumnInfo(int hView, uint eColumnInfo, out int hRecord);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int MsiCreateRecord(uint cParams);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiFormatRecord(int hInstall, int hRecord, StringBuilder szResultBuf, ref uint cchResultBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiRecordClearData(int hRecord);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiRecordGetFieldCount(int hRecord);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool MsiRecordIsNull(int hRecord, uint iField);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int MsiRecordGetInteger(int hRecord, uint iField);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiRecordGetString(int hRecord, uint iField, StringBuilder szValueBuf, ref uint cchValueBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiRecordSetInteger(int hRecord, uint iField, int iValue);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiRecordSetString(int hRecord, uint iField, string szValue);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiRecordDataSize(int hRecord, uint iField);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiRecordReadStream(int hRecord, uint iField, byte[] szDataBuf, ref uint cbDataBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiRecordSetStream(int hRecord, uint iField, string szFilePath);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetSummaryInformation(int hDatabase, string szDatabasePath, uint uiUpdateCount, out int hSummaryInfo);
//[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSummaryInfoGetPropertyCount(int hSummaryInfo, out uint uiPropertyCount);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSummaryInfoGetProperty(int hSummaryInfo, uint uiProperty, out uint uiDataType, out int iValue, ref long ftValue, StringBuilder szValueBuf, ref uint cchValueBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSummaryInfoSetProperty(int hSummaryInfo, uint uiProperty, uint uiDataType, int iValue, ref long ftValue, string szValue);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSummaryInfoPersist(int hSummaryInfo);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiCloseHandle(int hAny);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetFileVersion(string szFilePath, StringBuilder szVersionBuf, ref uint cchVersionBuf, StringBuilder szLangBuf, ref uint cchLangBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetFileHash(string szFilePath, uint dwOptions, uint[] hash);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int MsiGetActiveDatabase(int hInstall);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetProperty(int hInstall, string szName, StringBuilder szValueBuf, ref uint cchValueBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSetProperty(int hInstall, string szName, string szValue);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int MsiProcessMessage(int hInstall, uint eMessageType, int hRecord);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEvaluateCondition(int hInstall, string szCondition);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool MsiGetMode(int hInstall, uint iRunMode);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSetMode(int hInstall, uint iRunMode, [MarshalAs(UnmanagedType.Bool)] bool fState);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiDoAction(int hInstall, string szAction);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSequence(int hInstall, string szTable, int iSequenceMode);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetSourcePath(int hInstall, string szFolder, StringBuilder szPathBuf, ref uint cchPathBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetTargetPath(int hInstall, string szFolder, StringBuilder szPathBuf, ref uint cchPathBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSetTargetPath(int hInstall, string szFolder, string szFolderPath);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetComponentState(int hInstall, string szComponent, out int iInstalled, out int iAction);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSetComponentState(int hInstall, string szComponent, int iState);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetFeatureState(int hInstall, string szFeature, out int iInstalled, out int iAction);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSetFeatureState(int hInstall, string szFeature, int iState);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetFeatureValidStates(int hInstall, string szFeature, out uint dwInstallState);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSetInstallLevel(int hInstall, int iInstallLevel);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern ushort MsiGetLanguage(int hInstall);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEnumComponents(uint iComponentIndex, StringBuilder lpComponentBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEnumComponentsEx(string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint dwIndex, StringBuilder szInstalledProductCode, [MarshalAs(UnmanagedType.I4)] out UserContexts pdwInstalledContext, StringBuilder szSid, ref uint pcchSid);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEnumClients(string szComponent, uint iProductIndex, StringBuilder lpProductBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEnumClientsEx(string szComponent, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint iProductIndex, StringBuilder lpProductBuf, [MarshalAs(UnmanagedType.I4)] out UserContexts pdwInstalledContext, StringBuilder szSid, ref uint pcchSid);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int MsiGetComponentPath(string szProduct, string szComponent, StringBuilder lpPathBuf, ref uint pcchBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int MsiGetComponentPathEx(string szProduct, string szComponent, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, StringBuilder lpPathBuf, ref uint pcchBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEnumComponentQualifiers(string szComponent, uint iIndex, StringBuilder lpQualifierBuf, ref uint pcchQualifierBuf, StringBuilder lpApplicationDataBuf, ref uint pcchApplicationDataBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern int MsiGetLastErrorRecord();
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEnumRelatedProducts(string upgradeCode, uint dwReserved, uint iProductIndex, StringBuilder lpProductBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetProductCode(string szComponent, StringBuilder lpProductBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetFeatureUsage(string szProduct, string szFeature, out uint dwUseCount, out ushort dwDateUsed);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetFeatureCost(int hInstall, string szFeature, int iCostTree, int iState, out int iCost);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiVerifyPackage(string szPackagePath);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiIsProductElevated(string szProductCode, [MarshalAs(UnmanagedType.Bool)] out bool fElevated);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiAdvertiseProduct(string szPackagePath, IntPtr szScriptFilePath, string szTransforms, ushort lgidLanguage);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiAdvertiseProduct(string szPackagePath, string szScriptFilePath, string szTransforms, ushort lgidLanguage);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiAdvertiseProductEx(string szPackagePath, string szScriptFilePath, string szTransforms, ushort lgidLanguage, uint dwPlatform, uint dwReserved);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiAdvertiseScript(string szScriptFile, uint dwFlags, IntPtr phRegData, [MarshalAs(UnmanagedType.Bool)] bool fRemoveItems);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiProcessAdvertiseScript(string szScriptFile, string szIconFolder, IntPtr hRegData, [MarshalAs(UnmanagedType.Bool)] bool fShortcuts, [MarshalAs(UnmanagedType.Bool)] bool fRemoveItems);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetProductInfoFromScript(string szScriptFile, StringBuilder lpProductBuf39, out ushort plgidLanguage, out uint pdwVersion, StringBuilder lpNameBuf, ref uint cchNameBuf, StringBuilder lpPackageBuf, ref uint cchPackageBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiProvideAssembly(string szAssemblyName, string szAppContext, uint dwInstallMode, uint dwAssemblyInfo, StringBuilder lpPathBuf, ref uint cchPathBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiInstallMissingComponent(string szProduct, string szComponent, int eInstallState);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiInstallMissingFile(string szProduct, string szFile);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiLocateComponent(string szComponent, StringBuilder lpPathBuf, ref uint cchBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetProductProperty(int hProduct, string szProperty, StringBuilder lpValueBuf, ref uint cchValueBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetFeatureInfo(int hProduct, string szFeature, out uint lpAttributes, StringBuilder lpTitleBuf, ref uint cchTitleBuf, StringBuilder lpHelpBuf, ref uint cchHelpBuf);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiVerifyDiskSpace(int hInstall);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEnumComponentCosts(int hInstall, string szComponent, uint dwIndex, int iState, StringBuilder lpDriveBuf, ref uint cchDriveBuf, out int iCost, out int iTempCost);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSetFeatureAttributes(int hInstall, string szFeature, uint dwAttributes);

[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiRemovePatches(string szPatchList, string szProductCode, int eUninstallType, string szPropertyList);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiDetermineApplicablePatches(string szProductPackagePath, uint cPatchInfo, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1), In, Out] MsiPatchSequenceData[] pPatchInfo);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiDeterminePatchSequence(string szProductCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint cPatchInfo, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=3), In, Out] MsiPatchSequenceData[] pPatchInfo);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiApplyMultiplePatches(string szPatchPackages, string szProductCode, string szPropertiesList);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEnumPatchesEx(string szProductCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint dwFilter, uint dwIndex, StringBuilder szPatchCode, StringBuilder szTargetProductCode, [MarshalAs(UnmanagedType.I4)] out UserContexts pdwTargetProductContext, StringBuilder szTargetUserSid, ref uint pcchTargetUserSid);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetPatchInfoEx(string szPatchCode, string szProductCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, string szProperty, StringBuilder lpValue, ref uint pcchValue);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEnumProductsEx(string szProductCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint dwIndex, StringBuilder szInstalledProductCode, [MarshalAs(UnmanagedType.I4)] out UserContexts pdwInstalledContext, StringBuilder szSid, ref uint pcchSid);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetProductInfoEx(string szProductCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, string szProperty, StringBuilder lpValue, ref uint pcchValue);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiQueryFeatureStateEx(string szProductCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, string szFeature, out int pdwState);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiQueryComponentState(string szProductCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, string szComponent, out int pdwState);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiExtractPatchXMLData(string szPatchPath, uint dwReserved, StringBuilder szXMLData, ref uint pcchXMLData);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSourceListEnumSources(string szProductCodeOrPatchCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint dwOptions, uint dwIndex, StringBuilder szSource, ref uint pcchSource);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSourceListAddSourceEx(string szProductCodeOrPatchCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint dwOptions, string szSource, uint dwIndex);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSourceListClearSource(string szProductCodeOrPatchCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint dwOptions, string szSource);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSourceListClearAllEx(string szProductCodeOrPatchCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint dwOptions);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSourceListForceResolutionEx(string szProductCodeOrPatchCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint dwOptions);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSourceListGetInfo(string szProductCodeOrPatchCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint dwOptions, string szProperty, StringBuilder szValue, ref uint pcchValue);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSourceListSetInfo(string szProductCodeOrPatchCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint dwOptions, string szProperty, string szValue);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSourceListEnumMediaDisks(string szProductCodeOrPatchCode, string szUserSID, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint dwOptions, uint dwIndex, out uint pdwDiskId, StringBuilder szVolumeLabel, ref uint pcchVolumeLabel, StringBuilder szDiskPrompt, ref uint pcchDiskPrompt);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSourceListAddMediaDisk(string szProductCodeOrPatchCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint dwOptions, uint dwDiskId, string szVolumeLabel, string szDiskPrompt);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSourceListClearMediaDisk(string szProductCodeOrPatchCode, string szUserSid, [MarshalAs(UnmanagedType.I4)] UserContexts dwContext, uint dwOptions, uint dwDiskID);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiNotifySidChange(string szOldSid, string szNewSid);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiSetExternalUIRecord([MarshalAs(UnmanagedType.FunctionPtr)] NativeExternalUIRecordHandler puiHandler, uint dwMessageFilter, IntPtr pvContext, out NativeExternalUIRecordHandler ppuiPrevHandler);

[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiGetPatchFileList(string szProductCode, string szPatchList, out uint cFiles, out IntPtr phFileRecords);

[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiBeginTransaction(string szTransactionName, int dwTransactionAttributes, out int hTransaction, out IntPtr phChangeOfOwnerEvent);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiEndTransaction(int dwTransactionState);
[DllImport("msi.dll", CharSet=CharSet.Unicode)] internal static extern uint MsiJoinTransaction(int hTransaction, int dwTransactionAttributes, out IntPtr phChangeOfOwnerEvent);

[DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode, EntryPoint="LoadLibraryExW")] internal static extern IntPtr LoadLibraryEx(string fileName, IntPtr hFile, uint flags);
[DllImport("kernel32.dll", SetLastError=true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool FreeLibrary(IntPtr hModule);
[DllImport("kernel32.dll", SetLastError=true)] internal static extern IntPtr FindResourceEx(IntPtr hModule, IntPtr type, IntPtr name, ushort langId);
[DllImport("kernel32.dll", SetLastError=true)] internal static extern IntPtr LoadResource(IntPtr hModule, IntPtr lpResourceInfo);
[DllImport("kernel32.dll", SetLastError=true)] internal static extern IntPtr LockResource(IntPtr resourceData);
[DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode, EntryPoint="FormatMessageW")] internal static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, StringBuilder lpBuffer, uint nSize, IntPtr Arguments);
[DllImport("kernel32.dll", SetLastError=true)] internal static extern int WaitForSingleObject(IntPtr handle, int milliseconds);

[DllImport("ole32.dll")] internal static extern int StgOpenStorage([MarshalAs(UnmanagedType.LPWStr)] string wcsName, IntPtr stgPriority, uint grfMode, IntPtr snbExclude, uint reserved, [MarshalAs(UnmanagedType.Interface)] out IStorage stgOpen);
[DllImport("ole32.dll")] internal static extern int StgCreateDocfile([MarshalAs(UnmanagedType.LPWStr)] string wcsName, uint grfMode, uint reserved, [MarshalAs(UnmanagedType.Interface)] out IStorage stgOpen);

[DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="MessageBoxW")] internal static extern MessageResult MessageBox(IntPtr hWnd, string lpText, string lpCaption, [MarshalAs(UnmanagedType.U4)] int uType);

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    internal struct MsiPatchSequenceData
    {
        public string szPatchData;
        public int ePatchDataType;
        public int dwOrder;
        public uint dwStatus;
    }

    internal class MsiHandle : SafeHandle
    {
        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands")]
        public MsiHandle(IntPtr handle, bool ownsHandle)
            : base(handle, ownsHandle)
        {
        }

        public override bool IsInvalid
        {
            get
            {
                return this.handle == IntPtr.Zero;
            }
        }

        public static implicit operator IntPtr(MsiHandle msiHandle)
        {
            return msiHandle.handle;
        }

        [SecurityPermission(SecurityAction.Assert, UnmanagedCode = true)]
        protected override bool ReleaseHandle()
        {
            return RemotableNativeMethods.MsiCloseHandle((int) this.handle) == 0;
        }
    }
}
}
