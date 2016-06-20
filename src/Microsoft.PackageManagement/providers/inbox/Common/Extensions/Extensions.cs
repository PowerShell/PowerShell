namespace Microsoft.PackageManagement.Provider.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    internal static class HttpContentExtensions {

        internal static async Task<long> ReadAsFileAsync(this HttpContent content, string fileName) {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(fileName);
            }

            //Get the absolute path
            var pathName = Path.GetFullPath(fileName);

            using (var fileStream = new FileStream(pathName, FileMode.Create, FileAccess.Write, FileShare.None)) {
                await content.CopyToAsync(fileStream);
                return fileStream.Length;
            }
        }
    }

    internal static class XElementExtensions
    {
        internal static IEnumerable<XElement> ElementsNoNamespace(this XContainer container, string localName)
        {
            return container.Elements().Where(e => e.Name.LocalName == localName);
        }

        /// <summary>
        /// Get the optional attribute value depends on the localName and the nameSpace. Returns null if we can't find it
        /// </summary>
        /// <param name="element"></param>
        /// <param name="localName"></param>
        /// <param name="namespaceName"></param>
        /// <returns></returns>
        internal static string GetOptionalAttributeValue(this XElement element, string localName, string namespaceName = null)
        {
            XAttribute attr;
            // Name space is null so don't use it
            if (String.IsNullOrWhiteSpace(namespaceName))
            {
                attr = element.Attribute(localName);
            }
            else
            {
                attr = element.Attribute(XName.Get(localName, namespaceName));
            }

            return attr != null ? attr.Value : null;
        }
    }

    internal static class VersionExtensions
    {      
        internal static IEnumerable<string> GetComparableVersionStrings(this SemanticVersion version)
        {
            Version coreVersion = version.Version;
            string specialVersion = String.IsNullOrWhiteSpace(version.SpecialVersion) ? String.Empty : "-" + version.SpecialVersion;
            string[] originalVersionComponents = version.GetOriginalVersionComponents();


            if (coreVersion.Revision == 0)
            {
                if (coreVersion.Build == 0)
                {
                    yield return String.Format(
                        CultureInfo.InvariantCulture,
                        "{0}.{1}{2}",
                        originalVersionComponents[0],
                        originalVersionComponents[1],
                        specialVersion);
                }

                yield return String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.{1}.{2}{3}",
                    originalVersionComponents[0],
                    originalVersionComponents[1],
                    originalVersionComponents[2],
                    specialVersion);
            }

            yield return String.Format(
                   CultureInfo.InvariantCulture,
                   "{0}.{1}.{2}.{3}{4}",
                   originalVersionComponents[0],
                   originalVersionComponents[1],
                   originalVersionComponents[2],
                   originalVersionComponents[3],
                   specialVersion);

        }
    }

    internal static class ObjectExtensions
    {
        internal static string ToStringSafe(this object obj)
        {
            return obj == null ? null : obj.ToString();
        }
    }

    internal static class StringExtensions
    {
        internal static string SafeTrim(this string value)
        {
            return value == null ? null : value.Trim();
        }

        internal static string ToBase64(this string text)
        {
            if (text == null)
            {
                return null;
            }
            return Convert.ToBase64String(text.ToByteArray());
        }

        internal static string FromBase64(this string text)
        {
            if (text == null)
            {
                return null;
            }
            return Convert.FromBase64String(text).ToUtf8String();
        }

        internal static string FixVersion(this string versionString)
        {
            if (!String.IsNullOrWhiteSpace(versionString))
            {
                if (versionString[0] == '.')
                {
                    // make sure we have a leading zero when someone says .5
                    versionString = "0" + versionString;
                }

                if (versionString.IndexOf('.') == -1)
                {
                    // make sure we make a 1 work like 1.0
                    versionString = versionString + ".0";
                }
            }

            return versionString;
        }

        // Encodes the string as an array of UTF8 bytes.
        private static byte[] ToByteArray(this string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        // Creates a string from a collection of UTF8 bytes
        private static string ToUtf8String(this IEnumerable<byte> bytes)
        {
            var data = bytes.ToArray();
            try
            {
                return Encoding.UTF8.GetString(data);
            }
            finally
            {
                Array.Clear(data, 0, data.Length);
            }
        }

        public static bool EqualsIgnoreCase(this string str, string str2)
        {
            if (str == null && str2 == null)
            {
                return true;
            }

            if (str == null || str2 == null)
            {
                return false;
            }

            return str.Equals(str2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTrue(this string text)
        {
            return !string.IsNullOrWhiteSpace(text) && text.Equals("true", StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool CompareVersion(this string version1, string version2)
        {
            if(string.IsNullOrWhiteSpace(version1) && string.IsNullOrWhiteSpace(version2))
            {
                return true;
            }
            if (string.IsNullOrWhiteSpace(version1) || string.IsNullOrWhiteSpace(version2))
            {
                return false;
            }

            var semver1 = new SemanticVersion(version1);
            var semver2 = new SemanticVersion(version2);
            return (semver1 == semver2);
        }
    }

    internal static class LinqExtensions
    {
        internal static Dictionary<TKey, TElement> ToDictionaryNicely<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }
            if (keySelector == null)
            {
                throw new ArgumentNullException("keySelector");
            }
            if (elementSelector == null)
            {
                throw new ArgumentNullException("elementSelector");
            }

            var d = new Dictionary<TKey, TElement>(comparer);
            foreach (var element in source)
            {
                d.AddOrSet(keySelector(element), elementSelector(element));
            }
            return d;
        }

        internal static TValue AddOrSet<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            lock (dictionary)
            {
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key] = value;
                }
                else
                {
                    dictionary.Add(key, value);
                }
            }
            return value;
        }      
        
        internal static TSource SafeAggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func)
        {
            var src = source.ToArray();
            if (source != null && src.Any())
            {
                return src.Aggregate(func);
            }
            return default(TSource);
        }
    }
}