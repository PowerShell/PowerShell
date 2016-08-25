//---------------------------------------------------------------------
// <copyright file="FeatureInfo.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Msi.Internal.Deployment.WindowsInstaller
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Accessor for information about features within the context of an installation session.
    /// </summary>
    internal sealed class FeatureInfoCollection : ICollection<FeatureInfo>
    {
        private Session session;

        internal FeatureInfoCollection(Session session)
        {
            this.session = session;
        }

        /// <summary>
        /// Gets information about a feature within the context of an installation session.
        /// </summary>
        /// <param name="feature">name of the feature</param>
        /// <returns>feature object</returns>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public FeatureInfo this[string feature]
        {
            get
            {
                return new FeatureInfo(this.session, feature);
            }
        }

        void ICollection<FeatureInfo>.Add(FeatureInfo item)
        {
            throw new InvalidOperationException();
        }

        void ICollection<FeatureInfo>.Clear()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Checks if the collection contains a feature.
        /// </summary>
        /// <param name="feature">name of the feature</param>
        /// <returns>true if the feature is in the collection, else false</returns>
        public bool Contains(string feature)
        {
            return this.session.Database.CountRows(
                "Feature", "`Feature` = '" + feature + "'") == 1;
        }

        bool ICollection<FeatureInfo>.Contains(FeatureInfo item)
        {
            return item != null && this.Contains(item.Name);
        }

        /// <summary>
        /// Copies the features into an array.
        /// </summary>
        /// <param name="array">array that receives the features</param>
        /// <param name="arrayIndex">offset into the array</param>
        public void CopyTo(FeatureInfo[] array, int arrayIndex)
        {
            if (array == null) {
                throw new ArgumentNullException("array");
            }
            foreach (FeatureInfo feature in this)
            {
                array[arrayIndex++] = feature;
            }
        }

        /// <summary>
        /// Gets the number of features defined for the product.
        /// </summary>
        public int Count
        {
            get
            {
                return this.session.Database.CountRows("Feature");
            }
        }

        bool ICollection<FeatureInfo>.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        bool ICollection<FeatureInfo>.Remove(FeatureInfo item)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Enumerates the features in the collection.
        /// </summary>
        /// <returns>an enumerator over all features in the collection</returns>
        public IEnumerator<FeatureInfo> GetEnumerator()
        {
            using (View featureView = this.session.Database.OpenView(
                "SELECT `Feature` FROM `Feature`"))
            {
                featureView.Execute();

                foreach (Record featureRec in featureView) using (featureRec)
                {
                    string feature = featureRec.GetString(1);
                    yield return new FeatureInfo(this.session, feature);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    /// <summary>
    /// Provides access to information about a feature within the context of an installation session.
    /// </summary>
    internal class FeatureInfo
    {
        private Session session;
        private string name;

        internal FeatureInfo(Session session, string name)
        {
            this.session = session;
            this.name = name;
        }

        /// <summary>
        /// Gets the name of the feature (primary key in the Feature table).
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }
        }

        /// <summary>
        /// Gets the current install state of the feature.
        /// </summary>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="ArgumentException">an unknown feature was requested</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetfeaturestate.asp">MsiGetFeatureState</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public InstallState CurrentState
        {
            get
            {
                int installState, actionState;
                uint ret = RemotableNativeMethods.MsiGetFeatureState((int) this.session.Handle, this.name, out installState, out actionState);
                if (ret != 0)
                {
                    if (ret == (uint) NativeMethods.Error.UNKNOWN_FEATURE)
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret, this.name);
                    }
                    else
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret);
                    }
                }

                if (installState == (int) InstallState.Advertised)
                {
                    return InstallState.Advertised;
                }
                return (InstallState) installState;
            }
        }

        /// <summary>
        /// Gets or sets the action state of the feature.
        /// </summary>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="ArgumentException">an unknown feature was requested</exception>
        /// <remarks><p>
        /// When changing the feature action, the action state of all the Components linked to the changed
        /// Feature records are also updated appropriately, based on the new feature Select state.
        /// All Features can be configured at once by specifying the keyword ALL instead of a specific feature name.
        /// </p><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetfeaturestate.asp">MsiGetFeatureState</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisetfeaturestate.asp">MsiSetFeatureState</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public InstallState RequestState
        {
            get
            {
                int installState, actionState;
                uint ret = RemotableNativeMethods.MsiGetFeatureState((int) this.session.Handle, this.name, out installState, out actionState);
                if (ret != 0)
                {
                    if (ret == (uint) NativeMethods.Error.UNKNOWN_FEATURE)
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret, this.name);
                    }
                    else
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret);
                    }
                }
                return (InstallState) actionState;
            }

            set
            {
                uint ret = RemotableNativeMethods.MsiSetFeatureState((int) this.session.Handle, this.name, (int) value);
                if (ret != 0)
                {
                    if (ret == (uint) NativeMethods.Error.UNKNOWN_FEATURE)
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret, this.name);
                    }
                    else
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a list of valid installation states for the feature.
        /// </summary>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="ArgumentException">an unknown feature was requested</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetfeaturevalidstates.asp">MsiGetFeatureValidStates</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ICollection<InstallState> ValidStates
        {
            get
            {
                List<InstallState> states = new List<InstallState>();
                uint installState;
                uint ret = RemotableNativeMethods.MsiGetFeatureValidStates((int) this.session.Handle, this.name, out installState);
                if (ret != 0)
                {
                    if (ret == (uint) NativeMethods.Error.UNKNOWN_FEATURE)
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret, this.name);
                    }
                    else
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret);
                    }
                }

                for (int i = 1; i <= (int) InstallState.Default; i++)
                {
                    if (((int) installState & (1 << i)) != 0)
                    {
                        states.Add((InstallState) i);
                    }
                }
                return states.AsReadOnly();
            }
        }

        /// <summary>
        /// Gets or sets the attributes of the feature.
        /// </summary>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="ArgumentException">an unknown feature was requested</exception>
        /// <remarks><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetfeatureinfo.asp">MsiGetFeatureInfo</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisetfeatureattributes.asp">MsiSetFeatureAttributes</a>
        /// </p><p>
        /// Since the lpAttributes parameter of
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetfeatureinfo.asp">MsiGetFeatureInfo</a>
        /// does not contain an equivalent flag for <see cref="FeatureAttributes.UIDisallowAbsent"/>, this flag will
        /// not be retrieved.
        /// </p><p>
        /// Since the dwAttributes parameter of
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisetfeatureattributes.asp">MsiSetFeatureAttributes</a>
        /// does not contain an equivalent flag for <see cref="FeatureAttributes.UIDisallowAbsent"/>, the presence
        /// of this flag will be ignored.
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public FeatureAttributes Attributes
        {
            get
            {
                FeatureAttributes attributes;
                uint titleBufSize = 0;
                uint descBufSize = 0;
                uint attr;
                uint ret = NativeMethods.MsiGetFeatureInfo(
                    (int) this.session.Handle,
                    this.name,
                    out attr,
                    null,
                    ref titleBufSize,
                    null,
                    ref descBufSize);

                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }

                // Values for attributes that MsiGetFeatureInfo returns are
                // double the values in the Attributes column of the Feature Table.
                attributes = (FeatureAttributes) (attr >> 1);

                // MsiGetFeatureInfo MSDN documentation indicates
                // NOUNSUPPORTEDADVERTISE is 32.  Conversion above changes this to 16
                // which is UIDisallowAbsent.  MsiGetFeatureInfo isn't documented to
                // return an attribute for 'UIDisallowAbsent', so if UIDisallowAbsent
                // is set, change it to NoUnsupportedAdvertise which then maps correctly
                // to NOUNSUPPORTEDADVERTISE.
                if ((attributes & FeatureAttributes.UIDisallowAbsent) == FeatureAttributes.UIDisallowAbsent)
                {
                    attributes &= ~FeatureAttributes.UIDisallowAbsent;
                    attributes |= FeatureAttributes.NoUnsupportedAdvertise;
                }

                return attributes;
            }

            set
            {
                // MsiSetFeatureAttributes doesn't indicate UIDisallowAbsent is valid
                // so remove it.
                FeatureAttributes attributes = value;
                attributes &= ~FeatureAttributes.UIDisallowAbsent;

                // Values for attributes that MsiSetFeatureAttributes uses are
                // double the values in the Attributes column of the Feature Table.
                uint attr = ((uint) attributes) << 1;

                // MsiSetFeatureAttributes MSDN documentation indicates
                // NOUNSUPPORTEDADVERTISE is 32.  Conversion above changes this to 64
                // which is undefined.  Change this back to 32.
                uint noUnsupportedAdvertiseDbl = ((uint)FeatureAttributes.NoUnsupportedAdvertise) << 1;
                if ((attr & noUnsupportedAdvertiseDbl) == noUnsupportedAdvertiseDbl)
                {
                    attr &= ~noUnsupportedAdvertiseDbl;
                    attr |= (uint) FeatureAttributes.NoUnsupportedAdvertise;
                }

                uint ret = RemotableNativeMethods.MsiSetFeatureAttributes((int) this.session.Handle, this.name, attr);

                if (ret != (uint)NativeMethods.Error.SUCCESS)
                {
                    if (ret == (uint)NativeMethods.Error.UNKNOWN_FEATURE)
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret, this.name);
                    }
                    else
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the title of the feature.
        /// </summary>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="ArgumentException">an unknown feature was requested</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetfeatureinfo.asp">MsiGetFeatureInfo</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Title
        {
            get
            {
                StringBuilder titleBuf = new StringBuilder(80);
                uint titleBufSize = (uint) titleBuf.Capacity;
                uint descBufSize = 0;
                uint attr;
                uint ret = NativeMethods.MsiGetFeatureInfo(
                    (int) this.session.Handle,
                    this.name,
                    out attr,
                    titleBuf,
                    ref titleBufSize,
                    null,
                    ref descBufSize);

                if (ret == (uint) NativeMethods.Error.MORE_DATA)
                {
                    titleBuf.Capacity = (int) ++titleBufSize;
                    ret = NativeMethods.MsiGetFeatureInfo(
                        (int) this.session.Handle,
                        this.name,
                        out attr,
                        titleBuf,
                        ref titleBufSize,
                        null,
                        ref descBufSize);
                }

                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }

                return titleBuf.ToString();
            }
        }

        /// <summary>
        /// Gets the description of the feature.
        /// </summary>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="ArgumentException">an unknown feature was requested</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetfeatureinfo.asp">MsiGetFeatureInfo</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public string Description
        {
            get
            {
                StringBuilder descBuf = new StringBuilder(256);
                uint titleBufSize = 0;
                uint descBufSize = (uint) descBuf.Capacity;
                uint attr;
                uint ret = NativeMethods.MsiGetFeatureInfo(
                    (int) this.session.Handle,
                    this.name,
                    out attr,
                    null,
                    ref titleBufSize,
                    descBuf,
                    ref descBufSize);

                if (ret == (uint) NativeMethods.Error.MORE_DATA)
                {
                    descBuf.Capacity = (int) ++descBufSize;
                    ret = NativeMethods.MsiGetFeatureInfo(
                        (int) this.session.Handle,
                        this.name,
                        out attr,
                        null,
                        ref titleBufSize,
                        descBuf,
                        ref descBufSize);
                }

                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }

                return descBuf.ToString();
            }
        }

        /// <summary>
        /// Calculates the disk space required by the feature and its selected children and parent features.
        /// </summary>
        /// <param name="includeParents">If true, the parent features are included in the cost.</param>
        /// <param name="includeChildren">If true, the child features are included in the cost.</param>
        /// <param name="installState">Specifies the installation state.</param>
        /// <returns>The disk space requirement in bytes.</returns>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetfeaturecost.asp">MsiGetFeatureCost</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public long GetCost(bool includeParents, bool includeChildren, InstallState installState)
        {
            const int MSICOSTTREE_CHILDREN = 1;
            const int MSICOSTTREE_PARENTS = 2;

            int cost;
            uint ret = RemotableNativeMethods.MsiGetFeatureCost(
                (int) this.session.Handle,
                this.name,
                (includeParents ? MSICOSTTREE_PARENTS : 0) | (includeChildren ? MSICOSTTREE_CHILDREN : 0),
                (int) installState,
                out cost);
            if (ret != 0)
            {
                throw InstallerException.ExceptionFromReturnCode(ret);
            }
            return cost * 512L;
        }
    }
}
