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

using System.Runtime.CompilerServices;

namespace System.Management.Automation.Interpreter
{
    /// <summary>
    /// Implements dynamic call site with many arguments. Wraps the arguments into <see cref="ArgumentArray"/>.
    /// </summary>
    internal sealed partial class DynamicSplatInstruction : Instruction
    {
        private readonly CallSite<Func<CallSite, ArgumentArray, object>> _site;
        private readonly int _argumentCount;

        internal DynamicSplatInstruction(int argumentCount, CallSite<Func<CallSite, ArgumentArray, object>> site)
        {
            _site = site;
            _argumentCount = argumentCount;
        }

        public override int ProducedStack { get { return 1; } }

        public override int ConsumedStack { get { return _argumentCount; } }

        public override int Run(InterpretedFrame frame)
        {
            int first = frame.StackIndex - _argumentCount;
            object ret = _site.Target(_site, new ArgumentArray(frame.Data, first, _argumentCount));
            frame.Data[first] = ret;
            frame.StackIndex = first + 1;

            return 1;
        }

        public override string ToString()
        {
            return "DynamicSplatInstruction(" + _site + ")";
        }
    }
}
