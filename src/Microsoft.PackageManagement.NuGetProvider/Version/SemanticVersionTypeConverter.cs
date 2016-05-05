namespace Microsoft.PackageManagement.NuGetProvider 
{
    using System;
    using System.ComponentModel;
    using System.Globalization;

    /// <summary>
    /// Convert String  to SemanticVersion type
    /// </summary>
    public class SemanticVersionTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var stringValue = value as string;
            SemanticVersion semVer;
            if (stringValue != null && SemanticVersion.TryParse(stringValue, out semVer))
            {
                return semVer;
            }
            return null;
        }
    }
}
