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

namespace System.Management.Automation.Interpreter
{
    internal sealed class UpdatePositionInstruction : Instruction
    {
        private readonly int _sequencePoint;
        private readonly bool _checkBreakpoints;

        private UpdatePositionInstruction(bool checkBreakpoints, int sequencePoint)
        {
            _checkBreakpoints = checkBreakpoints;
            _sequencePoint = sequencePoint;
        }

        public override int Run(InterpretedFrame frame)
        {
            var functionContext = frame.FunctionContext;
            var context = frame.ExecutionContext;

            functionContext._currentSequencePointIndex = _sequencePoint;
            if (_checkBreakpoints)
            {
                if (context._debuggingMode > 0)
                {
                    context.Debugger.OnSequencePointHit(functionContext);
                }
            }

            return +1;
        }

        public static Instruction Create(int sequencePoint, bool checkBreakpoints)
        {
            return new UpdatePositionInstruction(checkBreakpoints, sequencePoint);
        }
    }
}
