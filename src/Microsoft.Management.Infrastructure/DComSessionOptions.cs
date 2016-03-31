/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using Microsoft.Management.Infrastructure.Options.Internal;

namespace Microsoft.Management.Infrastructure.Options
{
    /// <summary>
    /// Options of <see cref="CimSession"/> that uses DCOM as the transport protocol
    /// </summary>
    public class DComSessionOptions : CimSessionOptions
    {
        /// <summary>
        /// Creates a new <see cref="DComSessionOptions"/> instance
        /// </summary>
        public DComSessionOptions()
            : base(Native.ApplicationMethods.protocol_DCOM)
        {
        }

        /// <summary>
        /// Instantiates a deep copy of <paramref name="optionsToClone"/>
        /// </summary>
        /// <param name="optionsToClone">options to clone</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="optionsToClone"/> is <c>null</c></exception>
        public DComSessionOptions(DComSessionOptions optionsToClone)
            : base(optionsToClone)
        {
        }

        /// <summary>
        /// Sets packet privacy
        /// </summary>
        /// <value></value>
        public bool PacketPrivacy
        {
            set
            {
                this.AssertNotDisposed();

                Native.MiResult result = Native.DestinationOptionsMethods.SetPacketPrivacy(this.DestinationOptionsHandleOnDemand, value);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                bool privacy;
                Native.MiResult result = Native.DestinationOptionsMethods.GetPacketPrivacy(this.DestinationOptionsHandleOnDemand, out privacy);
                CimException.ThrowIfMiResultFailure(result);
                return privacy;
            }
        }

        /// <summary>
        /// Sets packet integrity
        /// </summary>
        /// <value></value>
        public bool PacketIntegrity
        {
            set
            {
                this.AssertNotDisposed();

                Native.MiResult result = Native.DestinationOptionsMethods.SetPacketIntegrity(
                    this.DestinationOptionsHandleOnDemand, value);
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                bool integrity;
                Native.MiResult result = Native.DestinationOptionsMethods.GetPacketIntegrity(
                    this.DestinationOptionsHandleOnDemand, out integrity);
                CimException.ThrowIfMiResultFailure(result);
                return integrity;
            }
        }

        /// <summary>
        /// Sets impersonation
        /// </summary>
        /// <value></value>
        public ImpersonationType Impersonation
        {
            set
            {
                this.AssertNotDisposed();

                Native.MiResult result = Native.DestinationOptionsMethods.SetImpersonationType(
                    this.DestinationOptionsHandleOnDemand, value.ToNativeType());
                CimException.ThrowIfMiResultFailure(result);
            }
            get
            {
                this.AssertNotDisposed();
                Native.DestinationOptionsMethods.MiImpersonationType type;
                Native.MiResult result = Native.DestinationOptionsMethods.GetImpersonationType(
                    this.DestinationOptionsHandleOnDemand, out type);
                CimException.ThrowIfMiResultFailure(result);
                return (ImpersonationType)type;
            }
        }
    }
}