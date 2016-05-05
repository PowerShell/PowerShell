//---------------------------------------------------------------------
// <copyright file="ComponentInfo.cs" company="Microsoft Corporation">
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
    /// Accessor for information about components within the context of an installation session.
    /// </summary>
    internal sealed class ComponentInfoCollection : ICollection<ComponentInfo>
    {
        private Session session;

        internal ComponentInfoCollection(Session session)
        {
            this.session = session;
        }

        /// <summary>
        /// Gets information about a component within the context of an installation session.
        /// </summary>
        /// <param name="component">name of the component</param>
        /// <returns>component object</returns>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public ComponentInfo this[string component]
        {
            get
            {
                return new ComponentInfo(this.session, component);
            }
        }

        void ICollection<ComponentInfo>.Add(ComponentInfo item)
        {
            throw new InvalidOperationException();
        }

        void ICollection<ComponentInfo>.Clear()
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Checks if the collection contains a component.
        /// </summary>
        /// <param name="component">name of the component</param>
        /// <returns>true if the component is in the collection, else false</returns>
        public bool Contains(string component)
        {
            return this.session.Database.CountRows(
                "Component", "`Component` = '" + component + "'") == 1;
        }

        bool ICollection<ComponentInfo>.Contains(ComponentInfo item)
        {
            return item != null && this.Contains(item.Name);
        }

        /// <summary>
        /// Copies the features into an array.
        /// </summary>
        /// <param name="array">array that receives the features</param>
        /// <param name="arrayIndex">offset into the array</param>
        public void CopyTo(ComponentInfo[] array, int arrayIndex)
        {
            if (array == null) {
                throw new ArgumentNullException("array");
            }
            foreach (ComponentInfo component in this)
            {
                array[arrayIndex++] = component;
            }
        }

        /// <summary>
        /// Gets the number of components defined for the product.
        /// </summary>
        public int Count
        {
            get
            {
                return this.session.Database.CountRows("Component");
            }
        }

        bool ICollection<ComponentInfo>.IsReadOnly
        {
            get
            {
                return true;
            }
        }

        bool ICollection<ComponentInfo>.Remove(ComponentInfo item)
        {
            throw new InvalidOperationException();
        }

        /// <summary>
        /// Enumerates the components in the collection.
        /// </summary>
        /// <returns>an enumerator over all features in the collection</returns>
        public IEnumerator<ComponentInfo> GetEnumerator()
        {
            using (View compView = this.session.Database.OpenView(
                "SELECT `Component` FROM `Component`"))
            {
                compView.Execute();

                foreach (Record compRec in compView) using (compRec)
                {
                    string comp = compRec.GetString(1);
                    yield return new ComponentInfo(this.session, comp);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    /// <summary>
    /// Provides access to information about a component within the context of an installation session.
    /// </summary>
    internal class ComponentInfo
    {
        private Session session;
        private string name;

        internal ComponentInfo(Session session, string name)
        {
            this.session = session;
            this.name = name;
        }

        /// <summary>
        /// Gets the name of the component (primary key in the Component table).
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }
        }

        /// <summary>
        /// Gets the current install state of the designated Component.
        /// </summary>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="ArgumentException">an unknown Component was requested</exception>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetcomponentstate.asp">MsiGetComponentState</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public InstallState CurrentState
        {
            get
            {
                int installedState, actionState;
                uint ret = RemotableNativeMethods.MsiGetComponentState((int) this.session.Handle, this.name, out installedState, out actionState);
                if (ret != 0)
                {
                    if (ret == (uint) NativeMethods.Error.UNKNOWN_COMPONENT)
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret, this.name);
                    }
                    else
                    {
                        throw InstallerException.ExceptionFromReturnCode(ret);
                    }
                }
                return (InstallState) installedState;
            }
        }

        /// <summary>
        /// Gets or sets the action state of the designated Component.
        /// </summary>
        /// <exception cref="InvalidHandleException">the Session handle is invalid</exception>
        /// <exception cref="ArgumentException">an unknown Component was requested</exception>
        /// <exception cref="InstallCanceledException">the user exited the installation</exception>
        /// <remarks><p>
        /// Win32 MSI APIs:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msigetcomponentstate.asp">MsiGetComponentState</a>,
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msisetcomponentstate.asp">MsiSetComponentState</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public InstallState RequestState
        {
            get
            {
                int installedState, actionState;
                uint ret = RemotableNativeMethods.MsiGetComponentState((int) this.session.Handle, this.name, out installedState, out actionState);
                if (ret != 0)
                {
                    if (ret == (uint) NativeMethods.Error.UNKNOWN_COMPONENT)
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
                uint ret = RemotableNativeMethods.MsiSetComponentState((int) this.session.Handle, this.name, (int) value);
                if (ret != 0)
                {
                    if (ret == (uint) NativeMethods.Error.UNKNOWN_COMPONENT)
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
        /// Gets disk space per drive required to install a component.
        /// </summary>
        /// <param name="installState">Requested component state</param>
        /// <returns>A list of InstallCost structures, specifying the cost for each drive for the component</returns>
        /// <remarks><p>
        /// Win32 MSI API:
        /// <a href="http://msdn.microsoft.com/library/en-us/msi/setup/msienumcomponentcosts.asp">MsiEnumComponentCosts</a>
        /// </p></remarks>
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public IList<InstallCost> GetCost(InstallState installState)
        {
            IList<InstallCost> costs = new List<InstallCost>();
            StringBuilder driveBuf = new StringBuilder(20);
            for (uint i = 0; true; i++)
            {
                int cost, tempCost;
                uint driveBufSize = (uint) driveBuf.Capacity;
                uint ret = RemotableNativeMethods.MsiEnumComponentCosts(
                    (int) this.session.Handle,
                    this.name,
                    i,
                    (int) installState,
                    driveBuf,
                    ref driveBufSize,
                    out cost,
                    out tempCost);
                if (ret == (uint) NativeMethods.Error.NO_MORE_ITEMS) break;
                if (ret == (uint) NativeMethods.Error.MORE_DATA)
                {
                    driveBuf.Capacity = (int) ++driveBufSize;
                    ret = RemotableNativeMethods.MsiEnumComponentCosts(
                        (int) this.session.Handle,
                        this.name,
                        i,
                        (int) installState,
                        driveBuf,
                        ref driveBufSize,
                        out cost,
                        out tempCost);
                }

                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
                costs.Add(new InstallCost(driveBuf.ToString(), cost * 512L, tempCost * 512L));
            }
            return costs;
        }
    }
}
