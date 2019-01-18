// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// ReSharper disable UnusedMember.Global
// ReSharper disable RedundantCast

namespace System.Management.Automation
{
    internal static class Boxed
    {
        internal static object True = (object)true;
        internal static object False = (object)false;
    }

    internal static class IntOps
    {
        internal static object Add(int lhs, int rhs)
        {
            long result = (long)lhs + (long)rhs;
            if (result <= int.MaxValue && result >= int.MinValue)
            {
                return (int)result;
            }

            return (double)result;
        }

        internal static object Sub(int lhs, int rhs)
        {
            long result = (long)lhs - (long)rhs;
            if (result <= int.MaxValue && result >= int.MinValue)
            {
                return (int)result;
            }

            return (double)result;
        }

        internal static object Multiply(int lhs, int rhs)
        {
            long result = (long)lhs * (long)rhs;
            if (result <= int.MaxValue && result >= int.MinValue)
            {
                return (int)result;
            }

            return (double)result;
        }

        internal static object Divide(int lhs, int rhs)
        {
            // TBD: is it better to cover the special cases explicitly, or
            //      alternatively guard with try/catch?

            if (rhs == 0)
            {
                DivideByZeroException dbze = new DivideByZeroException();
                throw new RuntimeException(dbze.Message, dbze);
            }

            if (lhs == int.MinValue && rhs == -1)
            {
                // The result of this operation can't fit in an int, so promote.
                return (double)lhs / (double)rhs;
            }

            // If the remainder is 0, stay with integer division, otherwise use doubles.
            if ((lhs % rhs) == 0)
            {
                return lhs / rhs;
            }

            return (double)lhs / (double)rhs;
        }

        internal static object Remainder(int lhs, int rhs)
        {
            // TBD: is it better to cover the special cases explicitly, or
            //      alternatively guard with try/catch?

            if (rhs == 0)
            {
                DivideByZeroException dbze = new DivideByZeroException();
                throw new RuntimeException(dbze.Message, dbze);
            }

            if (lhs == int.MinValue && rhs == -1)
            {
                // The CLR raises an overflow exception for these values.  PowerShell typically
                // promotes whenever things overflow, so we just hard code the result value.
                return 0;
            }

            return lhs % rhs;
        }

        internal static object CompareEq(int lhs, int rhs) { return (lhs == rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareNe(int lhs, int rhs) { return (lhs != rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareLt(int lhs, int rhs) { return (lhs < rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareLe(int lhs, int rhs) { return (lhs <= rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareGt(int lhs, int rhs) { return (lhs > rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareGe(int lhs, int rhs) { return (lhs >= rhs) ? Boxed.True : Boxed.False; }

        internal static object[] Range(int lower, int upper)
        {
            int absRange = Math.Abs(checked(upper - lower));

            object[] ra = new object[absRange + 1];
            if (lower > upper)
            {
                // 3 .. 1 => 3 2 1
                for (int offset = 0; offset < ra.Length; offset++)
                    ra[offset] = lower--;
            }
            else
            {
                // 1 .. 3 => 1 2 3
                for (int offset = 0; offset < ra.Length; offset++)
                    ra[offset] = lower++;
            }

            return ra;
        }
    }

    internal static class UIntOps
    {
        internal static object Add(uint lhs, uint rhs)
        {
            ulong result = (ulong)lhs + (ulong)rhs;
            if (result <= uint.MaxValue)
            {
                return (uint)result;
            }

            return (double)result;
        }

        internal static object Sub(uint lhs, uint rhs)
        {
            long result = (long)lhs - (long)rhs;
            if (result >= uint.MinValue)
            {
                return (uint)result;
            }

            return (double)result;
        }

        internal static object Multiply(uint lhs, uint rhs)
        {
            ulong result = (ulong)lhs * (ulong)rhs;
            if (result <= uint.MaxValue)
            {
                return (uint)result;
            }

            return (double)result;
        }

        internal static object Divide(uint lhs, uint rhs)
        {
            // TBD: is it better to cover the special cases explicitly, or
            //      alternatively guard with try/catch?

            if (rhs == 0)
            {
                DivideByZeroException dbze = new DivideByZeroException();
                throw new RuntimeException(dbze.Message, dbze);
            }

            // If the remainder is 0, stay with integer division, otherwise use doubles.
            if ((lhs % rhs) == 0)
            {
                return lhs / rhs;
            }

            return (double)lhs / (double)rhs;
        }

        internal static object Remainder(uint lhs, uint rhs)
        {
            // TBD: is it better to cover the special cases explicitly, or
            //      alternatively guard with try/catch?

            if (rhs == 0)
            {
                DivideByZeroException dbze = new DivideByZeroException();
                throw new RuntimeException(dbze.Message, dbze);
            }

            return lhs % rhs;
        }

        internal static object CompareEq(uint lhs, uint rhs) { return (lhs == rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareNe(uint lhs, uint rhs) { return (lhs != rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareLt(uint lhs, uint rhs) { return (lhs < rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareLe(uint lhs, uint rhs) { return (lhs <= rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareGt(uint lhs, uint rhs) { return (lhs > rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareGe(uint lhs, uint rhs) { return (lhs >= rhs) ? Boxed.True : Boxed.False; }
    }

    internal static class LongOps
    {
        internal static object Add(long lhs, long rhs)
        {
            decimal result = (decimal)lhs + (decimal)rhs;
            if (result <= long.MaxValue && result >= long.MinValue)
            {
                return (long)result;
            }

            return (double)result;
        }

        internal static object Sub(long lhs, long rhs)
        {
            decimal result = (decimal)lhs - (decimal)rhs;
            if (result <= long.MaxValue && result >= long.MinValue)
            {
                return (long)result;
            }

            return (double)result;
        }

        internal static object Multiply(long lhs, long rhs)
        {
            System.Numerics.BigInteger biLhs = lhs;
            System.Numerics.BigInteger biRhs = rhs;
            System.Numerics.BigInteger biResult = biLhs * biRhs;

            if (biResult <= long.MaxValue && biResult >= long.MinValue)
            {
                return (long)biResult;
            }

            return (double)biResult;
        }

        internal static object Divide(long lhs, long rhs)
        {
            // TBD: is it better to cover the special cases explicitly, or
            //      alternatively guard with try/catch?

            if (rhs == 0)
            {
                DivideByZeroException dbze = new DivideByZeroException();
                throw new RuntimeException(dbze.Message, dbze);
            }

            // Special case.
            // This changes the sign of the min value, causing an integer overflow.
            if (lhs == long.MinValue && rhs == -1)
            {
                return (double)lhs / (double)rhs;
            }

            // If the remainder is 0, stay with integer division, otherwise use doubles.
            if ((lhs % rhs) == 0)
            {
                return lhs / rhs;
            }

            return (double)lhs / (double)rhs;
        }

        internal static object Remainder(long lhs, long rhs)
        {
            // TBD: is it better to cover the special cases explicitly, or
            //      alternatively guard with try/catch?

            if (rhs == 0)
            {
                DivideByZeroException dbze = new DivideByZeroException();
                throw new RuntimeException(dbze.Message, dbze);
            }

            if (lhs == long.MinValue && rhs == -1)
            {
                // The CLR raises an overflow exception for these values.  PowerShell typically
                // promotes whenever things overflow, so we just hard code the result value.
                return 0L;
            }

            return lhs % rhs;
        }

        internal static object CompareEq(long lhs, long rhs) { return (lhs == rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareNe(long lhs, long rhs) { return (lhs != rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareLt(long lhs, long rhs) { return (lhs < rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareLe(long lhs, long rhs) { return (lhs <= rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareGt(long lhs, long rhs) { return (lhs > rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareGe(long lhs, long rhs) { return (lhs >= rhs) ? Boxed.True : Boxed.False; }
    }

    internal static class ULongOps
    {
        internal static object Add(ulong lhs, ulong rhs)
        {
            decimal result = (decimal)lhs + (decimal)rhs;
            if (result <= ulong.MaxValue)
            {
                return (ulong)result;
            }

            return (double)result;
        }

        internal static object Sub(ulong lhs, ulong rhs)
        {
            decimal result = (decimal)lhs - (decimal)rhs;
            if (result >= ulong.MinValue)
            {
                return (ulong)result;
            }

            return (double)result;
        }

        internal static object Multiply(ulong lhs, ulong rhs)
        {
            System.Numerics.BigInteger biLhs = lhs;
            System.Numerics.BigInteger biRhs = rhs;
            System.Numerics.BigInteger biResult = biLhs * biRhs;

            if (biResult <= ulong.MaxValue)
            {
                return (ulong)biResult;
            }

            return (double)biResult;
        }

        internal static object Divide(ulong lhs, ulong rhs)
        {
            // TBD: is it better to cover the special cases explicitly, or
            //      alternatively guard with try/catch?

            if (rhs == 0)
            {
                DivideByZeroException dbze = new DivideByZeroException();
                throw new RuntimeException(dbze.Message, dbze);
            }

            // If the remainder is 0, stay with integer division, otherwise use doubles.
            if ((lhs % rhs) == 0)
            {
                return lhs / rhs;
            }

            return (double)lhs / (double)rhs;
        }

        internal static object Remainder(ulong lhs, ulong rhs)
        {
            // TBD: is it better to cover the special cases explicitly, or
            //      alternatively guard with try/catch?

            if (rhs == 0)
            {
                DivideByZeroException dbze = new DivideByZeroException();
                throw new RuntimeException(dbze.Message, dbze);
            }

            return lhs % rhs;
        }

        internal static object CompareEq(ulong lhs, ulong rhs) { return (lhs == rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareNe(ulong lhs, ulong rhs) { return (lhs != rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareLt(ulong lhs, ulong rhs) { return (lhs < rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareLe(ulong lhs, ulong rhs) { return (lhs <= rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareGt(ulong lhs, ulong rhs) { return (lhs > rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareGe(ulong lhs, ulong rhs) { return (lhs >= rhs) ? Boxed.True : Boxed.False; }
    }

    internal static class DecimalOps
    {
        internal static object Add(decimal lhs, decimal rhs)
        {
            try
            {
                return checked(lhs + rhs);
            }
            catch (OverflowException oe)
            {
                throw new RuntimeException(oe.Message, oe);
            }
        }

        internal static object Sub(decimal lhs, decimal rhs)
        {
            try
            {
                return checked(lhs - rhs);
            }
            catch (OverflowException oe)
            {
                throw new RuntimeException(oe.Message, oe);
            }
        }

        internal static object Multiply(decimal lhs, decimal rhs)
        {
            try
            {
                return checked(lhs * rhs);
            }
            catch (OverflowException oe)
            {
                throw new RuntimeException(oe.Message, oe);
            }
        }

        internal static object Divide(decimal lhs, decimal rhs)
        {
            try
            {
                return checked(lhs / rhs);
            }
            catch (OverflowException oe)
            {
                throw new RuntimeException(oe.Message, oe);
            }
            catch (DivideByZeroException dbze)
            {
                throw new RuntimeException(dbze.Message, dbze);
            }
        }

        internal static object Remainder(decimal lhs, decimal rhs)
        {
            try
            {
                return checked(lhs % rhs);
            }
            catch (OverflowException oe)
            {
                throw new RuntimeException(oe.Message, oe);
            }
            catch (DivideByZeroException dbze)
            {
                throw new RuntimeException(dbze.Message, dbze);
            }
        }

        internal static object BNot(decimal val)
        {
            if (val <= int.MaxValue && val >= int.MinValue)
            {
                return unchecked(~LanguagePrimitives.ConvertTo<int>(val));
            }

            if (val <= uint.MaxValue && val >= uint.MinValue)
            {
                return unchecked(~LanguagePrimitives.ConvertTo<uint>(val));
            }

            if (val <= long.MaxValue && val >= long.MinValue)
            {
                return unchecked(~LanguagePrimitives.ConvertTo<long>(val));
            }

            if (val <= ulong.MaxValue && val >= ulong.MinValue)
            {
                return unchecked(~LanguagePrimitives.ConvertTo<ulong>(val));
            }

            LanguagePrimitives.ThrowInvalidCastException(val, typeof(int));
            Diagnostics.Assert(false, "an exception is raised by LanguagePrimitives.ThrowInvalidCastException.");
            return null;
        }

        internal static object BOr(decimal lhs, decimal rhs)
        {
            ulong l = ConvertToUlong(lhs);
            ulong r = ConvertToUlong(rhs);

            // If either operand is signed, return signed result
            if (lhs < 0 || rhs < 0)
            {
                unchecked
                {
                    return (long)(l | r);
                }
            }

            return l | r;
        }

        internal static object BXor(decimal lhs, decimal rhs)
        {
            ulong l = ConvertToUlong(lhs);
            ulong r = ConvertToUlong(rhs);

            // If either operand is signed, return signed result
            if (lhs < 0 || rhs < 0)
            {
                unchecked
                {
                    return (long)(l ^ r);
                }
            }

            return l ^ r;
        }

        internal static object BAnd(decimal lhs, decimal rhs)
        {
            ulong l = ConvertToUlong(lhs);
            ulong r = ConvertToUlong(rhs);

            // If either operand is signed, return signed result
            if (lhs < 0 || rhs < 0)
            {
                unchecked
                {
                    return (long)(l & r);
                }
            }

            return l & r;
        }

        // This had to be done because if we try to cast a negative decimal number to unsigned, we get an OverFlowException
        // We had to cast them to long (if they are negative) and then promote everything to ULong.
        // While returning the result, we can return either signed or unsigned depending on the input.
        private static ulong ConvertToUlong(decimal val)
        {
            if (val < 0)
            {
                long lValue = LanguagePrimitives.ConvertTo<long>(val);
                return unchecked((ulong)lValue);
            }

            return LanguagePrimitives.ConvertTo<ulong>(val);
        }

        internal static object LeftShift(decimal val, int count)
        {
            if (val <= int.MaxValue && val >= int.MinValue)
            {
                return unchecked(LanguagePrimitives.ConvertTo<int>(val) << count);
            }

            if (val <= uint.MaxValue && val >= uint.MinValue)
            {
                return unchecked(LanguagePrimitives.ConvertTo<uint>(val) << count);
            }

            if (val <= long.MaxValue && val >= long.MinValue)
            {
                return unchecked(LanguagePrimitives.ConvertTo<long>(val) << count);
            }

            if (val <= ulong.MaxValue && val >= ulong.MinValue)
            {
                return unchecked(LanguagePrimitives.ConvertTo<ulong>(val) << count);
            }

            LanguagePrimitives.ThrowInvalidCastException(val, typeof(int));
            Diagnostics.Assert(false, "an exception is raised by LanguagePrimitives.ThrowInvalidCastException.");
            return null;
        }

        internal static object RightShift(decimal val, int count)
        {
            if (val <= int.MaxValue && val >= int.MinValue)
            {
                return unchecked(LanguagePrimitives.ConvertTo<int>(val) >> count);
            }

            if (val <= uint.MaxValue && val >= uint.MinValue)
            {
                return unchecked(LanguagePrimitives.ConvertTo<uint>(val) >> count);
            }

            if (val <= long.MaxValue && val >= long.MinValue)
            {
                return unchecked(LanguagePrimitives.ConvertTo<long>(val) >> count);
            }

            if (val <= ulong.MaxValue && val >= ulong.MinValue)
            {
                return unchecked(LanguagePrimitives.ConvertTo<ulong>(val) >> count);
            }

            LanguagePrimitives.ThrowInvalidCastException(val, typeof(int));
            Diagnostics.Assert(false, "an exception is raised by LanguagePrimitives.ThrowInvalidCastException.");
            return null;
        }

        internal static object CompareEq(decimal lhs, decimal rhs) { return (lhs == rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareNe(decimal lhs, decimal rhs) { return (lhs != rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareLt(decimal lhs, decimal rhs) { return (lhs < rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareLe(decimal lhs, decimal rhs) { return (lhs <= rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareGt(decimal lhs, decimal rhs) { return (lhs > rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareGe(decimal lhs, decimal rhs) { return (lhs >= rhs) ? Boxed.True : Boxed.False; }

        private static object CompareWithDouble(decimal left, double right,
                                                Func<double, double, object> doubleComparer,
                                                Func<decimal, decimal, object> decimalComparer)
        {
            decimal rightAsDecimal;
            try
            {
                rightAsDecimal = (decimal)right;
            }
            catch (OverflowException)
            {
                return doubleComparer((double)left, right);
            }

            return decimalComparer(left, rightAsDecimal);
        }

        private static object CompareWithDouble(double left, decimal right,
                                                Func<double, double, object> doubleComparer,
                                                Func<decimal, decimal, object> decimalComparer)
        {
            decimal leftAsDecimal;
            try
            {
                leftAsDecimal = (decimal)left;
            }
            catch (OverflowException)
            {
                return doubleComparer(left, (double)right);
            }

            return decimalComparer(leftAsDecimal, right);
        }

        internal static object CompareEq1(double lhs, decimal rhs) { return CompareWithDouble(lhs, rhs, DoubleOps.CompareEq, CompareEq); }

        internal static object CompareNe1(double lhs, decimal rhs) { return CompareWithDouble(lhs, rhs, DoubleOps.CompareNe, CompareNe); }

        internal static object CompareLt1(double lhs, decimal rhs) { return CompareWithDouble(lhs, rhs, DoubleOps.CompareLt, CompareLt); }

        internal static object CompareLe1(double lhs, decimal rhs) { return CompareWithDouble(lhs, rhs, DoubleOps.CompareLe, CompareLe); }

        internal static object CompareGt1(double lhs, decimal rhs) { return CompareWithDouble(lhs, rhs, DoubleOps.CompareGt, CompareGt); }

        internal static object CompareGe1(double lhs, decimal rhs) { return CompareWithDouble(lhs, rhs, DoubleOps.CompareGe, CompareGe); }

        internal static object CompareEq2(decimal lhs, double rhs) { return CompareWithDouble(lhs, rhs, DoubleOps.CompareEq, CompareEq); }

        internal static object CompareNe2(decimal lhs, double rhs) { return CompareWithDouble(lhs, rhs, DoubleOps.CompareNe, CompareNe); }

        internal static object CompareLt2(decimal lhs, double rhs) { return CompareWithDouble(lhs, rhs, DoubleOps.CompareLt, CompareLt); }

        internal static object CompareLe2(decimal lhs, double rhs) { return CompareWithDouble(lhs, rhs, DoubleOps.CompareLe, CompareLe); }

        internal static object CompareGt2(decimal lhs, double rhs) { return CompareWithDouble(lhs, rhs, DoubleOps.CompareGt, CompareGt); }

        internal static object CompareGe2(decimal lhs, double rhs) { return CompareWithDouble(lhs, rhs, DoubleOps.CompareGe, CompareGe); }
    }

    internal static class DoubleOps
    {
        internal static object Add(double lhs, double rhs)
        {
            return lhs + rhs;
        }

        internal static object Sub(double lhs, double rhs)
        {
            return lhs - rhs;
        }

        internal static object Multiply(double lhs, double rhs)
        {
            return lhs * rhs;
        }

        internal static object Divide(double lhs, double rhs)
        {
            return lhs / rhs;
        }

        internal static object Remainder(double lhs, double rhs)
        {
            return lhs % rhs;
        }

        internal static object BNot(double val)
        {
            try
            {
                checked
                {
                    if (val <= int.MaxValue && val >= int.MinValue)
                    {
                        return ~LanguagePrimitives.ConvertTo<int>(val);
                    }

                    if (val <= uint.MaxValue && val >= uint.MinValue)
                    {
                        return ~LanguagePrimitives.ConvertTo<uint>(val);
                    }

                    if (val <= long.MaxValue && val >= long.MinValue)
                    {
                        return ~LanguagePrimitives.ConvertTo<long>(val);
                    }

                    if (val <= ulong.MaxValue && val >= ulong.MinValue)
                    {
                        return ~LanguagePrimitives.ConvertTo<ulong>(val);
                    }
                }
            }
            catch (OverflowException)
            {
            }

            LanguagePrimitives.ThrowInvalidCastException(val, typeof(ulong));
            Diagnostics.Assert(false, "an exception is raised by LanguagePrimitives.ThrowInvalidCastException.");
            return null;
        }

        internal static object BOr(double lhs, double rhs)
        {
            ulong l = ConvertToUlong(lhs);
            ulong r = ConvertToUlong(rhs);

            // If either operand is signed, return signed result
            if (lhs < 0 || rhs < 0)
            {
                unchecked
                {
                    return (long)(l | r);
                }
            }

            return l | r;
        }

        internal static object BXor(double lhs, double rhs)
        {
            ulong l = ConvertToUlong(lhs);
            ulong r = ConvertToUlong(rhs);

            // If either operand is signed, return signed result
            if (lhs < 0 || rhs < 0)
            {
                unchecked
                {
                    return (long)(l ^ r);
                }
            }

            return l ^ r;
        }

        internal static object BAnd(double lhs, double rhs)
        {
            ulong l = ConvertToUlong(lhs);
            ulong r = ConvertToUlong(rhs);

            // If either operand is signed, return signed result
            if (lhs < 0 || rhs < 0)
            {
                unchecked
                {
                    return (long)(l & r);
                }
            }

            return l & r;
        }

        // This had to be done because if we try to cast a negative double number to unsigned, we get an OverFlowException
        // We had to cast them to long (if they are negative) and then promote everything to ULong.
        // While returning the result, we can return either signed or unsigned depending on the input.
        private static ulong ConvertToUlong(double val)
        {
            if (val < 0)
            {
                long lValue = LanguagePrimitives.ConvertTo<long>(val);
                return unchecked((ulong)lValue);
            }

            return LanguagePrimitives.ConvertTo<ulong>(val);
        }

        internal static object LeftShift(double val, int count)
        {
            checked
            {
                if (val <= int.MaxValue && val >= int.MinValue)
                {
                    return LanguagePrimitives.ConvertTo<int>(val) << count;
                }

                if (val <= uint.MaxValue && val >= uint.MinValue)
                {
                    return LanguagePrimitives.ConvertTo<uint>(val) << count;
                }

                if (val <= long.MaxValue && val >= long.MinValue)
                {
                    return LanguagePrimitives.ConvertTo<long>(val) << count;
                }

                if (val <= ulong.MaxValue && val >= ulong.MinValue)
                {
                    return LanguagePrimitives.ConvertTo<ulong>(val) << count;
                }
            }

            LanguagePrimitives.ThrowInvalidCastException(val, typeof(ulong));
            Diagnostics.Assert(false, "an exception is raised by LanguagePrimitives.ThrowInvalidCastException.");
            return null;
        }

        internal static object RightShift(double val, int count)
        {
            checked
            {
                if (val <= int.MaxValue && val >= int.MinValue)
                {
                    return LanguagePrimitives.ConvertTo<int>(val) >> count;
                }

                if (val <= uint.MaxValue && val >= uint.MinValue)
                {
                    return LanguagePrimitives.ConvertTo<uint>(val) >> count;
                }

                if (val <= long.MaxValue && val >= long.MinValue)
                {
                    return LanguagePrimitives.ConvertTo<long>(val) >> count;
                }

                if (val <= ulong.MaxValue && val >= ulong.MinValue)
                {
                    return LanguagePrimitives.ConvertTo<ulong>(val) >> count;
                }
            }

            LanguagePrimitives.ThrowInvalidCastException(val, typeof(ulong));
            Diagnostics.Assert(false, "an exception is raised by LanguagePrimitives.ThrowInvalidCastException.");
            return null;
        }

        internal static object CompareEq(double lhs, double rhs) { return (lhs == rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareNe(double lhs, double rhs) { return (lhs != rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareLt(double lhs, double rhs) { return (lhs < rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareLe(double lhs, double rhs) { return (lhs <= rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareGt(double lhs, double rhs) { return (lhs > rhs) ? Boxed.True : Boxed.False; }

        internal static object CompareGe(double lhs, double rhs) { return (lhs >= rhs) ? Boxed.True : Boxed.False; }
    }

    internal static class CharOps
    {
        internal static object CompareStringIeq(char lhs, string rhs)
        {
            if (rhs.Length != 1)
            {
                return Boxed.False;
            }

            return CompareIeq(lhs, rhs[0]);
        }

        internal static object CompareStringIne(char lhs, string rhs)
        {
            if (rhs.Length != 1)
            {
                return Boxed.True;
            }

            return CompareIne(lhs, rhs[0]);
        }

        internal static object CompareIeq(char lhs, char rhs)
        {
            char firstAsUpper = char.ToUpperInvariant(lhs);
            char secondAsUpper = char.ToUpperInvariant(rhs);
            return firstAsUpper == secondAsUpper ? Boxed.True : Boxed.False;
        }

        internal static object CompareIne(char lhs, char rhs)
        {
            char firstAsUpper = char.ToUpperInvariant(lhs);
            char secondAsUpper = char.ToUpperInvariant(rhs);
            return firstAsUpper != secondAsUpper ? Boxed.True : Boxed.False;
        }

        internal static object[] Range(char start, char end)
        {
            int lower = (int)start;
            int upper = (int)end;

            int absRange = Math.Abs(checked(upper - lower));

            object[] ra = new object[absRange + 1];
            if (lower > upper)
            {
                // 3 .. 1 => 3 2 1
                for (int offset = 0; offset < ra.Length; offset++)
                    ra[offset] = (char)lower--;
            }
            else
            {
                // 1 .. 3 => 1 2 3
                for (int offset = 0; offset < ra.Length; offset++)
                    ra[offset] = (char)lower++;
            }

            return ra;
        }
    }
}
