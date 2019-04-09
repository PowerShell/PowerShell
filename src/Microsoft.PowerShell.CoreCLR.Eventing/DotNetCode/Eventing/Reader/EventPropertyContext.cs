// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/*============================================================
**
**
** Purpose:
** This public abstract class defines the methods / properties
** for a context object used to access a set of Data Values from
** an EventRecord.
**
============================================================*/

namespace System.Diagnostics.Eventing.Reader
{
    public abstract class EventPropertyContext : IDisposable
    {
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
