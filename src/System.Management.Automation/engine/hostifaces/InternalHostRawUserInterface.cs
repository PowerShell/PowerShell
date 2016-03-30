/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/



using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;

using Dbg = System.Management.Automation.Diagnostics;



namespace System.Management.Automation.Internal.Host
{
    internal
    class InternalHostRawUserInterface : PSHostRawUserInterface
    {
        internal
        InternalHostRawUserInterface(PSHostRawUserInterface externalRawUI, InternalHost parentHost)
        {
            // externalRawUI may be null
            this.externalRawUI = externalRawUI;
            this.parentHost = parentHost;
        }



        internal
        void
        ThrowNotInteractive()
        {
            // It might be interesting to do something like
            // GetCallingMethodAndParameters here and display the name,
            // but I don't want to put that in mainline non-trace code.
            string message = HostInterfaceExceptionsStrings.HostFunctionNotImplemented;
            HostException e = new HostException(
                message,
                null,
                "HostFunctionNotImplemented",
                ErrorCategory.NotImplemented);
            throw e;
        }


        
        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>
        public override
        ConsoleColor
        ForegroundColor
        {
            get
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                ConsoleColor result = externalRawUI.ForegroundColor;

                return result;
            }
            set
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                externalRawUI.ForegroundColor = value;
            }
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        ConsoleColor
        BackgroundColor
        {
            get
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                ConsoleColor result = externalRawUI.BackgroundColor;

                return result;
            }
            set
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                externalRawUI.BackgroundColor = value;
            }
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        Coordinates
        CursorPosition
        {
            get
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Coordinates result = externalRawUI.CursorPosition;

                return result;
            }
            set
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                externalRawUI.CursorPosition = value;
            }
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        Coordinates
        WindowPosition
        {
            get
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Coordinates result = externalRawUI.WindowPosition;

                return result;
            }
            set
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                externalRawUI.WindowPosition = value;
            }
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        int
        CursorSize
        {
            get
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                int result = externalRawUI.CursorSize;

                return result;
            }
            set
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                externalRawUI.CursorSize = value;
            }
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        Size
        BufferSize
        {
            get
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Size result = externalRawUI.BufferSize;

                return result;
            }
            set
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                externalRawUI.BufferSize = value;
            }
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        Size
        WindowSize
        {
            get
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Size result = externalRawUI.WindowSize;

                return result;
            }
            set
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                externalRawUI.WindowSize = value;
            }
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        Size
        MaxWindowSize
        {
            get
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Size result = externalRawUI.MaxWindowSize;

                return result;
            }
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        Size
        MaxPhysicalWindowSize
        {
            get
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Size result = externalRawUI.MaxPhysicalWindowSize;

                return result;
            }
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <param name="options">
        /// </param>
        /// <returns></returns>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        KeyInfo
        ReadKey(ReadKeyOptions options)
        {
            if (externalRawUI == null)
            {
                ThrowNotInteractive();
            }
            KeyInfo result = new KeyInfo();
            try
            {
               result = externalRawUI.ReadKey(options);
            }
            catch (PipelineStoppedException)
            {
                //PipelineStoppedException is thrown by host when it wants 
                //to stop the pipeline. 
                LocalPipeline lpl = (LocalPipeline)((RunspaceBase)this.parentHost.Context.CurrentRunspace).GetCurrentlyRunningPipeline();
                if (lpl == null)
                {
                    throw;
                }
                lpl.Stopper.Stop();
            }

            return result;
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        void
        FlushInputBuffer()
        {
            if (externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            externalRawUI.FlushInputBuffer();
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        bool
        KeyAvailable
        {
            get
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                bool result = externalRawUI.KeyAvailable;

                return result;
            }
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <value></value>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        string
        WindowTitle
        {
            get
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                string result = externalRawUI.WindowTitle;

                return result;
            }
            set
            {
                if (externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                externalRawUI.WindowTitle = value;
            }
        }


        
        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="contents"></param>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>
        public override
        void
        SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            if (externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            externalRawUI.SetBufferContents(origin, contents);
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <param name="r">
        /// </param>
        /// <param name="fill">
        /// </param>
        /// <remarks>
        /// </remarks>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        void
        SetBufferContents(Rectangle r, BufferCell fill)
        {
            if (externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            externalRawUI.SetBufferContents(r, fill);
        }



        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        BufferCell[,]
        GetBufferContents(Rectangle r)
        {
            if (externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            return externalRawUI.GetBufferContents(r);
        }
 


        /// <summary>
        /// 
        /// See base class
        /// 
        /// </summary>
        /// <param name="source">
        /// </param>
        /// <param name="destination">
        /// </param>
        /// <param name="clip">
        /// </param>
        /// <param name="fill">
        /// </param>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>

        public override
        void
        ScrollBufferContents
        (
            Rectangle source,     
            Coordinates destination,
            Rectangle clip,       
            BufferCell fill
        )
        {
            if (externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            externalRawUI.ScrollBufferContents(source, destination, clip, fill);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>
        public override int LengthInBufferCells(string str)
        {
            if (externalRawUI == null)
            {
                ThrowNotInteractive();
            }
            return externalRawUI.LengthInBufferCells(str);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>
        public override int LengthInBufferCells(string str, int offset)
        {
            Dbg.Assert(offset >= 0, "offset >= 0");
            Dbg.Assert(string.IsNullOrEmpty(str) || (offset < str.Length), "offset < str.Length");

            if (externalRawUI == null)
            {
                ThrowNotInteractive();
            }
            return externalRawUI.LengthInBufferCells(str, offset);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>
        public override
        int
        LengthInBufferCells(char character)
        {
            if (externalRawUI == null)
            {
                ThrowNotInteractive();
            }
            return externalRawUI.LengthInBufferCells(character);
        }

        private PSHostRawUserInterface externalRawUI;
        private InternalHost parentHost;
    }
    
}  // namespace 
