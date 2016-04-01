using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MI_NewTest
{
    using NativeObject;
    public class TestMIProperty
    {
        private MI_Value actualValue;

        public TestMIProperty(MI_Value value, MI_Type type, MI_Flags flags)
        {
            this.actualValue = value;
            this.Type = type;
            this.Flags = flags;
        }

        public object Value { get { return this.actualValue.GetValue(this.Type); } }
        public MI_Type Type { get; private set; }
        public MI_Flags Flags { get; private set;  }
    }
}
