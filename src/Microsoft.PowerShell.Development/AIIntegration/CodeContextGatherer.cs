// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.PowerShell.Development.AIIntegration
{
    /// <summary>
    /// Represents code context gathered for AI analysis.
    /// </summary>
    public class CodeContext
    {
        public string RootPath { get; set; }
        public List<CodeFile> Files { get; set; }
        public Dictionary<string, string> Metadata { get; set; }
        public int TotalLines { get; set; }
        public long TotalSize { get; set; }
        public DateTime GatheredAt { get; set; }

        public CodeContext()
        {
            Files = new List<CodeFile>();
            Metadata = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Represents a code file with its content.
    /// </summary>
    public class CodeFile
    {
        public string Path { get; set; }
        public string RelativePath { get; set; }
        public string Language { get; set; }
        public string Content { get; set; }
        public int LineCount { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
        public List<string> Dependencies { get; set; }
        public Dictionary<string, object> Metrics { get; set; }

        public CodeFile()
        {
            Dependencies = new List<string>();
            Metrics = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Gathers code context for AI analysis.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "CodeContext")]
    [OutputType(typeof(CodeContext))]
    [Alias("gcc", "context")]
    public sealed class GetCodeContextCommand : PSCmdlet
    {
        /// <summary>
        /// Path to gather context from.
        /// </summary>
        [Parameter(Position = 0)]
        public string Path { get; set; }

        /// <summary>
        /// Include recently modified files.
        /// </summary>
        [Parameter]
        public SwitchParameter RecentlyModified { get; set; }

        /// <summary>
        /// Number of hours to consider for recently modified.
        /// </summary>
        [Parameter]
        [ValidateRange(1, 720)]
        public int Hours { get; set; } = 24;

        /// <summary>
        /// File patterns to include (e.g., "*.cs", "*.js").
        /// </summary>
        [Parameter]
        public string[] Include { get; set; }

        /// <summary>
        /// File patterns to exclude.
        /// </summary>
        [Parameter]
        public string[] Exclude { get; set; }

        /// <summary>
        /// Maximum file size in KB.
        /// </summary>
        [Parameter]
        [ValidateRange(1, 10240)]
        public int MaxFileSizeKB { get; set; } = 500;

        /// <summary>
        /// Maximum number of files to include.
        /// </summary>
        [Parameter]
        [ValidateRange(1, 1000)]
        public int MaxFiles { get; set; } = 50;

        /// <summary>
        /// Include file content.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeContent { get; set; }

        /// <summary>
        /// Include code metrics.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeMetrics { get; set; }

        /// <summary>
        /// Include dependencies.
        /// </summary>
        [Parameter]
        public SwitchParameter IncludeDependencies { get; set; }

        /// <summary>
        /// Specific files to include.
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public string[] Files { get; set; }

        private string _rootPath;
        private HashSet<string> _excludePatterns;
        private DateTime _cutoffTime;

        protected override void BeginProcessing()
        {
            _rootPath = string.IsNullOrEmpty(Path)
                ? SessionState.Path.CurrentFileSystemLocation.Path
                : SessionState.Path.GetUnresolvedProviderPathFromPSPath(Path);

            if (!Directory.Exists(_rootPath))
            {
                throw new DirectoryNotFoundException($"Path not found: {_rootPath}");
            }

            // Default exclude patterns
            _excludePatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "node_modules", "bin", "obj", "target", "dist", "build",
                ".git", ".svn", ".hg",
                "packages", "vendor",
                "__pycache__", ".pytest_cache",
                ".vs", ".vscode", ".idea"
            };

            if (Exclude != null)
            {
                foreach (var pattern in Exclude)
                {
                    _excludePatterns.Add(pattern);
                }
            }

            _cutoffTime = DateTime.Now.AddHours(-Hours);
        }

        protected override void ProcessRecord()
        {
            var context = new CodeContext
            {
                RootPath = _rootPath,
                GatheredAt = DateTime.Now
            };

            List<FileInfo> filesToProcess;

            if (Files != null && Files.Length > 0)
            {
                // Specific files provided
                filesToProcess = Files
                    .Select(f => new FileInfo(SessionState.Path.GetUnresolvedProviderPathFromPSPath(f)))
                    .Where(f => f.Exists)
                    .ToList();
            }
            else
            {
                // Discover files
                filesToProcess = DiscoverFiles();
            }

            // Process files
            int count = 0;
            foreach (var fileInfo in filesToProcess)
            {
                if (count >= MaxFiles)
                {
                    WriteWarning($"Reached maximum file limit ({MaxFiles}). Use -MaxFiles to increase.");
                    break;
                }

                if (fileInfo.Length > MaxFileSizeKB * 1024)
                {
                    WriteVerbose($"Skipping large file: {fileInfo.Name} ({fileInfo.Length / 1024}KB)");
                    continue;
                }

                var codeFile = ProcessFile(fileInfo);
                if (codeFile != null)
                {
                    context.Files.Add(codeFile);
                    context.TotalLines += codeFile.LineCount;
                    context.TotalSize += codeFile.Size;
                    count++;
                }
            }

            // Add metadata
            context.Metadata["FileCount"] = context.Files.Count.ToString();
            context.Metadata["TotalLines"] = context.TotalLines.ToString();
            context.Metadata["TotalSizeKB"] = (context.TotalSize / 1024).ToString();

            var languageGroups = context.Files.GroupBy(f => f.Language);
            foreach (var group in languageGroups)
            {
                context.Metadata[$"Language_{group.Key}"] = group.Count().ToString();
            }

            WriteObject(context);
        }

        private List<FileInfo> DiscoverFiles()
        {
            var files = new List<FileInfo>();

            try
            {
                var allFiles = Directory.GetFiles(_rootPath, "*.*", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f))
                    .Where(f => !IsExcluded(f.FullName));

                // Filter by recent modification if requested
                if (RecentlyModified)
                {
                    allFiles = allFiles.Where(f => f.LastWriteTime >= _cutoffTime);
                }

                // Filter by include patterns
                if (Include != null && Include.Length > 0)
                {
                    allFiles = allFiles.Where(f => Include.Any(pattern =>
                        MatchesPattern(f.Name, pattern)));
                }
                else
                {
                    // Default: common code file extensions
                    allFiles = allFiles.Where(f => IsCodeFile(f.Extension));
                }

                // Sort by last modified (newest first)
                files = allFiles
                    .OrderByDescending(f => f.LastWriteTime)
                    .ToList();
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(
                    ex,
                    "DiscoverFilesFailed",
                    ErrorCategory.ReadError,
                    _rootPath));
            }

            return files;
        }

        private bool IsExcluded(string filePath)
        {
            var relativePath = filePath.Substring(_rootPath.Length).TrimStart(System.IO.Path.DirectorySeparatorChar);
            var parts = relativePath.Split(System.IO.Path.DirectorySeparatorChar);

            foreach (var part in parts)
            {
                if (_excludePatterns.Contains(part))
                {
                    return true;
                }
            }

            return false;
        }

        private bool MatchesPattern(string fileName, string pattern)
        {
            var regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";
            return Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
        }

        private bool IsCodeFile(string extension)
        {
            string[] codeExtensions = {
                ".cs", ".vb", ".fs",                    // .NET
                ".js", ".ts", ".jsx", ".tsx", ".mjs",   // JavaScript/TypeScript
                ".py", ".pyw",                          // Python
                ".java", ".kt", ".scala",               // JVM languages
                ".cpp", ".c", ".h", ".hpp", ".cc",      // C/C++
                ".rs",                                  // Rust
                ".go",                                  // Go
                ".rb",                                  // Ruby
                ".php",                                 // PHP
                ".swift",                               // Swift
                ".m", ".mm",                            // Objective-C
                ".sql",                                 // SQL
                ".ps1", ".psm1", ".psd1",              // PowerShell
                ".sh", ".bash", ".zsh",                 // Shell
                ".yaml", ".yml", ".json", ".xml",       // Config
                ".md", ".markdown", ".rst"              // Documentation
            };

            return codeExtensions.Contains(extension.ToLowerInvariant());
        }

        private CodeFile ProcessFile(FileInfo fileInfo)
        {
            try
            {
                var codeFile = new CodeFile
                {
                    Path = fileInfo.FullName,
                    RelativePath = fileInfo.FullName.Substring(_rootPath.Length).TrimStart(System.IO.Path.DirectorySeparatorChar),
                    Language = DetermineLanguage(fileInfo.Extension),
                    Size = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime
                };

                if (IncludeContent)
                {
                    codeFile.Content = File.ReadAllText(fileInfo.FullName);
                    codeFile.LineCount = codeFile.Content.Split('\n').Length;
                }
                else
                {
                    codeFile.LineCount = CountLines(fileInfo.FullName);
                }

                if (IncludeMetrics)
                {
                    CalculateMetrics(codeFile);
                }

                if (IncludeDependencies)
                {
                    ExtractDependencies(codeFile);
                }

                return codeFile;
            }
            catch (Exception ex)
            {
                WriteWarning($"Failed to process file {fileInfo.Name}: {ex.Message}");
                return null;
            }
        }

        private string DetermineLanguage(string extension)
        {
            var languageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { ".cs", "C#" },
                { ".vb", "Visual Basic" },
                { ".fs", "F#" },
                { ".js", "JavaScript" },
                { ".ts", "TypeScript" },
                { ".jsx", "JavaScript (JSX)" },
                { ".tsx", "TypeScript (TSX)" },
                { ".py", "Python" },
                { ".java", "Java" },
                { ".kt", "Kotlin" },
                { ".cpp", "C++" },
                { ".c", "C" },
                { ".rs", "Rust" },
                { ".go", "Go" },
                { ".rb", "Ruby" },
                { ".php", "PHP" },
                { ".swift", "Swift" },
                { ".sql", "SQL" },
                { ".ps1", "PowerShell" },
                { ".sh", "Shell" },
                { ".yaml", "YAML" },
                { ".yml", "YAML" },
                { ".json", "JSON" },
                { ".xml", "XML" },
                { ".md", "Markdown" }
            };

            return languageMap.TryGetValue(extension, out var language) ? language : "Unknown";
        }

        private int CountLines(string filePath)
        {
            try
            {
                return File.ReadLines(filePath).Count();
            }
            catch
            {
                return 0;
            }
        }

        private void CalculateMetrics(CodeFile codeFile)
        {
            if (string.IsNullOrEmpty(codeFile.Content))
            {
                return;
            }

            var lines = codeFile.Content.Split('\n');

            // Count empty lines
            var emptyLines = lines.Count(l => string.IsNullOrWhiteSpace(l));
            codeFile.Metrics["EmptyLines"] = emptyLines;

            // Count comment lines (basic detection)
            var commentLines = 0;
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("#") ||
                    trimmed.StartsWith("/*") || trimmed.StartsWith("*") ||
                    trimmed.StartsWith("<!--"))
                {
                    commentLines++;
                }
            }
            codeFile.Metrics["CommentLines"] = commentLines;

            // Code lines (total - empty - comments)
            codeFile.Metrics["CodeLines"] = codeFile.LineCount - emptyLines - commentLines;

            // Average line length
            var nonEmptyLines = lines.Where(l => !string.IsNullOrWhiteSpace(l));
            if (nonEmptyLines.Any())
            {
                codeFile.Metrics["AverageLineLength"] = (int)nonEmptyLines.Average(l => l.Length);
            }

            // Max line length
            if (lines.Any())
            {
                codeFile.Metrics["MaxLineLength"] = lines.Max(l => l.Length);
            }
        }

        private void ExtractDependencies(CodeFile codeFile)
        {
            if (string.IsNullOrEmpty(codeFile.Content))
            {
                return;
            }

            var dependencies = new HashSet<string>();

            // Language-specific dependency extraction
            switch (codeFile.Language)
            {
                case "C#":
                    ExtractCSharpDependencies(codeFile.Content, dependencies);
                    break;
                case "JavaScript":
                case "TypeScript":
                case "JavaScript (JSX)":
                case "TypeScript (TSX)":
                    ExtractJavaScriptDependencies(codeFile.Content, dependencies);
                    break;
                case "Python":
                    ExtractPythonDependencies(codeFile.Content, dependencies);
                    break;
                case "Java":
                    ExtractJavaDependencies(codeFile.Content, dependencies);
                    break;
            }

            codeFile.Dependencies = dependencies.ToList();
        }

        private void ExtractCSharpDependencies(string content, HashSet<string> dependencies)
        {
            // Match: using Namespace;
            var matches = Regex.Matches(content, @"^\s*using\s+([A-Za-z0-9_.]+);", RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                dependencies.Add(match.Groups[1].Value);
            }
        }

        private void ExtractJavaScriptDependencies(string content, HashSet<string> dependencies)
        {
            // Match: import ... from 'module'
            var matches = Regex.Matches(content, @"import\s+.*?\s+from\s+['""](.+?)['""]", RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                dependencies.Add(match.Groups[1].Value);
            }

            // Match: require('module')
            matches = Regex.Matches(content, @"require\(['""](.+?)['""]\)", RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                dependencies.Add(match.Groups[1].Value);
            }
        }

        private void ExtractPythonDependencies(string content, HashSet<string> dependencies)
        {
            // Match: import module
            var matches = Regex.Matches(content, @"^\s*import\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                dependencies.Add(match.Groups[1].Value);
            }

            // Match: from module import ...
            matches = Regex.Matches(content, @"^\s*from\s+([A-Za-z0-9_.]+)\s+import", RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                dependencies.Add(match.Groups[1].Value);
            }
        }

        private void ExtractJavaDependencies(string content, HashSet<string> dependencies)
        {
            // Match: import package.Class;
            var matches = Regex.Matches(content, @"^\s*import\s+([A-Za-z0-9_.]+);", RegexOptions.Multiline);
            foreach (Match match in matches)
            {
                dependencies.Add(match.Groups[1].Value);
            }
        }
    }
}
