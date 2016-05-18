//---------------------------------------------------------------------
// <copyright file="TransformInfo.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller.Package
{
    using System;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// Contains properties of a transform package (.MST).
    /// </summary>
    internal class TransformInfo
    {
        /// <summary>
        /// Reads transform information from a transform package.
        /// </summary>
        /// <param name="mstFile">Path to a transform package (.MST file).</param>
        public TransformInfo(string mstFile)
        {
            this.name = Path.GetFileName(mstFile);
            using (SummaryInfo transformSummInfo = new SummaryInfo(mstFile, false))
            {
                this.DecodeSummaryInfo(transformSummInfo);
            }
        }

        /// <summary>
        /// Reads transform information from the summary information of a transform package.
        /// </summary>
        /// <param name="name">Filename of the transform (optional).</param>
        /// <param name="transformSummaryInfo">Handle to the summary information of a transform package (.MST file).</param>
        public TransformInfo(string name, SummaryInfo transformSummaryInfo)
        {
            this.name = name;
            this.DecodeSummaryInfo(transformSummaryInfo);
        }

        private void DecodeSummaryInfo(SummaryInfo transformSummaryInfo)
        {
            try
            {
                string[] rev = transformSummaryInfo.RevisionNumber.Split(new char[] { ';' }, 3);
                this.targetProductCode = rev[0].Substring(0, 38);
                this.targetProductVersion = rev[0].Substring(38);
                this.upgradeProductCode = rev[1].Substring(0, 38);
                this.upgradeProductVersion = rev[1].Substring(38);
                this.upgradeCode = rev[2];

                string[] templ = transformSummaryInfo.Template.Split(new Char[] { ';' }, 2);
                this.targetPlatform = templ[0];
                this.targetLanguage = 0;
                if (templ.Length >= 2 && templ[1].Length > 0)
                {
                    this.targetLanguage = Int32.Parse(templ[1], CultureInfo.InvariantCulture.NumberFormat);
                }

                this.validateFlags = (TransformValidations) transformSummaryInfo.CharacterCount;
            }
            catch (Exception ex)
            {
                throw new InstallerException("Invalid transform summary info", ex);
            }
        }

        /// <summary>
        /// Gets the filename of the transform.
        /// </summary>
        public string Name
        {
            get { return this.name; }
        }
        private string name;

        /// <summary>
        /// Gets the target product code of the transform.
        /// </summary>
        public string TargetProductCode
        {
            get { return this.targetProductCode; }
        }
        private string targetProductCode;

        /// <summary>
        /// Gets the target product version of the transform.
        /// </summary>
        public string TargetProductVersion
        {
            get { return this.targetProductVersion; }
        }
        private string targetProductVersion;

        /// <summary>
        /// Gets the upgrade product code of the transform.
        /// </summary>
        public string UpgradeProductCode
        {
            get { return this.upgradeProductCode; }
        }
        private string upgradeProductCode;

        /// <summary>
        /// Gets the upgrade product version of the transform.
        /// </summary>
        public string UpgradeProductVersion
        {
            get { return this.upgradeProductVersion; }
        }
        private string upgradeProductVersion;

        /// <summary>
        /// Gets the upgrade code of the transform.
        /// </summary>
        public string UpgradeCode
        {
            get { return this.upgradeCode; }
        }
        private string upgradeCode;

        /// <summary>
        /// Gets the target platform of the transform.
        /// </summary>
        public string TargetPlatform
        {
            get { return this.targetPlatform; }
        }
        private string targetPlatform;

        /// <summary>
        /// Gets the target language of the transform, or 0 if the transform is language-neutral.
        /// </summary>
        public int TargetLanguage
        {
            get { return this.targetLanguage; }
        }
        private int targetLanguage;

        /// <summary>
        /// Gets the validation flags specified when the transform was generated.
        /// </summary>
        public TransformValidations Validations
        {
            get { return this.validateFlags; }
        }
        private TransformValidations validateFlags;

        /// <summary>
        /// Returns the name of the transform.
        /// </summary>
        public override string ToString()
        {
            return (this.Name != null ? this.Name : "MST");
        }
    }
}
