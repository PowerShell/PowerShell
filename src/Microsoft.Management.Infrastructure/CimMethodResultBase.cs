/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

namespace Microsoft.Management.Infrastructure
{
    /// <summary>
    /// Represents a method result - either
    /// 1) a regular method result - return value and all the out parameter values (<see cref="CimMethodResult"/>)
    /// or
    /// 2) a single item of a streamed out parameter array (<see cref="CimMethodStreamedResult"/>)
    /// </summary>
    public abstract class CimMethodResultBase
    {
        internal CimMethodResultBase()
        {
        }
    }
}