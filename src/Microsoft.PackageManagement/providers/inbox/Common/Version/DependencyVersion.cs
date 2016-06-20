using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Microsoft.PackageManagement.Provider.Utility
{
    /// <summary>
    /// Represents dependency version returned by nuget api call
    /// </summary>
    public class DependencyVersion
    {
        public SemanticVersion MinVersion { get; set; }
        /// <summary>
        /// True if the version we are looking for includes min version
        /// </summary>

        public bool IsMinInclusive { get; set; }
        public SemanticVersion MaxVersion { get; set; }
        /// <summary>
        /// True if the version we are looking for includes the max version
        /// </summary>
        public bool IsMaxInclusive { get; set; }

        /// <summary>
        /// Parse and return a dependency version
        /// The version string is either a simple version or an arithmetic range
        /// e.g.
        ///      1.0         --> 1.0 ≤ x
        ///      (,1.0]      --> x ≤ 1.0
        ///      (,1.0)      --> x lt 1.0
        ///      [1.0]       --> x == 1.0
        ///      (1.0,)      --> 1.0 lt x
        ///      (1.0, 2.0)   --> 1.0 lt x lt 2.0
        ///      [1.0, 2.0]   --> 1.0 ≤ x ≤ 2.0
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static DependencyVersion ParseDependencyVersion(string value)
        {
            var depVers = new DependencyVersion();

            if (String.IsNullOrWhiteSpace(value))
            {
                return depVers;
            }

            value = value.Trim();

            char first = value.First();
            char last = value.Last();

            if (first != '(' && first != '[' && last != ']' && last != ')')
            {
                // Stand alone version
                depVers.IsMinInclusive = true;
                depVers.MinVersion = new SemanticVersion(value);
                return depVers;
            }

            // value must have length greater than 3
            if (value.Length < 3)
            {
                return depVers;
            }

            // The first character must be [ or (
            switch (value.First())
            {
                case '[':
                    depVers.IsMinInclusive = true;
                    break;
                case '(':
                    depVers.IsMinInclusive = false;
                    break;
                default:
                    // If not, return without setting anything
                    return depVers;
            }

            // The last character must be ] or )
            switch (value.Last())
            {
                case ']':
                    depVers.IsMaxInclusive = true;
                    break;
                case ')':
                    depVers.IsMaxInclusive = false;
                    break;
                default:
                    // If not, return without setting anything
                    return depVers;
            }

            // Get rid of the two brackets
            value = value.Substring(1, value.Length - 2);

            // Split by comma, and make sure we don't get more than two pieces
            string[] parts = value.Split(',');

            // Wrong format if we have more than 2 parts or all the parts are empty
            if (parts.Length > 2 || parts.All(String.IsNullOrEmpty))
            {
                return depVers;
            }

            // First part is min
            string minVersionString = parts[0];

            // If there is only 1 part then first part will also be max
            string maxVersionString = (parts.Length == 2) ? parts[1] : parts[0];

            // Get min version if we have it
            if (!String.IsNullOrWhiteSpace(minVersionString))
            {
                depVers.MinVersion = new SemanticVersion(minVersionString);
            }

            // Get max version if we have it
            if (!String.IsNullOrWhiteSpace(maxVersionString))
            {
                depVers.MaxVersion = new SemanticVersion(maxVersionString);
            }

            return depVers;
        }

        public override string ToString()
        {
            // Returns nothing if no min or max
            if (MinVersion == null && MaxVersion == null)
            {
                return null;
            }

            // If we have min and minInclusive but no max, then return min string
            if (MinVersion != null && IsMinInclusive && MaxVersion == null && !IsMaxInclusive)
            {
                return MinVersion.ToString();
            }

            // MinVersion and MaxVersion is the same and both inclusives then return the value
            if (MinVersion == MaxVersion && IsMinInclusive && IsMaxInclusive)
            {
                return String.Format(CultureInfo.InvariantCulture, "[{0}]", MinVersion);
            }

            char lhs = IsMinInclusive ? '[' : '(';
            char rhs = IsMaxInclusive ? ']' : ')';

            return String.Format(CultureInfo.InvariantCulture, "{0}{1}, {2}{3}", lhs, MinVersion, MaxVersion, rhs);
        }
    
    }
}
