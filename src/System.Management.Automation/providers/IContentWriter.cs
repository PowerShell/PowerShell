// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.IO;

namespace System.Management.Automation.Provider
{
    #region IContentWriter

    /// <summary>
    /// A Cmdlet provider that implements the IContentCmdletProvider interface must provide an
    /// object that implements this interface when GetContentWriter() is called.
    ///
    /// The interface allows for writing content to an item.
    /// </summary>
    public interface IContentWriter : IDisposable
    {
        /// <summary>
        /// Writes content to the item.
        /// </summary>
        /// <param name="content">
        /// An array of content "blocks" to be written to the item.
        /// </param>
        /// <returns>
        /// The blocks of content that were successfully written to the item.
        /// </returns>
        /// <remarks>
        /// A "block" of content is provider specific.  For the file system
        /// a "block" may be considered a byte, a character, or delimited string.
        ///
        /// The implementation of this method should treat each element in the
        /// <paramref name="content"/> parameter as a block. Each additional
        /// call to this method should append any new values to the content
        /// writer's current location until <see cref="IContentWriter.Close"/> is called.
        /// </remarks>
        IList Write(IList content);

        /// <summary>
        /// Moves the current "block" to be written to a position relative to a place
        /// in the writer.
        /// </summary>
        /// <param name="offset">
        /// An offset of the number of blocks to seek from the origin.
        /// </param>
        /// <param name="origin">
        /// The place in the stream to start the seek from.
        /// </param>
        /// <remarks>
        /// The implementation of this method moves the content writer <paramref name="offset"/>
        /// number of blocks from the specified <paramref name="origin"/>. See <see cref="System.Management.Automation.Provider.IContentWriter.Write(IList)"/>
        /// for a description of what a block is.
        /// </remarks>
        void Seek(long offset, SeekOrigin origin);

        /// <summary>
        /// Closes the writer. Further writes should fail if the writer
        /// has been closed.
        /// </summary>
        /// <remarks>
        /// The implementation of this method should close any resources held open by the
        /// writer.
        /// </remarks>
        void Close();
    }

    #endregion IContentWriter
}

