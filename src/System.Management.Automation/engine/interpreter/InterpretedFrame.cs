/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation.
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A
 * copy of the license can be found in the License.html file at the root of this distribution. If
 * you cannot locate the Apache License, Version 2.0, please send an email to
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

#if !CLR2
#else
using Microsoft.Scripting.Ast;
#endif
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation.Language;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Management.Automation.Interpreter
{
    internal sealed class InterpretedFrame
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly ThreadLocal<InterpretedFrame> CurrentFrame = new ThreadLocal<InterpretedFrame>();

        internal readonly Interpreter Interpreter;
        internal InterpretedFrame _parent;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        private int[] _continuations;
        private int _continuationIndex;
        private int _pendingContinuation;
        private object _pendingValue;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        public readonly object[] Data;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2105:ArrayFieldsShouldNotBeReadOnly")]
        public readonly StrongBox<object>[] Closure;

        public int StackIndex;
        public int InstructionIndex;

        // When a ThreadAbortException is raised from interpreted code this is the first frame that caught it.
        // No handlers within this handler re-abort the current thread when left.
        public ExceptionHandler CurrentAbortHandler;

        internal InterpretedFrame(Interpreter interpreter, StrongBox<object>[] closure)
        {
            Interpreter = interpreter;
            StackIndex = interpreter.LocalCount;
            Data = new object[StackIndex + interpreter.Instructions.MaxStackDepth];

            int c = interpreter.Instructions.MaxContinuationDepth;
            if (c > 0)
            {
                _continuations = new int[c];
            }

            Closure = closure;

            _pendingContinuation = -1;
            _pendingValue = Interpreter.NoValue;
        }

        public DebugInfo GetDebugInfo(int instructionIndex)
        {
            return DebugInfo.GetMatchingDebugInfo(Interpreter._debugInfos, instructionIndex);
        }

        public string Name
        {
            get { return Interpreter._name; }
        }

        #region Data Stack Operations

        public void Push(object value)
        {
            Data[StackIndex++] = value;
        }

        public void Push(bool value)
        {
            Data[StackIndex++] = value ? ScriptingRuntimeHelpers.True : ScriptingRuntimeHelpers.False;
        }

        public void Push(int value)
        {
            Data[StackIndex++] = ScriptingRuntimeHelpers.Int32ToObject(value);
        }

        public object Pop()
        {
            return Data[--StackIndex];
        }

        internal void SetStackDepth(int depth)
        {
            StackIndex = Interpreter.LocalCount + depth;
        }

        public object Peek()
        {
            return Data[StackIndex - 1];
        }

        public void Dup()
        {
            int i = StackIndex;
            Data[i] = Data[i - 1];
            StackIndex = i + 1;
        }

        public ExecutionContext ExecutionContext
        {
            get { return (ExecutionContext)Data[1]; }
        }

        public FunctionContext FunctionContext
        {
            get { return (FunctionContext)Data[0]; }
        }

        #endregion

        #region Stack Trace

        public InterpretedFrame Parent
        {
            get { return _parent; }
        }

        public static bool IsInterpretedFrame(MethodBase method)
        {
            // ContractUtils.RequiresNotNull(method, "method");
            return method.DeclaringType == typeof(Interpreter) && method.Name == "Run";
        }

        /// <summary>
        /// A single interpreted frame might be represented by multiple subsequent Interpreter.Run CLR frames.
        /// This method filters out the duplicate CLR frames.
        /// </summary>
        public static IEnumerable<StackFrame> GroupStackFrames(IEnumerable<StackFrame> stackTrace)
        {
            bool inInterpretedFrame = false;
            foreach (StackFrame frame in stackTrace)
            {
                if (InterpretedFrame.IsInterpretedFrame(frame.GetMethod()))
                {
                    if (inInterpretedFrame)
                    {
                        continue;
                    }

                    inInterpretedFrame = true;
                }
                else
                {
                    inInterpretedFrame = false;
                }

                yield return frame;
            }
        }

        public IEnumerable<InterpretedFrameInfo> GetStackTraceDebugInfo()
        {
            var frame = this;
            do
            {
                yield return new InterpretedFrameInfo(frame.Name, frame.GetDebugInfo(frame.InstructionIndex));
                frame = frame.Parent;
            } while (frame != null);
        }

        internal void SaveTraceToException(Exception exception)
        {
            if (exception.Data[typeof(InterpretedFrameInfo)] == null)
            {
                exception.Data[typeof(InterpretedFrameInfo)] = new List<InterpretedFrameInfo>(GetStackTraceDebugInfo()).ToArray();
            }
        }

        public static InterpretedFrameInfo[] GetExceptionStackTrace(Exception exception)
        {
            return exception.Data[typeof(InterpretedFrameInfo)] as InterpretedFrameInfo[];
        }

#if DEBUG
        internal string[] Trace
        {
            get
            {
                var trace = new List<string>();
                var frame = this;
                do
                {
                    trace.Add(frame.Name);
                    frame = frame.Parent;
                } while (frame != null);
                return trace.ToArray();
            }
        }
#endif

        internal ThreadLocal<InterpretedFrame>.StorageInfo Enter()
        {
            var currentFrame = InterpretedFrame.CurrentFrame.GetStorageInfo();
            _parent = currentFrame.Value;
            currentFrame.Value = this;
            return currentFrame;
        }

        internal void Leave(ThreadLocal<InterpretedFrame>.StorageInfo currentFrame)
        {
            currentFrame.Value = _parent;
        }

        #endregion

        #region Continuations

        internal bool IsJumpHappened()
        {
            return _pendingContinuation >= 0;
        }

        public void RemoveContinuation()
        {
            _continuationIndex--;
        }

        public void PushContinuation(int continuation)
        {
            _continuations[_continuationIndex++] = continuation;
        }

        public int YieldToCurrentContinuation()
        {
            var target = Interpreter._labels[_continuations[_continuationIndex - 1]];
            SetStackDepth(target.StackDepth);
            return target.Index - InstructionIndex;
        }

        /// <summary>
        /// Get called from the LeaveFinallyInstruction.
        /// </summary>
        public int YieldToPendingContinuation()
        {
            Debug.Assert(_pendingContinuation >= 0);
            RuntimeLabel pendingTarget = Interpreter._labels[_pendingContinuation];

            // the current continuation might have higher priority (continuationIndex is the depth of the current continuation):
            if (pendingTarget.ContinuationStackDepth < _continuationIndex)
            {
                RuntimeLabel currentTarget = Interpreter._labels[_continuations[_continuationIndex - 1]];
                SetStackDepth(currentTarget.StackDepth);
                return currentTarget.Index - InstructionIndex;
            }

            SetStackDepth(pendingTarget.StackDepth);
            if (_pendingValue != Interpreter.NoValue)
            {
                Data[StackIndex - 1] = _pendingValue;
            }

            // Set the _pendingContinuation and _pendingValue to the default values if we finally gets to the Goto target
            _pendingContinuation = -1;
            _pendingValue = Interpreter.NoValue;
            return pendingTarget.Index - InstructionIndex;
        }

        internal void PushPendingContinuation()
        {
            Push(_pendingContinuation);
            Push(_pendingValue);

            _pendingContinuation = -1;
            _pendingValue = Interpreter.NoValue;
        }

        internal void PopPendingContinuation()
        {
            _pendingValue = Pop();
            _pendingContinuation = (int)Pop();
        }

        private static MethodInfo s_goto;
        private static MethodInfo s_voidGoto;

        internal static MethodInfo GotoMethod
        {
            get { return s_goto ?? (s_goto = typeof(InterpretedFrame).GetMethod("Goto")); }
        }

        internal static MethodInfo VoidGotoMethod
        {
            get { return s_voidGoto ?? (s_voidGoto = typeof(InterpretedFrame).GetMethod("VoidGoto")); }
        }

        public int VoidGoto(int labelIndex)
        {
            return Goto(labelIndex, Interpreter.NoValue, gotoExceptionHandler: false);
        }

        public int Goto(int labelIndex, object value, bool gotoExceptionHandler)
        {
            // TODO: we know this at compile time (except for compiled loop):
            RuntimeLabel target = Interpreter._labels[labelIndex];
            Debug.Assert(!gotoExceptionHandler || _continuationIndex == target.ContinuationStackDepth,
                "When it's time to jump to the exception handler, all previous finally blocks should already be processed");

            if (_continuationIndex == target.ContinuationStackDepth)
            {
                SetStackDepth(target.StackDepth);
                if (value != Interpreter.NoValue)
                {
                    Data[StackIndex - 1] = value;
                }

                return target.Index - InstructionIndex;
            }

            // if we are in the middle of executing jump we forget the previous target and replace it by a new one:
            _pendingContinuation = labelIndex;
            _pendingValue = value;
            return YieldToCurrentContinuation();
        }

        #endregion
    }
}
