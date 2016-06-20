
using Microsoft.PackageManagement.Provider.Utility;

namespace Microsoft.PackageManagement.NuGetProvider
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Runtime.InteropServices;

    internal static class NuGetPathUtility
    {
        private static readonly char[] _invalidPathChars = Path.GetInvalidPathChars();

        
        internal static bool IsManifest(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return Path.GetExtension(path).Equals(NuGetConstant.ManifestExtension, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool IsPackageFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            return Path.GetExtension(path).Equals(NuGetConstant.PackageExtension, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool ValidateSourceUri(IEnumerable<string> supportedSchemes, Uri srcUri, NuGetRequest request)
        {

            if (!supportedSchemes.Contains(srcUri.Scheme.ToLowerInvariant()))
            {
                return false;
            }

            if (srcUri.IsFile)
            {              
                //validate file source location
                if (Directory.Exists(srcUri.LocalPath))
                {
                    return true;
                }
                return false;
            }

            //validate uri source location
            return ValidateUri(srcUri, request) != null;
        }

        /// <summary>
        /// Returns the validated uri. Returns null if we cannot validate it
        /// </summary>
        /// <param name="query"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        internal static Uri ValidateUri(Uri query, NuGetRequest request)
        {
            var client = request.ClientWithoutAcceptHeader;

            var response = PathUtility.GetHttpResponse(client, query.AbsoluteUri, (() => request.IsCanceled),
                ((msg, num) => request.Verbose(Resources.Messages.RetryingDownload, msg, num)), (msg) => request.Verbose(msg), (msg) => request.Debug(msg));

            if (response == null)
            {
                return null;
            }

            // if response is not success, we need to check for redirection
            if (!response.IsSuccessStatusCode)
            {
                // Check for redirection (http status code 3xx)
                if (response.StatusCode == HttpStatusCode.MultipleChoices || response.StatusCode == HttpStatusCode.MovedPermanently
                    || response.StatusCode == HttpStatusCode.Found || response.StatusCode == HttpStatusCode.SeeOther
                    || response.StatusCode == HttpStatusCode.TemporaryRedirect)
                {
                    // get the redirected direction
                    string location = response.Headers.GetValues("Location").FirstOrDefault();
                    if (String.IsNullOrWhiteSpace(location))
                    {
                        return null;
                    }

                    // make a new query based on location
                    query = new Uri(location);
                }
                else
                {
                    // other status code is wrong
                    return null;
                }
            }
            else
            {
                query = new Uri(response.RequestMessage.RequestUri.AbsoluteUri);
            }

            //Making a query like: www.nuget.org/api/v2/FindPackagesById()?id='FoooBarr' to check the server available. 
            //'FoooBarr' is an any random package id
            string queryUri = "FoooBarr".MakeFindPackageByIdQuery(PathUtility.UriCombine(query.AbsoluteUri, NuGetConstant.FindPackagesById));

            response = PathUtility.GetHttpResponse(client, queryUri, (() => request.IsCanceled),
                ((msg, num) => request.Verbose(Resources.Messages.RetryingDownload, msg, num)), (msg) => request.Verbose(msg), (msg) => request.Debug(msg));


            // The link is not valid
            if (response == null || !response.IsSuccessStatusCode)
            {
                return null;
            }

            return query;
        }

#region CryptProtectData
        //internal struct DATA_BLOB
        //{
        //    public int cbData;
        //    public IntPtr pbData;
        //}

        //internal static void CopyByteToBlob(ref DATA_BLOB blob, byte[] data)
        //{
        //    blob.pbData = Marshal.AllocHGlobal(data.Length);

        //    blob.cbData = data.Length;

        //    Marshal.Copy(data, 0, blob.pbData, data.Length);
        //}

        //internal const uint CRYPTPROTECT_UI_FORBIDDEN = 0x1;

        //[DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        //private static extern bool CryptUnprotectData(ref DATA_BLOB pDataIn, ref string ppszDataDescr, ref DATA_BLOB pOptionalEntropy,
        //    IntPtr pvReserved, IntPtr pPromptStruct, uint dwFlags, ref DATA_BLOB pDataOut);

        //[DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        //private static extern bool CryptProtectData(ref DATA_BLOB pDataIn, string szDataDescr, ref DATA_BLOB pOptionalEntropy,
        //    IntPtr pvReserved, IntPtr pPromptStruct, uint dwFlags, ref DATA_BLOB pDataOut);

        //public static byte[] CryptProtect(byte[] dataIn, byte[] optionalEntropy, bool encryptionOperation)
        //{
        //    DATA_BLOB dataInBlob = new DATA_BLOB();
        //    DATA_BLOB optionalEntropyBlob = new DATA_BLOB();
        //    DATA_BLOB resultBlob = new DATA_BLOB();
        //    string description = String.Empty;

        //    try
        //    {
        //        // copy the encrypted blob
        //        CopyByteToBlob(ref dataInBlob, dataIn);
        //        CopyByteToBlob(ref optionalEntropyBlob, optionalEntropy);

        //        // use local user
        //        uint flags = CRYPTPROTECT_UI_FORBIDDEN;

        //        bool success = false;

        //        // doing decryption
        //        if (!encryptionOperation)
        //        {
        //            // call win32 api
        //            success = CryptUnprotectData(ref dataInBlob, ref description, ref optionalEntropyBlob, IntPtr.Zero, IntPtr.Zero, flags, ref resultBlob);
        //        }
        //        else
        //        {
        //            // doing encryption
        //            success = CryptProtectData(ref dataInBlob, description, ref optionalEntropyBlob, IntPtr.Zero, IntPtr.Zero, flags, ref resultBlob);
        //        }

        //        if (!success)
        //        {
        //            throw new Win32Exception(Marshal.GetLastWin32Error());
        //        }

        //        byte[] unencryptedBytes = new byte[resultBlob.cbData];

        //        Marshal.Copy(resultBlob.pbData, unencryptedBytes, 0, resultBlob.cbData);

        //        return unencryptedBytes;
        //    }
        //    finally
        //    {
        //        // free memory
        //        if (dataInBlob.pbData != IntPtr.Zero)
        //        {
        //            Marshal.FreeHGlobal(dataInBlob.pbData);
        //        }

        //        if (optionalEntropyBlob.pbData != IntPtr.Zero)
        //        {
        //            Marshal.FreeHGlobal(optionalEntropyBlob.pbData);
        //        }

        //        if (resultBlob.pbData != IntPtr.Zero)
        //        {
        //            Marshal.FreeHGlobal(resultBlob.pbData);
        //        }
        //    }
        //}
#endregion
    }
}