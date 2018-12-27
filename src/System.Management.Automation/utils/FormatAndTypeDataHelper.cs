// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Management.Automation.Host;
using System.Collections.Concurrent;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Runspaces
{
    internal class PSSnapInTypeAndFormatErrors
    {
        public string psSnapinName;
        // only one of fullPath or formatTable or typeData or typeDefinition should be specified..
        // typeData and isRemove should be used together

        internal PSSnapInTypeAndFormatErrors(string psSnapinName, string fullPath)
        {
            this.psSnapinName = psSnapinName;
            FullPath = fullPath;
            Errors = new ConcurrentBag<string>();
        }

        internal PSSnapInTypeAndFormatErrors(string psSnapinName, FormatTable formatTable)
        {
            this.psSnapinName = psSnapinName;
            FormatTable = formatTable;
            Errors = new ConcurrentBag<string>();
        }

        internal PSSnapInTypeAndFormatErrors(string psSnapinName, TypeData typeData, bool isRemove)
        {
            this.psSnapinName = psSnapinName;
            TypeData = typeData;
            IsRemove = isRemove;
            Errors = new ConcurrentBag<string>();
        }

        internal PSSnapInTypeAndFormatErrors(string psSnapinName, ExtendedTypeDefinition typeDefinition)
        {
            this.psSnapinName = psSnapinName;
            FormatData = typeDefinition;
            Errors = new ConcurrentBag<string>();
        }

        internal ExtendedTypeDefinition FormatData { get; }

        internal TypeData TypeData { get; }

        internal bool IsRemove { get; }

        internal string FullPath { get; }

        internal FormatTable FormatTable { get; }

        internal ConcurrentBag<string> Errors { get; set; }

        internal string PSSnapinName { get { return psSnapinName; } }

        internal bool FailToLoadFile;
    }

    internal static class FormatAndTypeDataHelper
    {
        private const string FileNotFound = "FileNotFound";
        private const string CannotFindRegistryKey = "CannotFindRegistryKey";
        private const string CannotFindRegistryKeyPath = "CannotFindRegistryKeyPath";
        private const string EntryShouldBeMshXml = "EntryShouldBeMshXml";
        private const string DuplicateFile = "DuplicateFile";
        internal const string ValidationException = "ValidationException";

        private static string GetBaseFolder(Collection<string> independentErrors)
        {
            return Path.GetDirectoryName(PsUtils.GetMainModule(System.Diagnostics.Process.GetCurrentProcess()).FileName);
        }

        private static string GetAndCheckFullFileName(
            string psSnapinName,
            HashSet<string> fullFileNameSet,
            string baseFolder,
            string baseFileName,
            Collection<string> independentErrors,
            ref bool needToRemoveEntry,
            bool checkFileExists)
        {
            string retValue = Path.IsPathRooted(baseFileName) ? baseFileName : Path.Combine(baseFolder, baseFileName);

            if (checkFileExists && !File.Exists(retValue))
            {
                string error = StringUtil.Format(TypesXmlStrings.FileNotFound, psSnapinName, retValue);
                independentErrors.Add(error);
                return null;
            }

            if (fullFileNameSet.Contains(retValue))
            {
                // Do not add Errors as we want loading of type/format files to be idempotent.
                // Just mark as Duplicate so the duplicate entry gets removed
                needToRemoveEntry = true;
                return null;
            }

            if (!retValue.EndsWith(".ps1xml", StringComparison.OrdinalIgnoreCase))
            {
                string error = StringUtil.Format(TypesXmlStrings.EntryShouldBeMshXml, psSnapinName, retValue);
                independentErrors.Add(error);
                return null;
            }

            fullFileNameSet.Add(retValue);
            return retValue;
        }

        internal static void ThrowExceptionOnError(
            string errorId,
            Collection<string> independentErrors,
            Collection<PSSnapInTypeAndFormatErrors> PSSnapinFilesCollection,
            Category category)
        {
            Collection<string> errors = new Collection<string>();
            if (independentErrors != null)
            {
                foreach (string error in independentErrors)
                {
                    errors.Add(error);
                }
            }

            foreach (PSSnapInTypeAndFormatErrors PSSnapinFiles in PSSnapinFilesCollection)
            {
                foreach (string error in PSSnapinFiles.Errors)
                {
                    errors.Add(error);
                }
            }

            if (errors.Count == 0)
            {
                return;
            }

            StringBuilder allErrors = new StringBuilder();

            allErrors.Append('\n');
            foreach (string error in errors)
            {
                allErrors.Append(error);
                allErrors.Append('\n');
            }

            string message = string.Empty;
            if (category == Category.Types)
            {
                message =
                    StringUtil.Format(ExtendedTypeSystem.TypesXmlError, allErrors.ToString());
            }
            else if (category == Category.Formats)
            {
                message = StringUtil.Format(FormatAndOutXmlLoadingStrings.FormatLoadingErrors, allErrors.ToString());
            }

            RuntimeException ex = new RuntimeException(message);
            ex.SetErrorId(errorId);
            throw ex;
        }

        internal static void ThrowExceptionOnError(
            string errorId,
            ConcurrentBag<string> errors,
            Category category)
        {
            if (errors.Count == 0)
            {
                return;
            }

            StringBuilder allErrors = new StringBuilder();

            allErrors.Append('\n');
            foreach (string error in errors)
            {
                allErrors.Append(error);
                allErrors.Append('\n');
            }

            string message = string.Empty;
            if (category == Category.Types)
            {
                message =
                    StringUtil.Format(ExtendedTypeSystem.TypesXmlError, allErrors.ToString());
            }
            else if (category == Category.Formats)
            {
                message = StringUtil.Format(FormatAndOutXmlLoadingStrings.FormatLoadingErrors, allErrors.ToString());
            }

            RuntimeException ex = new RuntimeException(message);
            ex.SetErrorId(errorId);
            throw ex;
        }

        internal enum Category
        {
            Types,
            Formats,
        }
    }
}

