// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Diagnostics.CodeAnalysis;

namespace System.Management.Automation
{
    /// <summary>
    /// <see cref="PSObject"/> implements <see cref="System.IConvertible"/> interface for its base object.
    /// </summary>
    /// <remarks>
    /// If <see cref="PSObject"/> wraps a value type implementing <see cref="System.IConvertible"/>
    /// it provides that functionality too.
    /// </remarks>
    /// <exception cref="System.InvalidCastException">The base object's implementation cannot convert and throws.</exception>
    public partial class PSObject : IConvertible
    {
        /// <summary>
        /// Returns the type code of the base object wrapped by the <see cref="PSObject"/>.
        /// </summary>
        /// <remarks>
        /// If the base object is <see cref="PSObject"/> the method returns <see cref="TypeCode.Object"/>
        /// i.e. it does not implement <see cref="System.IConvertible"/>.
        /// Otherwise, the result is the type code of the base object,
        /// as determined by the base object's implementation of <see cref="System.IConvertible"/>.
        /// </remarks>
        /// <returns>Returns <see cref="System.TypeCode"/> of the PSObject.</returns>
        public TypeCode GetTypeCode()
        {
            object obj = PSObject.Base(this);

            // Take into account PSObject and all derived classes like InternalPSObject.
            return typeof(PSObject).IsAssignableFrom(obj.GetType()) ? TypeCode.Object : Convert.GetTypeCode(obj);
        }

        /// <inheritdoc/>
        public bool ToBoolean(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToBoolean(provider);

        /// <inheritdoc/>
        public char ToChar(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToChar(provider);

        /// <inheritdoc/>
        public sbyte ToSByte(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToSByte(provider);

        /// <inheritdoc/>
        public byte ToByte(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToByte(provider);

        /// <inheritdoc/>
        public short ToInt16(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToInt16(provider);

        /// <inheritdoc/>
        public ushort ToUInt16(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToUInt16(provider);

        /// <inheritdoc/>
        public int ToInt32(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToInt32(provider);

        /// <inheritdoc/>
        public uint ToUInt32(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToUInt32(provider);

        /// <inheritdoc/>
        public long ToInt64(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToInt64(provider);

        /// <inheritdoc/>
        public ulong ToUInt64(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToUInt64(provider);

        /// <inheritdoc/>
        public float ToSingle(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToSingle(provider);

        /// <inheritdoc/>
        public double ToDouble(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToDouble(provider);

        /// <inheritdoc/>
        public decimal ToDecimal(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToDecimal(provider);

        /// <inheritdoc/>
        public DateTime ToDateTime(IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToDateTime(provider);

        /// <inheritdoc/>
        public string ToString(IFormatProvider? provider) => this.ToString(format: null, provider);

        /// <inheritdoc/>
        public object ToType(Type conversionType, IFormatProvider? provider) => GetIConvertibleOrThrow(this).ToType(conversionType, provider);

        private static IConvertible GetIConvertibleOrThrow(PSObject pso)
        {
            object obj = PSObject.Base(pso);
            if (obj is PSObject || obj is not IConvertible value)
            {
                ThrowInvalidCastException();

                // We have to use the workaround
                // because of C# limitations (it doesn't know that ThrowInvalidCastException() is never returned).
                // DoesNotReturn don't address the scenario.
                // See https://github.com/dotnet/runtime/issues/79647#issuecomment-1351370392
                return null!;
            }

            return value;
        }

        [DoesNotReturn]
        private static void ThrowInvalidCastException() => throw new InvalidCastException();
    }
}
