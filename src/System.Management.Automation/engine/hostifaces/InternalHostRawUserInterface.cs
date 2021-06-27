// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
            _externalRawUI = externalRawUI;
            _parentHost = parentHost;
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
        /// See base class.
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
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                ConsoleColor result = _externalRawUI.ForegroundColor;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.ForegroundColor = value;
            }
        }

        /// <summary>
        /// See base class.
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
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                ConsoleColor result = _externalRawUI.BackgroundColor;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.BackgroundColor = value;
            }
        }

        /// <summary>
        /// See base class.
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
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Coordinates result = _externalRawUI.CursorPosition;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.CursorPosition = value;
            }
        }

        /// <summary>
        /// See base class.
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
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Coordinates result = _externalRawUI.WindowPosition;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.WindowPosition = value;
            }
        }

        /// <summary>
        /// See base class.
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
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                int result = _externalRawUI.CursorSize;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.CursorSize = value;
            }
        }

        /// <summary>
        /// See base class.
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
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Size result = _externalRawUI.BufferSize;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.BufferSize = value;
            }
        }

        /// <summary>
        /// See base class.
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
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Size result = _externalRawUI.WindowSize;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.WindowSize = value;
            }
        }

        /// <summary>
        /// See base class.
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
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Size result = _externalRawUI.MaxWindowSize;

                return result;
            }
        }

        /// <summary>
        /// See base class.
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
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                Size result = _externalRawUI.MaxPhysicalWindowSize;

                return result;
            }
        }

        /// <summary>
        /// See base class.
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
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            KeyInfo result = new KeyInfo();
            try
            {
                result = _externalRawUI.ReadKey(options);
            }
            catch (PipelineStoppedException)
            {
                // PipelineStoppedException is thrown by host when it wants
                // to stop the pipeline.
                LocalPipeline lpl = (LocalPipeline)((RunspaceBase)_parentHost.Context.CurrentRunspace).GetCurrentlyRunningPipeline();
                if (lpl == null)
                {
                    throw;
                }

                lpl.Stopper.Stop();
            }

            return result;
        }

        /// <summary>
        /// See base class.
        /// </summary>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>
        public override
        void
        FlushInputBuffer()
        {
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            _externalRawUI.FlushInputBuffer();
        }

        /// <summary>
        /// See base class.
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
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                bool result = _externalRawUI.KeyAvailable;

                return result;
            }
        }

        /// <summary>
        /// See base class.
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
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                string result = _externalRawUI.WindowTitle;

                return result;
            }

            set
            {
                if (_externalRawUI == null)
                {
                    ThrowNotInteractive();
                }

                _externalRawUI.WindowTitle = value;
            }
        }

        /// <summary>
        /// See base class.
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
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            _externalRawUI.SetBufferContents(origin, contents);
        }

        /// <summary>
        /// See base class.
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
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            _externalRawUI.SetBufferContents(r, fill);
        }

        /// <summary>
        /// See base class.
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
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            return _externalRawUI.GetBufferContents(r);
        }

        /// <summary>
        /// See base class.
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
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            _externalRawUI.ScrollBufferContents(source, destination, clip, fill);
        }

        /// <summary>
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        /// <exception cref="HostException">
        /// if the RawUI property of the external host is null, possibly because the PSHostRawUserInterface is not
        ///  implemented by the external host
        /// </exception>
        public override int LengthInBufferCells(string str)
        {
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            return _externalRawUI.LengthInBufferCells(str);
        }

        /// <summary>
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

            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            return _externalRawUI.LengthInBufferCells(str, offset);
        }

        /// <summary>
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
            if (_externalRawUI == null)
            {
                ThrowNotInteractive();
            }

            return _externalRawUI.LengthInBufferCells(character);
        }

        private readonly PSHostRawUserInterface _externalRawUI;
        private readonly InternalHost _parentHost;
    }
}
