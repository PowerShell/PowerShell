// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

internal static partial class Interop
{
    internal readonly partial struct HRESULT : IEquatable<HRESULT>
    {
        internal readonly int Value;

        internal HRESULT(int value) => Value = value;
                    
        public static implicit operator int(HRESULT value) => value.Value;

        public static explicit operator HRESULT(int value) => new HRESULT(value);
        
        public override bool Equals(object obj) => obj is HRESULT other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();
        
        public bool Failed => Value < 0;

        public bool Succeeded => Value >= 0;
        
        public bool Equals(HRESULT other) => Value == other.Value;
    }
}
