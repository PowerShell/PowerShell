// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.PowerShell.Development.ProjectContext
{
    /// <summary>
    /// Represents detected project context information.
    /// </summary>
    public class ProjectContext
    {
        public string ProjectType { get; set; }
        public string DetectedFrom { get; set; }
        public string RootPath { get; set; }
        public string BuildTool { get; set; }
        public string TestFramework { get; set; }
        public string Language { get; set; }
        public List<string> SuggestedCommands { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public ProjectContext()
        {
            SuggestedCommands = new List<string>();
            Metadata = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Detector for identifying project types based on files.
    /// </summary>
    public static class ProjectDetector
    {
        private static readonly List<ProjectPattern> Patterns = new List<ProjectPattern>
        {
            new ProjectPattern
            {
                Name = "Node.js",
                DetectionFiles = new[] { "package.json" },
                Language = "JavaScript",
                BuildTool = "npm",
                TestFramework = "jest/mocha",
                SuggestedCommands = new[] { "npm install", "npm run build", "npm test" }
            },
            new ProjectPattern
            {
                Name = "Rust",
                DetectionFiles = new[] { "Cargo.toml" },
                Language = "Rust",
                BuildTool = "cargo",
                TestFramework = "cargo test",
                SuggestedCommands = new[] { "cargo build", "cargo test", "cargo run" }
            },
            new ProjectPattern
            {
                Name = ".NET",
                DetectionFiles = new[] { "*.csproj", "*.fsproj", "*.vbproj", "*.sln" },
                Language = "C#/F#/VB.NET",
                BuildTool = "dotnet",
                TestFramework = "xUnit/NUnit/MSTest",
                SuggestedCommands = new[] { "dotnet restore", "dotnet build", "dotnet test" }
            },
            new ProjectPattern
            {
                Name = "Python",
                DetectionFiles = new[] { "setup.py", "pyproject.toml", "requirements.txt" },
                Language = "Python",
                BuildTool = "pip/poetry",
                TestFramework = "pytest/unittest",
                SuggestedCommands = new[] { "pip install -r requirements.txt", "pytest", "python -m unittest" }
            },
            new ProjectPattern
            {
                Name = "Go",
                DetectionFiles = new[] { "go.mod" },
                Language = "Go",
                BuildTool = "go",
                TestFramework = "go test",
                SuggestedCommands = new[] { "go build", "go test", "go run ." }
            },
            new ProjectPattern
            {
                Name = "Java (Maven)",
                DetectionFiles = new[] { "pom.xml" },
                Language = "Java",
                BuildTool = "mvn",
                TestFramework = "JUnit",
                SuggestedCommands = new[] { "mvn clean install", "mvn test", "mvn package" }
            },
            new ProjectPattern
            {
                Name = "Java (Gradle)",
                DetectionFiles = new[] { "build.gradle", "build.gradle.kts" },
                Language = "Java/Kotlin",
                BuildTool = "gradle",
                TestFramework = "JUnit",
                SuggestedCommands = new[] { "gradle build", "gradle test", "./gradlew build" }
            },
            new ProjectPattern
            {
                Name = "Ruby",
                DetectionFiles = new[] { "Gemfile" },
                Language = "Ruby",
                BuildTool = "bundle",
                TestFramework = "RSpec/Minitest",
                SuggestedCommands = new[] { "bundle install", "rake test", "rspec" }
            },
            new ProjectPattern
            {
                Name = "PHP (Composer)",
                DetectionFiles = new[] { "composer.json" },
                Language = "PHP",
                BuildTool = "composer",
                TestFramework = "PHPUnit",
                SuggestedCommands = new[] { "composer install", "composer test", "phpunit" }
            },
            new ProjectPattern
            {
                Name = "PowerShell Module",
                DetectionFiles = new[] { "*.psd1" },
                Language = "PowerShell",
                BuildTool = "pwsh",
                TestFramework = "Pester",
                SuggestedCommands = new[] { "Import-Module ./build.psm1", "Invoke-Pester", "Test-ModuleManifest" }
            }
        };

        public static ProjectContext Detect(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return null;
            }

            foreach (var pattern in Patterns)
            {
                foreach (var detectionFile in pattern.DetectionFiles)
                {
                    string[] matchedFiles;

                    if (detectionFile.Contains("*"))
                    {
                        // Glob pattern
                        var searchPattern = Path.GetFileName(detectionFile);
                        matchedFiles = Directory.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
                    }
                    else
                    {
                        // Exact filename
                        var fullPath = Path.Combine(path, detectionFile);
                        matchedFiles = File.Exists(fullPath) ? new[] { fullPath } : Array.Empty<string>();
                    }

                    if (matchedFiles.Length > 0)
                    {
                        return new ProjectContext
                        {
                            ProjectType = pattern.Name,
                            DetectedFrom = Path.GetFileName(matchedFiles[0]),
                            RootPath = path,
                            BuildTool = pattern.BuildTool,
                            TestFramework = pattern.TestFramework,
                            Language = pattern.Language,
                            SuggestedCommands = pattern.SuggestedCommands.ToList(),
                            Metadata = new Dictionary<string, object>
                            {
                                { "DetectedFiles", matchedFiles.Select(Path.GetFileName).ToList() }
                            }
                        };
                    }
                }
            }

            return null;
        }
    }

    internal class ProjectPattern
    {
        public string Name { get; set; }
        public string[] DetectionFiles { get; set; }
        public string Language { get; set; }
        public string BuildTool { get; set; }
        public string TestFramework { get; set; }
        public string[] SuggestedCommands { get; set; }
    }
}
