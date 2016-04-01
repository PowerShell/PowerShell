using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Globalization;
using NativeObject;

namespace Microsoft.Management.Infrastructure.Native
{
    internal class SubscriptionDeliveryOptionsMethods
    {
        // Methods
        private SubscriptionDeliveryOptionsMethods()
        {
            throw new NotImplementedException();
        }
        internal static MiResult AddCredentials(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, NativeCimCredentialHandle credentials, uint flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult Clone(SubscriptionDeliveryOptionsHandle subscriptionDeliveryOptionsHandle, out SubscriptionDeliveryOptionsHandle newSubscriptionDeliveryOptionsHandle)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetDateTime(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, object value, uint flags)
        {
            throw new NotImplementedException();
        }
        //internal static MiResult SetInterval(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, ValueType modopt(TimeSpan) modopt(IsBoxed) value, uint flags)
        internal static MiResult SetInterval(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, ValueType value, uint flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetNumber(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, uint value, uint flags)
        {
            throw new NotImplementedException();
        }
        internal static MiResult SetString(SubscriptionDeliveryOptionsHandle OptionsHandle, string optionName, string value, uint flags)
        {
            throw new NotImplementedException();
        }
    }
}
