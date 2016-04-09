//
// System.Security.SecureString class
//
// Authors
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Runtime.InteropServices;

namespace System.Security {


    public sealed class SecureString : IDisposable
    {

        private const int BlockSize = 16;
        private const int MaxSize = 65536;

        private int length;
        private bool disposed;
        private bool read_only;
        private byte[] data;

        static SecureString()
        {
            // ProtectedMemory has been moved to System.Security.dll
            // we use reflection to call it (if available) or we'll 
            // throw an exception
        }

        public SecureString()
        {
            Alloc(BlockSize >> 1, false);
        }

        public unsafe SecureString(char* value, int length)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if ((length < 0) || (length > MaxSize))
                throw new ArgumentOutOfRangeException("length", "< 0 || > 65536");

            this.length = length; // real length
            Alloc(length, false);
            int n = 0;
            for (int i = 0; i < length; i++)
            {
                char c = *value++;
                data[n++] = (byte)c;
                data[n++] = (byte)(c >> 8);
            }
            Encrypt();
        }

        // properties

        public int Length
        {
            get
            {
                if (disposed)
                    throw new ObjectDisposedException("SecureString");
                return length;
            }
        }

        
        public void AppendChar(char c)
        {
            if (disposed)
                throw new ObjectDisposedException("SecureString");
            if (read_only)
            {
                throw new InvalidOperationException("SecureString is read-only.");
            }
            if (length == MaxSize)
                throw new ArgumentOutOfRangeException("length", "> 65536");

            try
            {
                Decrypt();
                int n = length * 2;
                Alloc(++length, true);
                data[n++] = (byte)c;
                data[n++] = (byte)(c >> 8);
            }
            finally
            {
                Encrypt();
            }
        }

        public void Clear()
        {
            if (disposed)
                throw new ObjectDisposedException("SecureString");
            if (read_only)
            {
                throw new InvalidOperationException("SecureString is read-only.");
            }

            Array.Clear(data, 0, data.Length);
            length = 0;
        }

        public SecureString Copy()
        {
            SecureString ss = new SecureString();
            ss.data = (byte[])data.Clone();
            ss.length = length;
            return ss;
        }

        public void Dispose()
        {
            disposed = true;
            // don't call clear because we could be either in read-only 
            // or already disposed - but DO CLEAR the data
            if (data != null)
            {
                Array.Clear(data, 0, data.Length);
                data = null;
            }
            length = 0;
        }

        
        public void InsertAt(int index, char c)
        {
            if (disposed)
                throw new ObjectDisposedException("SecureString");
            if (read_only)
            {
                throw new InvalidOperationException("SecureString is read-only.");
            }
            if ((index < 0) || (index > length))
                throw new ArgumentOutOfRangeException("index", "< 0 || > length");
            // insert increments length
            if (length >= MaxSize)
            {
                string msg = $"Maximum string size is '{MaxSize}'.";
                throw new ArgumentOutOfRangeException("index", msg);
            }

            try
            {
                Decrypt();
                Alloc(++length, true);
                int n = index * 2;
                Buffer.BlockCopy(data, n, data, n + 2, data.Length - n - 2);
                data[n++] = (byte)c;
                data[n] = (byte)(c >> 8);
            }
            finally
            {
                Encrypt();
            }
        }

        public bool IsReadOnly()
        {
            if (disposed)
                throw new ObjectDisposedException("SecureString");
            return read_only;
        }

        public void MakeReadOnly()
        {
            read_only = true;
        }

        
        public void RemoveAt(int index)
        {
            if (disposed)
                throw new ObjectDisposedException("SecureString");
            if (read_only)
            {
                throw new InvalidOperationException("SecureString is read-only.");
            }
            if ((index < 0) || (index >= length))
                throw new ArgumentOutOfRangeException("index", "< 0 || > length");

            try
            {
                Decrypt();
                Buffer.BlockCopy(data, index * 2 + 2, data, index * 2, data.Length - index * 2 - 2);
                Alloc(--length, true);
            }
            finally
            {
                Encrypt();
            }
        }

        
        public void SetAt(int index, char c)
        {
            if (disposed)
                throw new ObjectDisposedException("SecureString");
            if (read_only)
            {
                throw new InvalidOperationException("SecureString is read-only.");
            }
            if ((index < 0) || (index >= length))
                throw new ArgumentOutOfRangeException("index", "< 0 || > length");

            try
            {
                Decrypt();
                int n = index * 2;
                data[n++] = (byte)c; 
                data[n] = (byte)(c >> 8);
            }
            finally
            {
                Encrypt();
            }
        }

        // internal/private stuff

        //		[MethodImplAttribute(MethodImplOptions.InternalCall)]
        //		extern static void EncryptInternal (byte [] data, object scope);

        //		[MethodImplAttribute(MethodImplOptions.InternalCall)]
        //		extern static void DecryptInternal (byte [] data, object scope);

        //		static readonly object scope = Enum.Parse (
        //			Assembly.Load (Consts.AssemblySystem_Security)
        //			.GetType ("System.Security.Cryptography.MemoryProtectionScope"), "SameProcess");

        // Note that ProtectedMemory is not supported on non-Windows environment right now.
        private void Encrypt()
        {
            if ((data != null) && (data.Length > 0))
            {
                // It somehow causes nunit test breakage
                // EncryptInternal (data, scope);
            }
        }

        // Note that ProtectedMemory is not supported on non-Windows environment right now.
        private void Decrypt()
        {
            if ((data != null) && (data.Length > 0))
            {
                // It somehow causes nunit test breakage
                // DecryptInternal (data, scope);
            }
        }

        // note: realloc only work for bigger buffers. Clear will 
        // reset buffers to default (and small) size.
        private void Alloc(int length, bool realloc)
        {
            if ((length < 0) || (length > MaxSize))
                throw new ArgumentOutOfRangeException("length", "< 0 || > 65536");

            // (size / blocksize) + 1 * blocksize
            // where size = length * 2 (unicode) and blocksize == 16 (ProtectedMemory)
            // length * 2 (unicode) / 16 (blocksize)
            int size = (length >> 3) + (((length & 0x7) == 0) ? 0 : 1) << 4;

            // is re-allocation necessary ? (i.e. grow or shrink 
            // but do not re-allocate the same amount of memory)
            if (realloc && (data != null) && (size == data.Length))
                return;

            if (realloc)
            {
                // copy, then clear
                byte[] newdata = new byte[size];
                Array.Copy(data, 0, newdata, 0, Math.Min(data.Length, newdata.Length));
                Array.Clear(data, 0, data.Length);
                data = newdata;
            }
            else
            {
                data = new byte[size];
            }
        }

        // dangerous method (put a LinkDemand on it)
        internal byte[] GetBuffer()
        {
            byte[] secret = new byte[length << 1];
            try
            {
                Decrypt();
                Buffer.BlockCopy(data, 0, secret, 0, secret.Length);
            }
            finally
            {
                Encrypt();
            }
            // NOTE: CALLER IS RESPONSIBLE TO ZEROIZE THE DATA
            return secret;
        }

        internal IntPtr ToPtr()
        {
            var ptr = Marshal.AllocHGlobal((this.Length+1) *2 );
            
            for (var i = 0; i < this.Length * 2; i++)
            {
                Marshal.WriteByte(ptr,i, this.data[i]);
            }
            Marshal.WriteByte(ptr,this.Length*2, 0);
            Marshal.WriteByte(ptr,this.Length*2+1, 0);
            return ptr;
        }
    }

    public static class SecureStringMarshal
    {
        public static IntPtr SecureStringToCoTaskMemUnicode(SecureString s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            return s.ToPtr();
        }

        public static void ZeroFreeCoTaskMemUnicode(IntPtr s)
        {
            Marshal.ZeroFreeCoTaskMemUnicode(s);
        }
    }
}