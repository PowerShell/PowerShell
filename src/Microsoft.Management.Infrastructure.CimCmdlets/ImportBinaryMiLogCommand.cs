/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

#if CORECLR
// Use stubs for SafeHandleZeroOrMinusOneIsInvalid and ReliabilityContractAttribute
using Microsoft.PowerShell.CoreClr.Stubs;
#else
using System.Runtime.ConstrainedExecution;
#endif

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    [Cmdlet(VerbsData.Import, BinaryMiLogBase.Noun, HelpUri = "http://go.microsoft.com/fwlink/?LinkId=301309")]
    public sealed class ImportBinaryMiLogCommand : BinaryMiLogBase
    {
        private static class NativeMethods 
        {
            internal class ReaderHandle : SafeHandleZeroOrMinusOneIsInvalid
            {
                private ReaderHandle()
                    : base(true)
                {
                }

                [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
                protected override bool ReleaseHandle()
                {
                    Native.InstanceHandle errorHandle = null;
                    Native.MiResult result = SyncBmilReader_Delete(this.handle, out errorHandle);
                    return true;
                }
            }

            [DllImport("mpunits.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Native.MiResult SyncBmilReader_Create(
                string filePath, 
                out ReaderHandle readerHandle, 
                out Native.InstanceHandle errorHandle);

            [DllImport("mpunits.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Native.MiResult SyncBmilReader_ReadInstance(
                ReaderHandle readerHandle,
                out Native.InstanceHandle instanceHandle,
                out Native.InstanceHandle errorHandle);

            [DllImport("mpunits.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            private static extern Native.MiResult SyncBmilReader_Delete(
                IntPtr readerHandle,
                out Native.InstanceHandle errorHandle);
        }

        private sealed class Reader : IDisposable
        {
            private readonly NativeMethods.ReaderHandle _readerHandle;

            internal Reader(string file)
            {
                Native.InstanceHandle errorHandle;
                Native.MiResult miResult;
                miResult = NativeMethods.SyncBmilReader_Create(file, out _readerHandle, out errorHandle);
                CimException.ThrowIfMiResultFailure(miResult, errorHandle);
            }

            internal CimInstance ReadInstance()
            {
                Native.InstanceHandle errorHandle;
                Native.InstanceHandle instanceHandle;
                Native.MiResult miResult;
                miResult = NativeMethods.SyncBmilReader_ReadInstance(_readerHandle, out instanceHandle, out errorHandle);
                CimException.ThrowIfMiResultFailure(miResult, errorHandle);

                if (instanceHandle.IsInvalid)
                {
                    return null;
                }
                var cimInstance = new CimInstance(instanceHandle, null);
                return cimInstance;
            }

            public void Dispose()
            {
                _readerHandle.Dispose();
            }
        }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            this.GetVerifiedFilePath(FileAccess.Read);
            using (var reader = new Reader(this.GetVerifiedFilePath(FileAccess.Read)))
            {
                CimInstance cimInstance;
                do
                {
                    cimInstance = reader.ReadInstance();
                    if (cimInstance != null)
                    {
                        this.WriteObject(cimInstance);
                    }
                } while (cimInstance != null);
            }
        }
    }
}
