using Microsoft.PackageManagement.Provider.Utility;

namespace Microsoft.PackageManagement.NuGetProvider
{
    using System;
    using System.Globalization;
    
    internal static class StringExtensions
    {

        internal static string MakeFindPackageByIdQuery(this string packageId, string urlFormat)
        {
            return String.Format(CultureInfo.InvariantCulture, urlFormat, packageId);
        }

        internal static string MakeSearchQuery(this string searchTerm, string baseUrl, bool allowPrereleaseVersions, bool allVersions)
        {
            // Fill in the searchTerm, targetFrameworks, and allowPrereleaseVersions fileds in the query format string.
            // Skip and Take will be filled later on
            var newSearchTerm = string.Format(CultureInfo.InvariantCulture, NuGetConstant.SearchTerm, searchTerm, allowPrereleaseVersions ? "true" : "false");

            // Make the uri query string
            return PathUtility.UriCombine(baseUrl, String.Concat((allVersions ? NuGetConstant.SearchFilterAllVersions : NuGetConstant.SearchFilter), newSearchTerm));
        }

        internal static string InsertSkipAndTop(this string query)
        {
            return String.Concat(query, NuGetConstant.SkipAndTop);
        }
    }
    internal static class ExceptionExtensions
    {
        internal static void Dump(this Exception ex, NuGetRequest request)
        {
            request.Debug(ex.ToString());
        }
    }

}