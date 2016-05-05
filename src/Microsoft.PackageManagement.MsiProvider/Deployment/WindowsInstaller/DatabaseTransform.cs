//---------------------------------------------------------------------
// <copyright file="DatabaseTransform.cs" company="Microsoft Corporation">
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
    using System.Globalization;

    internal partial class Database
    {
        /// <summary>
        /// Creates a transform that, when applied to the object database, results in the reference database.
        /// </summary>
        /// <param name="referenceDatabase">Database that does not include the changes</param>
        /// <param name="transformFile">Name of the generated transform file, or null to only
        /// check whether or not the two database are identical</param>
        /// <returns>true if a transform is generated, or false if a transform is not generated
        /// because there are no differences between the two databases.</returns>
        /// <exception cref="InstallerException">the transform could not be generated</exception>
        /// <exception cref="InvalidHandleException">a Database handle is invalid</exception>
        /// <remarks><p>
        /// A transform can add non-primary key columns to the end of a table. A transform cannot
        /// be created that adds primary key columns to a table. A transform cannot be created that
        /// changes the order, names, or definitions of columns.
        /// </p><p>
        /// If the transform is to be applied during an installation you must use the
        /// <see cref="Database.CreateTransformSummaryInfo"/> method to populate the
        /// summary information stream.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabasegeneratetransform.asp">MsiDatabaseGenerateTransform</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool GenerateTransform(Database referenceDatabase, string transformFile)
        {
            if (referenceDatabase == null)
            {
                throw new ArgumentNullException("referenceDatabase");
            }

            if (string.IsNullOrWhiteSpace(transformFile))
            {
                throw new ArgumentNullException("transformFile");
            }

            uint ret = NativeMethods.MsiDatabaseGenerateTransform((int) this.Handle, (int) referenceDatabase.Handle, transformFile, 0, 0);
            if (ret == (uint) NativeMethods.Error.NO_DATA)
            {
                return false;
            }
            else if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
            return true;
        }

        /// <summary>
        /// Creates and populates the summary information stream of an existing transform file, and
        /// fills in the properties with the base and reference ProductCode and ProductVersion.
        /// </summary>
        /// <param name="referenceDatabase">Database that does not include the changes</param>
        /// <param name="transformFile">Name of the generated transform file</param>
        /// <param name="errors">Error conditions that should be suppressed
        /// when the transform is applied</param>
        /// <param name="validations">Defines which properties should be validated
        /// to verify that this transform can be applied to a database.</param>
        /// <exception cref="InstallerException">the transform summary info could not be
        /// generated</exception>
        /// <exception cref="InvalidHandleException">a Database handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msicreatetransformsummaryinfo.asp">MsiCreateTransformSummaryInfo</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void CreateTransformSummaryInfo(
            Database referenceDatabase,
            string transformFile,
            TransformErrors errors,
            TransformValidations validations)
        {
            if (referenceDatabase == null)
            {
                throw new ArgumentNullException("referenceDatabase");
            }

            if (string.IsNullOrWhiteSpace(transformFile))
            {
                throw new ArgumentNullException("transformFile");
            }

            uint ret = NativeMethods.MsiCreateTransformSummaryInfo(
                (int) this.Handle,
                (int) referenceDatabase.Handle,
                transformFile,
                (int) errors,
                (int) validations);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Apply a transform to the database, recording the changes in the "_TransformView" table.
        /// </summary>
        /// <param name="transformFile">Path to the transform file</param>
        /// <exception cref="InstallerException">the transform could not be applied</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseapplytransform.asp">MsiDatabaseApplyTransform</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void ViewTransform(string transformFile)
        {
            TransformErrors transformErrors =
                TransformErrors.AddExistingRow |
                TransformErrors.DelMissingRow |
                TransformErrors.AddExistingTable |
                TransformErrors.DelMissingTable |
                TransformErrors.UpdateMissingRow |
                TransformErrors.ChangeCodePage |
                TransformErrors.ViewTransform;
            this.ApplyTransform(transformFile, transformErrors);
        }

        /// <summary>
        /// Apply a transform to the database, suppressing any error conditions
        /// specified by the transform's summary information.
        /// </summary>
        /// <param name="transformFile">Path to the transform file</param>
        /// <exception cref="InstallerException">the transform could not be applied</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseapplytransform.asp">MsiDatabaseApplyTransform</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void ApplyTransform(string transformFile)
        {
            if (string.IsNullOrWhiteSpace(transformFile))
            {
                throw new ArgumentNullException("transformFile");
            }

            TransformErrors errorConditionsToSuppress;
            using (SummaryInfo transformSummInfo = new SummaryInfo(transformFile, false))
            {
                int errorConditions = transformSummInfo.CharacterCount & 0xFFFF;
                errorConditionsToSuppress = (TransformErrors) errorConditions;
            }
            this.ApplyTransform(transformFile, errorConditionsToSuppress);
        }

        /// <summary>
        /// Apply a transform to the database, specifying error conditions to suppress.
        /// </summary>
        /// <param name="transformFile">Path to the transform file</param>
        /// <param name="errorConditionsToSuppress">Error conditions that are to be suppressed</param>
        /// <exception cref="InstallerException">the transform could not be applied</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msidatabaseapplytransform.asp">MsiDatabaseApplyTransform</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public void ApplyTransform(string transformFile, TransformErrors errorConditionsToSuppress)
        {
            if (string.IsNullOrWhiteSpace(transformFile))
            {
                throw new ArgumentNullException("transformFile");
            }

            uint ret = NativeMethods.MsiDatabaseApplyTransform((int) this.Handle, transformFile, (int) errorConditionsToSuppress);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
        }

        /// <summary>
        /// Checks whether a transform is valid for this Database, according to its validation data and flags.
        /// </summary>
        /// <param name="transformFile">Path to the transform file</param>
        /// <returns>true if the transform can be validly applied to this Database; false otherwise</returns>
        /// <exception cref="InstallerException">the transform could not be applied</exception>
        /// <exception cref="InvalidHandleException">the Database handle is invalid</exception>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsTransformValid(string transformFile)
        {
            if (string.IsNullOrWhiteSpace(transformFile))
            {
                throw new ArgumentNullException("transformFile");
            }

            using (SummaryInfo transformSummInfo = new SummaryInfo(transformFile, false))
            {
                return this.IsTransformValid(transformSummInfo);
            }
        }

        /// <summary>
        /// Checks whether a transform is valid for this Database, according to its SummaryInfo data.
        /// </summary>
        /// <param name="transformSummaryInfo">SummaryInfo data of a transform file</param>
        /// <returns>true if the transform can be validly applied to this Database; false otherwise</returns>
        /// <exception cref="InstallerException">error processing summary info</exception>
        /// <exception cref="InvalidHandleException">the Database or SummaryInfo handle is invalid</exception>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public bool IsTransformValid(SummaryInfo transformSummaryInfo)
        {
            if (transformSummaryInfo == null)
            {
                throw new ArgumentNullException("transformSummaryInfo");
            }

            string[] rev = transformSummaryInfo.RevisionNumber.Split(new char[] { ';' }, 3);
            string targetProductCode = rev[0].Substring(0, 38);
            string targetProductVersion = rev[0].Substring(38);
            string upgradeCode = rev[2];

            string[] templ = transformSummaryInfo.Template.Split(new char[] { ';' }, 2);
            int targetProductLanguage = 0;
            if (templ.Length >= 2 && templ[1].Length > 0)
            {
                targetProductLanguage = Int32.Parse(templ[1], CultureInfo.InvariantCulture.NumberFormat);
            }

            int flags = transformSummaryInfo.CharacterCount;
            int validateFlags = flags >> 16;

            string thisProductCode = this.ExecutePropertyQuery("ProductCode");
            string thisProductVersion = this.ExecutePropertyQuery("ProductVersion");
            string thisUpgradeCode = this.ExecutePropertyQuery("UpgradeCode");
            string thisProductLang = this.ExecutePropertyQuery("ProductLanguage");
            int thisProductLanguage = 0;
            if (!string.IsNullOrWhiteSpace(thisProductLang))
            {
                thisProductLanguage = Int32.Parse(thisProductLang, CultureInfo.InvariantCulture.NumberFormat);
            }

            if ((validateFlags & (int) TransformValidations.Product) != 0 &&
                thisProductCode != targetProductCode)
            {
                return false;
            }

            if ((validateFlags & (int) TransformValidations.UpgradeCode) != 0 &&
                thisUpgradeCode != upgradeCode)
            {
                return false;
            }

            if ((validateFlags & (int) TransformValidations.Language) != 0 &&
                targetProductLanguage != 0 && thisProductLanguage != targetProductLanguage)
            {
                return false;
            }

            Version thisProductVer = new Version(thisProductVersion);
            Version targetProductVer = new Version(targetProductVersion);
            if ((validateFlags & (int) TransformValidations.UpdateVersion) != 0)
            {
                if (thisProductVer.Major != targetProductVer.Major) return false;
                if (thisProductVer.Minor != targetProductVer.Minor) return false;
                if (thisProductVer.Build != targetProductVer.Build) return false;
            }
            else if ((validateFlags & (int) TransformValidations.MinorVersion) != 0)
            {
                if (thisProductVer.Major != targetProductVer.Major) return false;
                if (thisProductVer.Minor != targetProductVer.Minor) return false;
            }
            else if ((validateFlags & (int) TransformValidations.MajorVersion) != 0)
            {
                if (thisProductVer.Major != targetProductVer.Major) return false;
            }

            return true;
        }
    }
}
