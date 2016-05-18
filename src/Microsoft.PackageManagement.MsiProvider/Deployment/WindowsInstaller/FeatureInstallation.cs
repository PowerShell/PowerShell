//---------------------------------------------------------------------
// <copyright file="FeatureInstallation.cs" company="Microsoft Corporation">
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
    using System.Text;

    /// <summary>
    /// Represents an instance of a feature of an installed product.
    /// </summary>
    internal class FeatureInstallation : InstallationPart
    {
        /// <summary>
        /// Creates a new FeatureInstallation instance for a feature of a product.
        /// </summary>
        /// <param name="featureName">feature name</param>
        /// <param name="productCode">ProductCode GUID</param>
        public FeatureInstallation(string featureName, string productCode)
            : base(featureName, productCode)
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                throw new ArgumentNullException("featureName");
            }
        }

        /// <summary>
        /// Gets the name of the feature.
        /// </summary>
        public string FeatureName
        {
            get
            {
                return this.Id;
            }
        }

        /// <summary>
        /// Gets the installed state of the feature.
        /// </summary>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msiqueryfeaturestate.asp">MsiQueryFeatureState</a>
        /// </p></remarks>
        public override InstallState State
        {
            get
            {
                int installState = NativeMethods.MsiQueryFeatureState(
                    this.ProductCode, this.FeatureName);
                return (InstallState) installState;
            }
        }

        /// <summary>
        /// Gets the parent of the feature, or null if the feature has no parent (it is a root feature).
        /// </summary>
        /// <remarks>
        /// Invocation of this property may be slightly costly for products with many features,
        /// because it involves an enumeration of all the features in the product.
        /// </remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public FeatureInstallation Parent
        {
            get
            {
                StringBuilder featureBuf = new StringBuilder(256);
                StringBuilder parentBuf = new StringBuilder(256);
                for (uint i = 0; ; i++)
                {
                    uint ret = NativeMethods.MsiEnumFeatures(this.ProductCode, i, featureBuf, parentBuf);

                    if (ret != 0)
                    {
                        break;
                    }

                    if (featureBuf.ToString() == this.FeatureName)
                    {
                        if (parentBuf.Length > 0)
                        {
                            return new FeatureInstallation(parentBuf.ToString(), this.ProductCode);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the usage metrics for the feature.
        /// </summary>
        /// <remarks><p>
        /// If no usage metrics are recorded, the <see cref="UsageData.UseCount" /> value is 0.
        /// </p><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetfeatureusage.asp">MsiGetFeatureUsage</a>
        /// </p></remarks>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public FeatureInstallation.UsageData Usage
        {
            get
            {
                uint useCount;
                ushort useDate;
                uint ret = NativeMethods.MsiGetFeatureUsage(
                    this.ProductCode, this.FeatureName, out useCount, out useDate);
                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }

                DateTime lastUsedDate;
                if (useCount == 0)
                {
                    lastUsedDate = DateTime.MinValue;
                }
                else
                {
                    lastUsedDate = new DateTime(
                        1980 + (useDate >> 9),
                        (useDate & 0x01FF) >> 5,
                        (useDate & 0x001F));
                }

                return new UsageData((int) useCount, lastUsedDate);
            }
        }

        /// <summary>
        /// Holds data about the usage of a feature.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible")]
        [SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
        public struct UsageData
        {
            private int useCount;
            private DateTime lastUsedDate;

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            internal UsageData(int useCount, DateTime lastUsedDate)
            {
                this.useCount = useCount;
                this.lastUsedDate = lastUsedDate;
            }

            /// <summary>
            /// Gets count of the number of times the feature has been used.
            /// </summary>
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public int UseCount
            {
                get
                {
                    return this.useCount;
                }
            }

            /// <summary>
            /// Gets the date the feature was last used.
            /// </summary>
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public DateTime LastUsedDate
            {
                get
                {
                    return this.lastUsedDate;
                }
            }
        }
    }
}
