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
    [Cmdlet(VerbsData.Export, BinaryMiLogBase.Noun, HelpUri = "http://go.microsoft.com/fwlink/?LinkId=301310")]
    public sealed class ExportBinaryMiLogCommand : BinaryMiLogBase, IDisposable
    {
        [Parameter(ValueFromPipeline = true)]
        [ValidateNotNull]
        public CimInstance InputObject { get; set; }

        private static class NativeMethods 
        {
            internal class WriterHandle : SafeHandleZeroOrMinusOneIsInvalid
            {
                private WriterHandle()
                    : base(true)
                {
                }

                [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
                protected override bool ReleaseHandle()
                {
                    Native.InstanceHandle errorHandle = null;
                    Native.MiResult result = SyncBmilWriter_Delete(this.handle, out errorHandle);
                    return true;
                }
            }

            [DllImport("mpunits.dll", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Native.MiResult SyncBmilWriter_Create(
                string filePath, 
                out WriterHandle writerHandle, 
                out Native.InstanceHandle errorHandle);

            [DllImport("mpunits.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Native.MiResult SyncBmilWriter_WriteInstance(
                WriterHandle writerHandle,
                Native.InstanceHandle instanceHandle,
                out Native.InstanceHandle errorHandle);

            [DllImport("mpunits.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
            private static extern Native.MiResult SyncBmilWriter_Delete(
                IntPtr writerHandle,
                out Native.InstanceHandle errorHandle);
        }

        private sealed class Writer : IDisposable
        {
            private readonly NativeMethods.WriterHandle _writerHandle = null;

            internal Writer(string file)
            {
                Native.InstanceHandle errorHandle;
                Native.MiResult miResult;
                miResult = NativeMethods.SyncBmilWriter_Create(file, out _writerHandle, out errorHandle);
                CimException.ThrowIfMiResultFailure(miResult, errorHandle);
            }

            internal void WriteInstance(CimInstance cimInstance)
            {
                if (cimInstance == null)
                {
                    throw new ArgumentNullException("cimInstance");
                }

                Native.InstanceHandle errorHandle;
                Native.MiResult miResult;
                miResult = NativeMethods.SyncBmilWriter_WriteInstance(_writerHandle, cimInstance.InstanceHandle, out errorHandle);
                CimException.ThrowIfMiResultFailure(miResult, errorHandle);
            }

            public void Dispose()
            {
                _writerHandle.Dispose();
            }
        }

        private Writer _writer;

        protected override void BeginProcessing()
        {
            _writer = new Writer(this.GetVerifiedFilePath(FileAccess.Write));
        }

        protected override void ProcessRecord()
        {
            _writer.WriteInstance(InputObject);
        }

        public void Dispose()
        {
            if (_writer != null)
            {
                _writer.Dispose();
                _writer = null;
            }
        }
    }
}
