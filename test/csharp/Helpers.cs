using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MI_NewTest
{
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Microsoft.Management.Infrastructure;
    using NativeObject;

    public static class Helpers
    {
        private readonly static System.Reflection.BindingFlags PrivateBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        public static Y GetPrivateProperty<X, Y>(this X self, string name)
        {
            object[] emptyArgs = new object[] { };
            var property = typeof(X).GetProperty(name, PrivateBindingFlags);
            return (Y)property.GetMethod.Invoke(self, emptyArgs);
        }

        public static Y GetPrivateVariable<X, Y>(this X self, string name)
        {
            return (Y)typeof(X).GetField(name, PrivateBindingFlags).GetValue(self);
        }

        public static IntPtr Ptr(this CimInstance instance)
        {
            SafeHandle safeHandle = instance.GetPrivateProperty<CimInstance, SafeHandle>("InstanceHandle");
            IntPtr handle = safeHandle.GetPrivateVariable<SafeHandle, IntPtr>("handle");
            return handle;
        }

        public static CimType ToCimType(MI_Type type)
        {
            // CimType has Unknown at position 0 and throws off the list
            return (CimType)((uint)type + 1);
        }
    }
}
