using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NativeObject
{
    [Flags]
    public enum MI_ModuleFlags
    {
        /** Whether standard qualifiers were generated */
        MI_MODULE_FLAG_STANDARD_QUALIFIERS = (1 << 0),

        /** Whether description qualifiers were generated */
        MI_MODULE_FLAG_DESCRIPTIONS = (1 << 1),

        /** Whether Values and ValueMap qualifiers were generated */
        MI_MODULE_FLAG_VALUES = (1 << 2),

        /** Whether the MappingStrings qualifiers were generated */
        MI_MODULE_FLAG_MAPPING_STRINGS = (1 << 3),

        /** Whether the boolean qualifiers were generated */
        MI_MODULE_FLAG_BOOLEANS = (1 << 4),

        /** Whether C++ extensions were generated */
        MI_MODULE_FLAG_CPLUSPLUS = (1 << 5),

        /** Whether translatable qualifiers were localized = (and STRING.RC generated), */
        MI_MODULE_FLAG_LOCALIZED = (1 << 6),

        /** Whether filters are supported */
        MI_MODULE_FLAG_FILTER_SUPPORT = (1 << 7)
    }
}
