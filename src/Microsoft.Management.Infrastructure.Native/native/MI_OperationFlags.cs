using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeObject
{
    /* Flags to be passed into Session operation functions.
     * One item from each group can be bit-wise combined.
     */
    [Flags]
    public enum MI_OperationFlags
    {
        Default                                             = 0x0000,
        MI_OPERATIONFLAGS_AUTOMATIC_ACK_RESULTS             = 0x0000,
        MI_OPERATIONFLAGS_MANUAL_ACK_RESULTS                = 0x0001,

        /* RTTI bitmasks in order to specify what options are acceptable to the client */
        MI_OPERATIONFLAGS_NO_RTTI                           = 0x0400, /* All instance elements are string except embedded objects and references, but their elements will be strings also */
        MI_OPERATIONFLAGS_BASIC_RTTI                        = 0x0002, /* All instance elements may be strings or the correct type */
        MI_OPERATIONFLAGS_STANDARD_RTTI                     = 0x0800, /* All instance elements are of the correct type, but some hierarchy information may be lost due to optimizations */
        MI_OPERATIONFLAGS_FULL_RTTI                         = 0x0004, /* All instance elements at every level of the instances class hierarchy will be accurate.  This may be very expensive */

        /* If no RTTI flag is specified (i.e. 0 for relevant RTTI bits) then the protocol handler itself will pick the best option */
        MI_OPERATIONFLAGS_DEFAULT_RTTI                      = 0x0000,

        MI_OPERATIONFLAGS_NON_LOCALIZED_QUALIFIERS          = 0x0000,
        MI_OPERATIONFLAGS_LOCALIZED_QUALIFIERS              = 0x0008,

        MI_OPERATIONFLAGS_NON_EXPENSIVE_PROPERTIES_ONLY     = 0x0000,
        MI_OPERATIONFLAGS_EXPENSIVE_PROPERTIES              = 0x0040,

        MI_OPERATIONFLAGS_POLYMORPHISM_DEEP                 = 0x0000,
        MI_OPERATIONFLAGS_POLYMORPHISM_SHALLOW              = 0x0080,
        MI_OPERATIONFLAGS_POLYMORPHISM_DEEP_BASE_PROPS_ONLY = 0x0180,

        /* Report an empty result when the operation has successfully started.
         * The first result may have a NULL instance/class/indication, with
         * moreResults set to MI_TRUE.  If a result is delivered very quickly
         * the actual result will be delivered instead.
         * Not all operations or protocol handlers can achieve this.
         */
        MI_OPERATIONFLAGS_REPORT_OPERATION_STARTED          = 0x0200
    }
}
