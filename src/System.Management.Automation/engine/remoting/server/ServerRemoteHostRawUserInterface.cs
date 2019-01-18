// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Management.Automation.Host;
using Dbg = System.Management.Automation.Diagnostics;
// Stops compiler from warning about unknown warnings
#pragma warning disable 1634, 1691

namespace System.Management.Automation.Remoting
{
    /// <summary>
    /// The ServerRemoteHostRawUserInterface class.
    /// </summary>
    internal class ServerRemoteHostRawUserInterface : PSHostRawUserInterface
    {
        /// <summary>
        /// Remote host user interface.
        /// </summary>
        private ServerRemoteHostUserInterface _remoteHostUserInterface;

        /// <summary>
        /// Server method executor.
        /// </summary>
        private ServerMethodExecutor _serverMethodExecutor;

        /// <summary>
        /// Host default data.
        /// </summary>
        private HostDefaultData HostDefaultData
        {
            get
            {
                return _remoteHostUserInterface.ServerRemoteHost.HostInfo.HostDefaultData;
            }
        }

        /// <summary>
        /// Constructor for ServerRemoteHostRawUserInterface.
        /// </summary>
        internal ServerRemoteHostRawUserInterface(ServerRemoteHostUserInterface remoteHostUserInterface)
        {
            Dbg.Assert(remoteHostUserInterface != null, "Expected remoteHostUserInterface != null");
            _remoteHostUserInterface = remoteHostUserInterface;
            Dbg.Assert(!remoteHostUserInterface.ServerRemoteHost.HostInfo.IsHostRawUINull, "Expected !remoteHostUserInterface.ServerRemoteHost.HostInfo.IsHostRawUINull");

            _serverMethodExecutor = remoteHostUserInterface.ServerRemoteHost.ServerMethodExecutor;
        }

        /// <summary>
        /// Foreground color.
        /// </summary>
        public override ConsoleColor ForegroundColor
        {
            get
            {
                if (this.HostDefaultData.HasValue(HostDefaultDataId.ForegroundColor))
                {
                    return (ConsoleColor)this.HostDefaultData.GetValue(HostDefaultDataId.ForegroundColor);
                }
                else
                {
                    throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.GetForegroundColor);
                }
            }

            set
            {
                this.HostDefaultData.SetValue(HostDefaultDataId.ForegroundColor, value);
                _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.SetForegroundColor, new object[] { value });
            }
        }

        /// <summary>
        /// Background color.
        /// </summary>
        public override ConsoleColor BackgroundColor
        {
            get
            {
                if (this.HostDefaultData.HasValue(HostDefaultDataId.BackgroundColor))
                {
                    return (ConsoleColor)this.HostDefaultData.GetValue(HostDefaultDataId.BackgroundColor);
                }
                else
                {
                    throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.GetBackgroundColor);
                }
            }

            set
            {
                this.HostDefaultData.SetValue(HostDefaultDataId.BackgroundColor, value);
                _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.SetBackgroundColor, new object[] { value });
            }
        }

        /// <summary>
        /// Cursor position.
        /// </summary>
        public override Coordinates CursorPosition
        {
            get
            {
                if (this.HostDefaultData.HasValue(HostDefaultDataId.CursorPosition))
                {
                    return (Coordinates)this.HostDefaultData.GetValue(HostDefaultDataId.CursorPosition);
                }
                else
                {
                    throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.GetCursorPosition);
                }
            }

            set
            {
                this.HostDefaultData.SetValue(HostDefaultDataId.CursorPosition, value);
                _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.SetCursorPosition, new object[] { value });
            }
        }

        /// <summary>
        /// Window position.
        /// </summary>
        public override Coordinates WindowPosition
        {
            get
            {
                if (this.HostDefaultData.HasValue(HostDefaultDataId.WindowPosition))
                {
                    return (Coordinates)this.HostDefaultData.GetValue(HostDefaultDataId.WindowPosition);
                }
                else
                {
                    throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.GetWindowPosition);
                }
            }

            set
            {
                this.HostDefaultData.SetValue(HostDefaultDataId.WindowPosition, value);
                _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.SetWindowPosition, new object[] { value });
            }
        }

        /// <summary>
        /// Cursor size.
        /// </summary>
        public override int CursorSize
        {
            get
            {
                if (this.HostDefaultData.HasValue(HostDefaultDataId.CursorSize))
                {
                    return (int)this.HostDefaultData.GetValue(HostDefaultDataId.CursorSize);
                }
                else
                {
                    throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.GetCursorSize);
                }
            }

            set
            {
                this.HostDefaultData.SetValue(HostDefaultDataId.CursorSize, value);
                _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.SetCursorSize, new object[] { value });
            }
        }

        /// <summary>
        /// Buffer size.
        /// </summary>
        public override Size BufferSize
        {
            get
            {
                if (this.HostDefaultData.HasValue(HostDefaultDataId.BufferSize))
                {
                    return (Size)this.HostDefaultData.GetValue(HostDefaultDataId.BufferSize);
                }
                else
                {
                    throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.GetBufferSize);
                }
            }

            set
            {
                this.HostDefaultData.SetValue(HostDefaultDataId.BufferSize, value);
                _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.SetBufferSize, new object[] { value });
            }
        }

        /// <summary>
        /// Window size.
        /// </summary>
        public override Size WindowSize
        {
            get
            {
                if (this.HostDefaultData.HasValue(HostDefaultDataId.WindowSize))
                {
                    return (Size)this.HostDefaultData.GetValue(HostDefaultDataId.WindowSize);
                }
                else
                {
                    throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.GetWindowSize);
                }
            }

            set
            {
                this.HostDefaultData.SetValue(HostDefaultDataId.WindowSize, value);
                _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.SetWindowSize, new object[] { value });
            }
        }

        /// <summary>
        /// Window title.
        /// </summary>
        public override string WindowTitle
        {
            get
            {
                if (this.HostDefaultData.HasValue(HostDefaultDataId.WindowTitle))
                {
                    return (string)this.HostDefaultData.GetValue(HostDefaultDataId.WindowTitle);
                }
                else
                {
                    throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.GetWindowTitle);
                }
            }

            set
            {
                this.HostDefaultData.SetValue(HostDefaultDataId.WindowTitle, value);
                _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.SetWindowTitle, new object[] { value });
            }
        }

        /// <summary>
        /// Max window size.
        /// </summary>
        public override Size MaxWindowSize
        {
            get
            {
                if (this.HostDefaultData.HasValue(HostDefaultDataId.MaxWindowSize))
                {
                    return (Size)this.HostDefaultData.GetValue(HostDefaultDataId.MaxWindowSize);
                }
                else
                {
                    throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.GetMaxWindowSize);
                }
            }
        }

        /// <summary>
        /// Max physical window size.
        /// </summary>
        public override Size MaxPhysicalWindowSize
        {
            get
            {
                if (this.HostDefaultData.HasValue(HostDefaultDataId.MaxPhysicalWindowSize))
                {
                    return (Size)this.HostDefaultData.GetValue(HostDefaultDataId.MaxPhysicalWindowSize);
                }
                else
                {
                    throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.GetMaxPhysicalWindowSize);
                }
            }
        }

        /// <summary>
        /// Key available.
        /// </summary>
        public override bool KeyAvailable
        {
#pragma warning disable 56503
            get
            {
                throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.GetKeyAvailable);
            }
#pragma warning restore 56503
        }

        /// <summary>
        /// Read key.
        /// </summary>
        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            return _serverMethodExecutor.ExecuteMethod<KeyInfo>(RemoteHostMethodId.ReadKey, new object[] { options });
        }

        /// <summary>
        /// Flush input buffer.
        /// </summary>
        public override void FlushInputBuffer()
        {
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.FlushInputBuffer);
        }

        /// <summary>
        /// Scroll buffer contents.
        /// </summary>
        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.ScrollBufferContents, new object[] { source, destination, clip, fill });
        }

        /// <summary>
        /// Set buffer contents.
        /// </summary>
        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.SetBufferContents1, new object[] { rectangle, fill });
        }

        /// <summary>
        /// Set buffer contents.
        /// </summary>
        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents)
        {
            _serverMethodExecutor.ExecuteVoidMethod(RemoteHostMethodId.SetBufferContents2, new object[] { origin, contents });
        }

        /// <summary>
        /// Get buffer contents.
        /// </summary>
        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            // This method had an implementation earlier. However, owing
            // to a potential security risk of a malicious server scrapping
            // the screen contents of a client, this is now removed
            throw RemoteHostExceptions.NewNotImplementedException(RemoteHostMethodId.GetBufferContents);
        }

        // same as the implementation in the base class; included here to make it easier
        // to keep the other overload in sync: LengthInBufferCells(string, int)
        public override int LengthInBufferCells(string source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            return source.Length;
        }

        // more performant than the default implementation provided by PSHostRawUserInterface
        public override int LengthInBufferCells(string source, int offset)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            Dbg.Assert(offset >= 0, "offset >= 0");
            Dbg.Assert(string.IsNullOrEmpty(source) || (offset < source.Length), "offset < source.Length");

            return source.Length - offset;
        }
    }
}
