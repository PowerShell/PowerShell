// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace System.Management.Automation
{
    public partial class CommandCompletion
    {
        /// <summary>
        /// PSv2CompletionCompleter implements the algorithm we use to complete cmdlet/file names in PowerShell v2. This class
        /// exists for legacy purpose only. It is used only in a remote interactive session from Win8 to Win7. V3 and forward
        /// uses completely different completers.
        /// </summary>
        /// <remarks>
        /// The implementation of file name completion is completely different on V2 and V3 for remote scenarios. On PSv3, the
        /// CompletionResults are generated always on the target machine, and
        /// </remarks>
        private static class PSv2CompletionCompleter
        {
            private const string CharsRequiringQuotedString = "`&@'#{}()$,;|<> \t";

            private static readonly Regex s_cmdletTabRegex = new Regex(@"^[\w\*\?]+-[\w\*\?]*");

            #region "Handle Command"

            /// <summary>
            /// Used when remoting from a win8 machine to a win7 machine.
            /// </summary>
            /// <param name="lastWord"></param>
            /// <param name="isSnapinSpecified"></param>
            /// <returns></returns>
            private static bool PSv2IsCommandLikeCmdlet(string lastWord, out bool isSnapinSpecified)
            {
                isSnapinSpecified = false;

                string[] cmdletParts = lastWord.Split(Utils.Separators.Backslash);
                if (cmdletParts.Length == 1)
                {
                    return s_cmdletTabRegex.IsMatch(lastWord);
                }

                if (cmdletParts.Length == 2)
                {
                    isSnapinSpecified = PSSnapInInfo.IsPSSnapinIdValid(cmdletParts[0]);
                    if (isSnapinSpecified)
                    {
                        return s_cmdletTabRegex.IsMatch(cmdletParts[1]);
                    }
                }

                return false;
            }

            private readonly struct CommandAndName
            {
                internal readonly PSObject Command;
                internal readonly PSSnapinQualifiedName CommandName;

                internal CommandAndName(PSObject command, PSSnapinQualifiedName commandName)
                {
                    this.Command = command;
                    this.CommandName = commandName;
                }
            }

            /// <summary>
            /// Used when remoting from a win8 machine to a win7 machine. Complete command names.
            /// </summary>
            /// <param name="helper"></param>
            /// <param name="lastWord"></param>
            /// <param name="quote"></param>
            /// <param name="completingAtStartOfLine"></param>
            /// <returns></returns>
            internal static List<CompletionResult> PSv2GenerateMatchSetOfCmdlets(PowerShellExecutionHelper helper, string lastWord, string quote, bool completingAtStartOfLine)
            {
                var results = new List<CompletionResult>();
                bool isSnapinSpecified;

                if (!PSv2IsCommandLikeCmdlet(lastWord, out isSnapinSpecified))
                    return results;

                helper.CurrentPowerShell
                    .AddCommand("Get-Command")
                    .AddParameter("Name", lastWord + "*")
                    .AddCommand("Sort-Object")
                    .AddParameter("Property", "Name");

                Exception exceptionThrown;
                Collection<PSObject> commands = helper.ExecuteCurrentPowerShell(out exceptionThrown);

                if (commands != null && commands.Count > 0)
                {
                    // convert the PSObjects into strings
                    CommandAndName[] cmdlets = new CommandAndName[commands.Count];
                    // if the command causes cmdlets from multiple mshsnapin is returned,
                    // append the mshsnapin name to disambiguate the cmdlets.
                    for (int i = 0; i < commands.Count; ++i)
                    {
                        PSObject command = commands[i];
                        string cmdletFullName = CmdletInfo.GetFullName(command);
                        cmdlets[i] = new CommandAndName(command, PSSnapinQualifiedName.GetInstance(cmdletFullName));
                    }

                    if (isSnapinSpecified)
                    {
                        foreach (CommandAndName cmdlet in cmdlets)
                        {
                            AddCommandResult(cmdlet, true, completingAtStartOfLine, quote, results);
                        }
                    }
                    else
                    {
                        PrependSnapInNameForSameCmdletNames(cmdlets, completingAtStartOfLine, quote, results);
                    }
                }

                return results;
            }

            private static void AddCommandResult(CommandAndName commandAndName, bool useFullName, bool completingAtStartOfLine, string quote, List<CompletionResult> results)
            {
                Diagnostics.Assert(results != null, "Caller needs to make sure the result list is not null");

                string name = useFullName ? commandAndName.CommandName.FullName : commandAndName.CommandName.ShortName;
                string quotedFileName = AddQuoteIfNecessary(name, quote, completingAtStartOfLine);

                var commandType = SafeGetProperty<CommandTypes?>(commandAndName.Command, "CommandType");
                if (commandType == null)
                {
                    return;
                }

                string toolTip;
                string displayName = SafeGetProperty<string>(commandAndName.Command, "Name");

                if (commandType.Value == CommandTypes.Cmdlet || commandType.Value == CommandTypes.Application)
                {
                    toolTip = SafeGetProperty<string>(commandAndName.Command, "Definition");
                }
                else
                {
                    toolTip = displayName;
                }

                results.Add(new CompletionResult(quotedFileName, displayName, CompletionResultType.Command, toolTip));
            }

            private static void PrependSnapInNameForSameCmdletNames(CommandAndName[] cmdlets, bool completingAtStartOfLine, string quote, List<CompletionResult> results)
            {
                Diagnostics.Assert(cmdlets != null && cmdlets.Length > 0,
                    "HasMultiplePSSnapIns must be called with a non-empty collection of PSObject");

                int i = 0;
                bool previousMatched = false;
                while (true)
                {
                    CommandAndName commandAndName = cmdlets[i];

                    int lookAhead = i + 1;
                    if (lookAhead >= cmdlets.Length)
                    {
                        AddCommandResult(commandAndName, previousMatched, completingAtStartOfLine, quote, results);
                        break;
                    }

                    CommandAndName nextCommandAndName = cmdlets[lookAhead];

                    if (string.Equals(
                            commandAndName.CommandName.ShortName,
                            nextCommandAndName.CommandName.ShortName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        AddCommandResult(commandAndName, true, completingAtStartOfLine, quote, results);
                        previousMatched = true;
                    }
                    else
                    {
                        AddCommandResult(commandAndName, previousMatched, completingAtStartOfLine, quote, results);
                        previousMatched = false;
                    }

                    i++;
                }
            }

            #endregion "Handle Command"

            #region "Handle File Names"

            internal static List<CompletionResult> PSv2GenerateMatchSetOfFiles(PowerShellExecutionHelper helper, string lastWord, bool completingAtStartOfLine, string quote)
            {
                var results = new List<CompletionResult>();

                // lastWord is treated as an PSPath. The match set includes those items that match that
                // path, namely, the union of:
                //  (S1) the sorted set of items matching the last word
                //  (S2) the sorted set of items matching the last word + *
                // If the last word contains no wildcard characters, then S1 is the empty set.  S1 is always
                // a subset of S2, but we want to present S1 first, then (S2 - S1) next.  The idea is that
                // if the user typed some wildcard characters, they'd prefer to see those matches before
                // all of the rest.

                // Determine if we need to quote the paths we parse

                lastWord ??= string.Empty;
                bool isLastWordEmpty = string.IsNullOrEmpty(lastWord);
                bool lastCharIsStar = !isLastWordEmpty && lastWord.EndsWith('*');
                bool containsGlobChars = WildcardPattern.ContainsWildcardCharacters(lastWord);

                string wildWord = lastWord + "*";
                bool shouldFullyQualifyPaths = PSv2ShouldFullyQualifyPathsPath(helper, lastWord);

                // NTRAID#Windows Out Of Band Releases-927933-2006/03/13-JeffJon
                // Need to detect when the path is a provider-direct path and make sure
                // to remove the provider-qualifier when the resolved path is returned.
                bool isProviderDirectPath = lastWord.StartsWith(@"\\", StringComparison.Ordinal) ||
                                            lastWord.StartsWith("//", StringComparison.Ordinal);

                List<PathItemAndConvertedPath> s1 = null;
                List<PathItemAndConvertedPath> s2 = null;

                if (containsGlobChars && !isLastWordEmpty)
                {
                    s1 = PSv2FindMatches(
                            helper,
                            lastWord,
                            shouldFullyQualifyPaths);
                }

                if (!lastCharIsStar)
                {
                    s2 = PSv2FindMatches(
                            helper,
                            wildWord,
                            shouldFullyQualifyPaths);
                }

                IEnumerable<PathItemAndConvertedPath> combinedMatches = CombineMatchSets(s1, s2);

                if (combinedMatches != null)
                {
                    foreach (var combinedMatch in combinedMatches)
                    {
                        string combinedMatchPath = WildcardPattern.Escape(combinedMatch.Path);
                        string combinedMatchConvertedPath = WildcardPattern.Escape(combinedMatch.ConvertedPath);
                        string completionText = isProviderDirectPath ? combinedMatchConvertedPath : combinedMatchPath;

                        completionText = AddQuoteIfNecessary(completionText, quote, completingAtStartOfLine);

                        bool? isContainer = SafeGetProperty<bool?>(combinedMatch.Item, "PSIsContainer");
                        string childName = SafeGetProperty<string>(combinedMatch.Item, "PSChildName");
                        string toolTip = PowerShellExecutionHelper.SafeToString(combinedMatch.ConvertedPath);

                        if (isContainer != null && childName != null && toolTip != null)
                        {
                            CompletionResultType resultType = isContainer.Value
                                                                  ? CompletionResultType.ProviderContainer
                                                                  : CompletionResultType.ProviderItem;
                            results.Add(new CompletionResult(completionText, childName, resultType, toolTip));
                        }
                    }
                }

                return results;
            }

            private static string AddQuoteIfNecessary(string completionText, string quote, bool completingAtStartOfLine)
            {
                if (completionText.AsSpan().IndexOfAny(CharsRequiringQuotedString) >= 0)
                {
                    bool needAmpersand = quote.Length == 0 && completingAtStartOfLine;
                    string quoteInUse = quote.Length == 0 ? "'" : quote;
                    completionText = quoteInUse == "'" ? completionText.Replace("'", "''") : completionText;
                    completionText = quoteInUse + completionText + quoteInUse;
                    completionText = needAmpersand ? "& " + completionText : completionText;
                }
                else
                {
                    completionText = quote + completionText + quote;
                }

                return completionText;
            }

            private static IEnumerable<PathItemAndConvertedPath> CombineMatchSets(List<PathItemAndConvertedPath> s1, List<PathItemAndConvertedPath> s2)
            {
                if (s1 == null || s1.Count < 1)
                {
                    // only s2 contains results; which may be null or empty
                    return s2;
                }

                if (s2 == null || s2.Count < 1)
                {
                    // only s1 contains results
                    return s1;
                }

                // s1 and s2 contain results
                Diagnostics.Assert(s1 != null && s1.Count > 0, "s1 should have results");
                Diagnostics.Assert(s2 != null && s2.Count > 0, "if s1 has results, s2 must also");
                Diagnostics.Assert(s1.Count <= s2.Count, "s2 should always be larger than s1");

                var result = new List<PathItemAndConvertedPath>();

                // we need to remove from s2 those items in s1.  Since the results from FindMatches will be sorted,
                // just copy out the unique elements from s2 and s1.  We know that every element of S1 will be in S2,
                // so the result set will be S1 + (S2 - S1), which is the same size as S2.
                result.AddRange(s1);
                for (int i = 0, j = 0; i < s2.Count; ++i)
                {
                    if (j < s1.Count && string.Equals(s2[i].Path, s1[j].Path, StringComparison.CurrentCultureIgnoreCase))
                    {
                        ++j;
                        continue;
                    }

                    result.Add(s2[i]);
                }

#if DEBUG
                Diagnostics.Assert(result.Count == s2.Count, "result should be the same size as s2, see the size comment above");
                for (int i = 0; i < s1.Count; ++i)
                {
                    string path = result[i].Path;
                    int j = result.FindLastIndex(item => item.Path == path);
                    Diagnostics.Assert(j == i, "elements of s1 should only come at the start of the results");
                }
#endif
                return result;
            }

            private static T SafeGetProperty<T>(PSObject psObject, string propertyName)
            {
                if (psObject == null)
                {
                    return default(T);
                }

                PSPropertyInfo property = psObject.Properties[propertyName];
                if (property == null)
                {
                    return default(T);
                }

                object propertyValue = property.Value;
                if (propertyValue == null)
                {
                    return default(T);
                }

                T returnValue;
                if (LanguagePrimitives.TryConvertTo(propertyValue, out returnValue))
                {
                    return returnValue;
                }

                return default(T);
            }

            private static bool PSv2ShouldFullyQualifyPathsPath(PowerShellExecutionHelper helper, string lastWord)
            {
                // These are special cases, as they represent cases where the user expects to
                // see the full path.
                if (lastWord.StartsWith('~') ||
                    lastWord.StartsWith('\\') ||
                    lastWord.StartsWith('/'))
                {
                    return true;
                }

                helper.CurrentPowerShell
                    .AddCommand("Split-Path")
                    .AddParameter("Path", lastWord)
                    .AddParameter("IsAbsolute", true);

                bool isAbsolute = helper.ExecuteCommandAndGetResultAsBool();
                return isAbsolute;
            }

            private readonly struct PathItemAndConvertedPath
            {
                internal readonly string Path;
                internal readonly PSObject Item;
                internal readonly string ConvertedPath;

                internal PathItemAndConvertedPath(string path, PSObject item, string convertedPath)
                {
                    this.Path = path;
                    this.Item = item;
                    this.ConvertedPath = convertedPath;
                }
            }

            private static List<PathItemAndConvertedPath> PSv2FindMatches(PowerShellExecutionHelper helper, string path, bool shouldFullyQualifyPaths)
            {
                Diagnostics.Assert(!string.IsNullOrEmpty(path), "path should have a value");
                var result = new List<PathItemAndConvertedPath>();

                Exception exceptionThrown;
                PowerShell powershell = helper.CurrentPowerShell;

                // It's OK to use script, since tab completion is useless when the remote Win7 machine is in nolanguage mode
                if (!shouldFullyQualifyPaths)
                {
                    powershell.AddScript(string.Format(
                        CultureInfo.InvariantCulture,
                        "& {{ trap {{ continue }} ; resolve-path {0} -Relative -WarningAction SilentlyContinue | ForEach-Object {{,($_,(get-item $_ -WarningAction SilentlyContinue),(convert-path $_ -WarningAction SilentlyContinue))}} }}",
                        path));
                }
                else
                {
                    powershell.AddScript(string.Format(
                        CultureInfo.InvariantCulture,
                        "& {{ trap {{ continue }} ; resolve-path {0} -WarningAction SilentlyContinue | ForEach-Object {{,($_,(get-item $_ -WarningAction SilentlyContinue),(convert-path $_ -WarningAction SilentlyContinue))}} }}",
                        path));
                }

                Collection<PSObject> paths = helper.ExecuteCurrentPowerShell(out exceptionThrown);
                if (paths == null || paths.Count == 0)
                {
                    return null;
                }

                foreach (PSObject t in paths)
                {
                    var pathsArray = t.BaseObject as IList;
                    if (pathsArray != null && pathsArray.Count == 3)
                    {
                        object objectPath = pathsArray[0];
                        PSObject item = pathsArray[1] as PSObject;
                        object convertedPath = pathsArray[1];

                        if (objectPath == null || item == null || convertedPath == null)
                        {
                            continue;
                        }

                        result.Add(new PathItemAndConvertedPath(
                                            PowerShellExecutionHelper.SafeToString(objectPath),
                                            item,
                                            PowerShellExecutionHelper.SafeToString(convertedPath)));
                    }
                }

                if (result.Count == 0)
                {
                    return null;
                }

                result.Sort((PathItemAndConvertedPath x, PathItemAndConvertedPath y) =>
                {
                    Diagnostics.Assert(x.Path != null && y.Path != null, "SafeToString always returns a non-null string");
                    return string.Compare(x.Path, y.Path, StringComparison.CurrentCultureIgnoreCase);
                });

                return result;
            }

            #endregion "Handle File Names"
        }
    }
}
