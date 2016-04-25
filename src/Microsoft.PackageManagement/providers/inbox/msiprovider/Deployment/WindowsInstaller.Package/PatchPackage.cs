//---------------------------------------------------------------------
// <copyright file="PatchPackage.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller.Package
{
    using System;
    using System.Collections;
    using System.IO;

    /// <summary>
    /// Provides access to convenient properties and operations on a patch package (.MSP).
    /// </summary>
    internal class PatchPackage : Database
    {
        /// <summary>
        /// Creates a new patch package object; opening the patch database in read-only mode.
        /// </summary>
        /// <param name="packagePath">Path to the patch package (.MSP)</param>
        /// <remarks>The PatchPackage object only opens the patch database in read-only mode, because
        /// transforms (sub-storages) cannot be read if the database is open in read-write mode.</remarks>
        public PatchPackage(string packagePath)
            : base(packagePath, (DatabaseOpenMode) ((int) DatabaseOpenMode.ReadOnly | 32))
            // TODO: figure out what to do about DatabaseOpenMode.Patch
        {
        }

        /// <summary>
        /// Handle this event to receive status messages when operations are performed on the patch package.
        /// </summary>
        /// <example>
        /// <c>patchPackage.Message += new InstallPackageMessageHandler(Console.WriteLine);</c>
        /// </example>
        public event InstallPackageMessageHandler Message;

        /// <summary>
        /// Sends a message to the <see cref="Message"/> event-handler.
        /// </summary>
        /// <param name="format">Message string, containing 0 or more format items</param>
        /// <param name="args">Items to be formatted</param>
        protected void LogMessage(string format, params object[] args)
        {
            if(this.Message != null)
            {
                this.Message(format, args);
            }
        }

        /// <summary>
        /// Gets the patch code (GUID) of the patch package.
        /// </summary>
        /// <remarks>
        /// The patch code is stored in the RevisionNumber field of the patch summary information.
        /// </remarks>
        public string PatchCode
        {
            get
            {
                string guids = this.SummaryInfo.RevisionNumber;
                return guids.Substring(0, guids.IndexOf('}') + 1);
            }
        }

        /// <summary>
        /// Gets the list of patch codes that are replaced by this patch package.
        /// </summary>
        /// <returns>Array of replaced patch codes (GUIDs)</returns>
        /// <remarks>
        /// The list of replaced patch codes is stored in the RevisionNumber field of the patch summary information.
        /// </remarks>
        public string[] GetReplacedPatchCodes()
        {
            ArrayList patchCodeList = new ArrayList();
            string guids = this.SummaryInfo.RevisionNumber;
            int thisGuid = guids.IndexOf('}') + 1;
            int nextGuid = guids.IndexOf('}', thisGuid) + 1;
            while(nextGuid > 0)
            {
                patchCodeList.Add(guids.Substring(thisGuid, (nextGuid - thisGuid)));
                thisGuid = nextGuid;
                nextGuid = guids.IndexOf('}', thisGuid) + 1;
            }
            return (string[]) patchCodeList.ToArray(typeof(string));
        }

        /// <summary>
        /// Gets the list of product codes of products targeted by this patch package.
        /// </summary>
        /// <returns>Array of product codes (GUIDs)</returns>
        /// <remarks>
        /// The list of target product codes is stored in the Template field of the patch summary information.
        /// </remarks>
        public string[] GetTargetProductCodes()
        {
            string productList = this.SummaryInfo.Template;
            return productList.Split(';');
        }

        /// <summary>
        /// Gets the names of the transforms included in the patch package.
        /// </summary>
        /// <returns>Array of transform names</returns>
        /// <remarks>
        /// The returned list does not include the &quot;patch special transforms&quot; that are prefixed with &quot;#&quot;
        /// <p>The list of transform names is stored in the LastSavedBy field of the patch summary information.</p>
        /// </remarks>
        public string[] GetTransforms()
        {
            return this.GetTransforms(false);
        }
        /// <summary>
        /// Gets the names of the transforms included in the patch package.
        /// </summary>
        /// <param name="includeSpecialTransforms">Specifies whether to include the
        /// &quot;patch special transforms&quot; that are prefixed with &quot;#&quot;</param>
        /// <returns>Array of transform names</returns>
        /// <remarks>
        /// The list of transform names is stored in the LastSavedBy field of the patch summary information.
        /// </remarks>
        public string[] GetTransforms(bool includeSpecialTransforms)
        {
            ArrayList transformArray = new ArrayList();
            string transformList = this.SummaryInfo.LastSavedBy;
            foreach(string transform in transformList.Split(';', ':'))
            {
                if(transform.Length != 0 && (includeSpecialTransforms || !transform.StartsWith("#", StringComparison.Ordinal)))
                {
                    transformArray.Add(transform);
                }
            }
            return (string[]) transformArray.ToArray(typeof(string));
        }

        /// <summary>
        /// Gets information about the transforms included in the patch package.
        /// </summary>
        /// <returns>Array containing information about each transform</returns>
        /// <remarks>
        /// The returned info does not include the &quot;patch special transforms&quot; that are prefixed with &quot;#&quot;
        /// </remarks>
        public TransformInfo[] GetTransformsInfo()
        {
            return this.GetTransformsInfo(false);
        }

        /// <summary>
        /// Gets information about the transforms included in the patch package.
        /// </summary>
        /// <param name="includeSpecialTransforms">Specifies whether to include the
        /// &quot;patch special transforms&quot; that are prefixed with &quot;#&quot;</param>
        /// <returns>Array containing information about each transform</returns>
        public TransformInfo[] GetTransformsInfo(bool includeSpecialTransforms)
        {
            string[] transforms = this.GetTransforms(includeSpecialTransforms);
            ArrayList transformInfoArray = new ArrayList(transforms.Length);
            foreach(string transform in transforms)
            {
                transformInfoArray.Add(this.GetTransformInfo(transform));
            }
            return (TransformInfo[]) transformInfoArray.ToArray(typeof(TransformInfo));
        }

        /// <summary>
        /// Gets information about a transforms included in the patch package.
        /// </summary>
        /// <param name="transform">Name of the transform to extract; this may optionally be a
        /// special transform prefixed by &quot;#&quot;</param>
        /// <returns>Information about the transform</returns>
        public TransformInfo GetTransformInfo(string transform)
        {
            string tempTransformFile = null;
            try
            {
                tempTransformFile = Path.GetTempFileName();
                this.ExtractTransform(transform, tempTransformFile);
                using(SummaryInfo transformSummInfo = new SummaryInfo(tempTransformFile, false))
                {
                    return new TransformInfo(transform, transformSummInfo);
                }
            }
            finally
            {
                if(tempTransformFile != null && File.Exists(tempTransformFile))
                {
                    File.Delete(tempTransformFile);
                }
            }
        }

        /// <summary>
        /// Analyzes the transforms included in the patch package to find the ones that
        /// are applicable to an install package.
        /// </summary>
        /// <param name="installPackage">The install package to validate the transforms against</param>
        /// <returns>Array of valid transform names</returns>
        /// <remarks>
        /// The returned list does not include the &quot;patch special transforms&quot; that
        /// are prefixed with &quot;#&quot; If a transform is valid, then its corresponding
        /// special transform is assumed to be valid as well.
        /// </remarks>
        public string[] GetValidTransforms(Database installPackage)
        {
            ArrayList transformArray = new ArrayList();
            string transformList = this.SummaryInfo.LastSavedBy;
            foreach(string transform in transformList.Split(';', ':'))
            {
                if(transform.Length != 0 && !transform.StartsWith("#", StringComparison.Ordinal))
                {
                    this.LogMessage("Checking validity of transform {0}", transform);
                    string tempTransformFile = null;
                    try
                    {
                        tempTransformFile = Path.GetTempFileName();
                        this.ExtractTransform(transform, tempTransformFile);
                        if(installPackage.IsTransformValid(tempTransformFile))
                        {
                            this.LogMessage("Found valid transform: {0}", transform);
                            transformArray.Add(transform);
                        }
                    }
                    finally
                    {
                        if(tempTransformFile != null && File.Exists(tempTransformFile))
                        {
                            try { File.Delete(tempTransformFile); }
                            catch(IOException) { }
                        }
                    }
                }
            }
            return (string[]) transformArray.ToArray(typeof(string));
        }

        /// <summary>
        /// Extracts a transform (.MST) from a patch package.
        /// </summary>
        /// <param name="transform">Name of the transform to extract; this may optionally be a
        /// special transform prefixed by &quot;#&quot;</param>
        /// <param name="extractFile">Location where the transform will be extracted</param>
        public void ExtractTransform(string transform, string extractFile)
        {
            using(View stgView = this.OpenView("SELECT `Name`, `Data` FROM `_Storages` WHERE `Name` = '{0}'", transform))
            {
                stgView.Execute();
                Record stgRec = stgView.Fetch();
                if(stgRec == null)
                {
                    this.LogMessage("Transform not found: {0}", transform);
                    throw new InstallerException("Transform not found: " + transform);
                }
                using(stgRec)
                {
                    stgRec.GetStream("Data", extractFile);
                }
            }
        }
    }
}
