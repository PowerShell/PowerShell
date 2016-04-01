using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeObject
{
    [Flags]
    public enum MI_Flags
    {
        None                     = 0,
        /* CIM meta types (or qualifier scopes) */
        MI_FLAG_CLASS            = (1 << 0),
        MI_FLAG_METHOD           = (1 << 1),
        MI_FLAG_PROPERTY         = (1 << 2),
        MI_FLAG_PARAMETER        = (1 << 3),
        MI_FLAG_ASSOCIATION      = (1 << 4),
        MI_FLAG_INDICATION       = (1 << 5),
        MI_FLAG_REFERENCE        = (1 << 6),
        MI_FLAG_ANY              = (1|2|4|8|16|32|64),

        /* Qualifier flavors */
        MI_FLAG_ENABLEOVERRIDE   = (1 << 7),
        MI_FLAG_DISABLEOVERRIDE  = (1 << 8),
        MI_FLAG_RESTRICTED       = (1 << 9),
        MI_FLAG_TOSUBCLASS       = (1 << 10),
        MI_FLAG_TRANSLATABLE     = (1 << 11),

        /* Select boolean qualifier */
        MI_FLAG_KEY              = (1 << 12),
        MI_FLAG_IN               = (1 << 13),
        MI_FLAG_OUT              = (1 << 14),
        MI_FLAG_REQUIRED         = (1 << 15),
        MI_FLAG_STATIC           = (1 << 16),
        MI_FLAG_ABSTRACT         = (1 << 17),
        MI_FLAG_TERMINAL         = (1 << 18),
        MI_FLAG_EXPENSIVE        = (1 << 19),
        MI_FLAG_STREAM           = (1 << 20),
        MI_FLAG_READONLY         = (1 << 21),

        /* Special flags */
        MI_FLAG_EXTENDED         = (1 << 12),
        MI_FLAG_NOT_MODIFIED     = (1 << 25), // indicates that the property is not modified
        MI_FLAG_VERSION          = (1<<26|1<<27|1<<28),
        MI_FLAG_NULL             = (1 << 29),
        MI_FLAG_BORROW           = (1 << 30),
        MI_FLAG_ADOPT	         = (1 << 31)
    }
}
