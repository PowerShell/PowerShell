// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation
{
    /// <summary>
    /// This is the base class from which other classes should derive.
    /// This class defines the options for the specified configuration type.
    /// </summary>
    public abstract class PSSessionTypeOption
    {
        /// <summary>
        /// Returns a xml formatted data that represents the options.
        /// </summary>
        /// <returns></returns>
        protected internal virtual string ConstructPrivateData()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a new instance constructed from privateData string.
        /// </summary>
        /// <returns></returns>
        protected internal virtual PSSessionTypeOption ConstructObjectFromPrivateData(string privateData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copies values from updated.  Only non default values are copies.
        /// </summary>
        /// <param name="updated"></param>
        protected internal virtual void CopyUpdatedValuesFrom(PSSessionTypeOption updated)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// This the abstract class that defines the options for underlying transport layer.
    /// </summary>
    public abstract class PSTransportOption : ICloneable
    {
        /// <summary>
        /// Returns all the non-quota options set in this object in a format of xml attributes.
        /// </summary>
        /// <returns></returns>
        internal virtual string ConstructOptionsAsXmlAttributes()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns all the non-quota options set in this object in a name-value pair (hashtable).
        /// </summary>
        /// <returns></returns>
        internal virtual Hashtable ConstructOptionsAsHashtable()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns all the quota related options set in this object in a format of xml attributes.
        /// </summary>
        /// <returns></returns>
        internal virtual string ConstructQuotas()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns all the quota related options in the form of a hashtable.
        /// </summary>
        /// <returns></returns>
        internal virtual Hashtable ConstructQuotasAsHashtable()
        {
            throw new NotImplementedException();
        }

        internal void LoadFromDefaults(PSSessionType sessionType)
        {
            LoadFromDefaults(sessionType, false);
        }

        /// <summary>
        /// Sets all the values to default values.
        /// If keepAssigned is true only those values are set
        /// which are unassigned.
        /// </summary>
        /// <param name="sessionType"></param>
        /// <param name="keepAssigned"></param>
        protected internal virtual void LoadFromDefaults(PSSessionType sessionType, bool keepAssigned)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clone from ICloneable.
        /// </summary>
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
