// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// INTERNAL IMPLEMENTATION CLASS
    ///
    /// It manages the finite state machine for the sequence of formatting messages.
    /// It achieves this by maintaining a stack of OutputContext-derived objects.
    /// A predefined set of events allows the host of this class to process the information
    /// as it comes trough the finite state machine (push model)
    ///
    /// IMPORTANT: The code using this class will have to provide ALL the callbacks.
    /// </summary>
    internal class FormatMessagesContextManager
    {
        // callbacks declarations
        internal delegate OutputContext FormatContextCreationCallback(OutputContext parentContext, FormatInfoData formatData);
        internal delegate void FormatStartCallback(OutputContext c);
        internal delegate void FormatEndCallback(FormatEndData fe, OutputContext c);
        internal delegate void GroupStartCallback(OutputContext c);
        internal delegate void GroupEndCallback(GroupEndData fe, OutputContext c);
        internal delegate void PayloadCallback(FormatEntryData formatEntryData, OutputContext c);

        // callback instances
        internal FormatContextCreationCallback contextCreation = null;
        internal FormatStartCallback fs = null;
        internal FormatEndCallback fe = null;
        internal GroupStartCallback gs = null;
        internal GroupEndCallback ge = null;
        internal PayloadCallback payload = null;

        /// <summary>
        /// The current output context, as determined by the
        /// sequence of formatting messages in the object stream.
        /// </summary>
        internal abstract class OutputContext
        {
            /// <summary>
            /// </summary>
            /// <param name="parentContextInStack">Parent context in the stack, it can be null.</param>
            internal OutputContext(OutputContext parentContextInStack)
            {
                ParentContext = parentContextInStack;
            }

            /// <summary>
            /// The outer context: the context object pushed onto the
            /// stack before the current one. For the first object pushed onto
            /// the stack it will be null.
            /// </summary>
            internal OutputContext ParentContext { get; }
        }

        /// <summary>
        /// Process an object from an input stream. It manages the context stack and
        /// calls back on the specified event delegates.
        /// </summary>
        /// <param name="o">Object to process.</param>
        internal void Process(object o)
        {
            PacketInfoData formatData = o as PacketInfoData;
            FormatEntryData fed = formatData as FormatEntryData;
            if (fed != null)
            {
                OutputContext ctx = null;

                if (!fed.outOfBand)
                {
                    ctx = _stack.Peek();
                }
                //  notify for Payload
                this.payload(fed, ctx);
            }
            else
            {
                bool formatDataIsFormatStartData = formatData is FormatStartData;
                bool formatDataIsGroupStartData = formatData is GroupStartData;
                // it's one of our formatting messages
                // we assume for the moment that they are in the correct sequence
                if (formatDataIsFormatStartData || formatDataIsGroupStartData)
                {
                    OutputContext oc = this.contextCreation(this.ActiveOutputContext, formatData);
                    _stack.Push(oc);

                    // now we have the context properly set: need to notify the
                    // underlying algorithm to do the start document or group stuff
                    if (formatDataIsFormatStartData)
                    {
                        // notify for Fs
                        this.fs(oc);
                    }
                    else if (formatDataIsGroupStartData)
                    {
                        // GroupStartData gsd = (GroupStartData) formatData;
                        // notify for Gs
                        this.gs(oc);
                    }
                }
                else
                {
                    GroupEndData ged = formatData as GroupEndData;
                    FormatEndData fEndd = formatData as FormatEndData;
                    if (ged != null || fEndd != null)
                    {
                        OutputContext oc = _stack.Peek();
                        if (fEndd != null)
                        {
                            // notify for Fe, passing the Fe info, before a Pop()
                            this.fe(fEndd, oc);
                        }
                        else if (ged != null)
                        {
                            // notify for Fe, passing the Fe info, before a Pop()
                            this.ge(ged, oc);
                        }

                        _stack.Pop();
                    }
                }
            }
        }

        /// <summary>
        /// Access the active context (top of the stack). It can be null.
        /// </summary>
        internal OutputContext ActiveOutputContext
        {
            get { return (_stack.Count > 0) ? _stack.Peek() : null; }
        }

        /// <summary>
        /// Internal stack to manage context.
        /// </summary>
        private Stack<OutputContext> _stack = new Stack<OutputContext>();
    }
}
