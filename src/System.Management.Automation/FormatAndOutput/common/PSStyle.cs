// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation
{
    #region OutputRendering
    /// <summary>
    /// Defines the options for output rendering.
    /// </summary>
    public enum OutputRendering
    {
        /// <summary>Render ANSI only to host.</summary>
        Host = 0,

        /// <summary>Render as plaintext.</summary>
        PlainText = 1,

        /// <summary>Render as ANSI.</summary>
        Ansi = 2,
    }
    #endregion OutputRendering

    /// <summary>
    /// Defines the options for views of progress rendering.
    /// </summary>
    public enum ProgressView
    {
        /// <summary>Render progress using minimal space.</summary>
        Minimal = 0,

        /// <summary>Classic rendering of progress.</summary>
        Classic = 1,
    }

    #region PSStyle
    /// <summary>
    /// Contains configuration for how PowerShell renders text.
    /// </summary>
    public sealed class PSStyle : IScopedPropertyContainer
    {
        private readonly PSScopedPropertyFacade _scopedFacade;

        /// <summary>
        /// Contains foreground colors.
        /// </summary>
        public sealed class ForegroundColor : IScopedPropertyContainer
        {
            private readonly PSStyle _owner;

            /// <summary>Initializes a standalone foreground color group.</summary>
            public ForegroundColor() { }

            internal ForegroundColor(PSStyle owner) => _owner = owner;

            private string GetColor(string value)
            {
                _owner?.ValidateView();
                return value;
            }

            /// <summary>
            /// Gets the color black.
            /// </summary>
            public string Black { get => GetColor("\x1b[30m"); }

            /// <summary>
            /// Gets the color red.
            /// </summary>
            public string Red { get => GetColor("\x1b[31m"); }

            /// <summary>
            /// Gets the color green.
            /// </summary>
            public string Green { get => GetColor("\x1b[32m"); }

            /// <summary>
            /// Gets the color yellow.
            /// </summary>
            public string Yellow { get => GetColor("\x1b[33m"); }

            /// <summary>
            /// Gets the color blue.
            /// </summary>
            public string Blue { get => GetColor("\x1b[34m"); }

            /// <summary>
            /// Gets the color magenta.
            /// </summary>
            public string Magenta { get => GetColor("\x1b[35m"); }

            /// <summary>
            /// Gets the color cyan.
            /// </summary>
            public string Cyan { get => GetColor("\x1b[36m"); }

            /// <summary>
            /// Gets the color white.
            /// </summary>
            public string White { get => GetColor("\x1b[37m"); }

            /// <summary>
            /// Gets the color bright black.
            /// </summary>
            public string BrightBlack { get => GetColor("\x1b[90m"); }

            /// <summary>
            /// Gets the color bright red.
            /// </summary>
            public string BrightRed { get => GetColor("\x1b[91m"); }

            /// <summary>
            /// Gets the color bright green.
            /// </summary>
            public string BrightGreen { get => GetColor("\x1b[92m"); }

            /// <summary>
            /// Gets the color bright yellow.
            /// </summary>
            public string BrightYellow { get => GetColor("\x1b[93m"); }

            /// <summary>
            /// Gets the color bright blue.
            /// </summary>
            public string BrightBlue { get => GetColor("\x1b[94m"); }

            /// <summary>
            /// Gets the color bright magenta.
            /// </summary>
            public string BrightMagenta { get => GetColor("\x1b[95m"); }

            /// <summary>
            /// Gets the color bright cyan.
            /// </summary>
            public string BrightCyan { get => GetColor("\x1b[96m"); }

            /// <summary>
            /// Gets the color bright white.
            /// </summary>
            public string BrightWhite { get => GetColor("\x1b[97m"); }

            /// <summary>
            /// Set as RGB (Red, Green, Blue).
            /// </summary>
            /// <param name="red">Byte value representing red.</param>
            /// <param name="green">Byte value representing green.</param>
            /// <param name="blue">Byte value representing blue.</param>
            /// <returns>String representing ANSI code for RGB value.</returns>
            public string FromRgb(byte red, byte green, byte blue)
            {
                _owner?.ValidateView();
                return $"\x1b[38;2;{red};{green};{blue}m";
            }

            /// <summary>
            /// The color set as RGB as a single number.
            /// </summary>
            /// <param name="rgb">RGB value specified as an integer.</param>
            /// <returns>String representing ANSI code for RGB value.</returns>
            public string FromRgb(int rgb)
            {
                byte red, green, blue;
                blue = (byte)(rgb & 0xFF);
                rgb >>= 8;
                green = (byte)(rgb & 0xFF);
                rgb >>= 8;
                red = (byte)(rgb & 0xFF);

                return FromRgb(red, green, blue);
            }

            /// <summary>
            /// Return the VT escape sequence for a foreground color.
            /// </summary>
            /// <param name="color">The foreground color to be mapped from.</param>
            /// <returns>The VT escape sequence representing the foreground color.</returns>
            public string FromConsoleColor(ConsoleColor color)
            {
                _owner?.ValidateView();
                return MapForegroundColorToEscapeSequence(color);
            }

            /// <summary>Returns a description of the foreground color group.</summary>
            /// <returns>The foreground color group description.</returns>
            public override string ToString() => SessionStateStrings.ScopedStyleForegroundDescription;
        }

        /// <summary>
        /// Contains background colors.
        /// </summary>
        public sealed class BackgroundColor : IScopedPropertyContainer
        {
            private readonly PSStyle _owner;

            /// <summary>Initializes a standalone background color group.</summary>
            public BackgroundColor() { }

            internal BackgroundColor(PSStyle owner) => _owner = owner;

            private string GetColor(string value)
            {
                _owner?.ValidateView();
                return value;
            }

            /// <summary>
            /// Gets the color black.
            /// </summary>
            public string Black { get => GetColor("\x1b[40m"); }

            /// <summary>
            /// Gets the color red.
            /// </summary>
            public string Red { get => GetColor("\x1b[41m"); }

            /// <summary>
            /// Gets the color green.
            /// </summary>
            public string Green { get => GetColor("\x1b[42m"); }

            /// <summary>
            /// Gets the color yellow.
            /// </summary>
            public string Yellow { get => GetColor("\x1b[43m"); }

            /// <summary>
            /// Gets the color blue.
            /// </summary>
            public string Blue { get => GetColor("\x1b[44m"); }

            /// <summary>
            /// Gets the color magenta.
            /// </summary>
            public string Magenta { get => GetColor("\x1b[45m"); }

            /// <summary>
            /// Gets the color cyan.
            /// </summary>
            public string Cyan { get => GetColor("\x1b[46m"); }

            /// <summary>
            /// Gets the color white.
            /// </summary>
            public string White { get => GetColor("\x1b[47m"); }

            /// <summary>
            /// Gets the color bright black.
            /// </summary>
            public string BrightBlack { get => GetColor("\x1b[100m"); }

            /// <summary>
            /// Gets the color bright red.
            /// </summary>
            public string BrightRed { get => GetColor("\x1b[101m"); }

            /// <summary>
            /// Gets the color bright green.
            /// </summary>
            public string BrightGreen { get => GetColor("\x1b[102m"); }

            /// <summary>
            /// Gets the color bright yellow.
            /// </summary>
            public string BrightYellow { get => GetColor("\x1b[103m"); }

            /// <summary>
            /// Gets the color bright blue.
            /// </summary>
            public string BrightBlue { get => GetColor("\x1b[104m"); }

            /// <summary>
            /// Gets the color bright magenta.
            /// </summary>
            public string BrightMagenta { get => GetColor("\x1b[105m"); }

            /// <summary>
            /// Gets the color bright cyan.
            /// </summary>
            public string BrightCyan { get => GetColor("\x1b[106m"); }

            /// <summary>
            /// Gets the color bright white.
            /// </summary>
            public string BrightWhite { get => GetColor("\x1b[107m"); }

            /// <summary>
            /// The color set as RGB (Red, Green, Blue).
            /// </summary>
            /// <param name="red">Byte value representing red.</param>
            /// <param name="green">Byte value representing green.</param>
            /// <param name="blue">Byte value representing blue.</param>
            /// <returns>String representing ANSI code for RGB value.</returns>
            public string FromRgb(byte red, byte green, byte blue)
            {
                _owner?.ValidateView();
                return $"\x1b[48;2;{red};{green};{blue}m";
            }

            /// <summary>
            /// The color set as RGB as a single number.
            /// </summary>
            /// <param name="rgb">RGB value specified as an integer.</param>
            /// <returns>String representing ANSI code for RGB value.</returns>
            public string FromRgb(int rgb)
            {
                byte red, green, blue;
                blue = (byte)(rgb & 0xFF);
                rgb >>= 8;
                green = (byte)(rgb & 0xFF);
                rgb >>= 8;
                red = (byte)(rgb & 0xFF);

                return FromRgb(red, green, blue);
            }

            /// <summary>
            /// Return the VT escape sequence for a background color.
            /// </summary>
            /// <param name="color">The background color to be mapped from.</param>
            /// <returns>The VT escape sequence representing the background color.</returns>
            public string FromConsoleColor(ConsoleColor color)
            {
                _owner?.ValidateView();
                return MapBackgroundColorToEscapeSequence(color);
            }

            /// <summary>Returns a description of the background color group.</summary>
            /// <returns>The background color group description.</returns>
            public override string ToString() => SessionStateStrings.ScopedStyleBackgroundDescription;
        }

        /// <summary>
        /// Contains configuration for the progress bar visualization.
        /// </summary>
        public sealed class ProgressConfiguration : IScopedPropertyContainer
        {
            private readonly PSStyle _owner;

            /// <summary>Initializes standalone progress rendering settings.</summary>
            public ProgressConfiguration() : this(null) { }

            internal ProgressConfiguration(PSStyle owner) => _owner = owner;

            /// <summary>
            /// Gets or sets the style for progress bar.
            /// </summary>
            public string Style
            {
                get => _owner?.GetScoped(ScopedProperties.StyleProgressStyleSetting, _style) ?? _style;
                set
                {
                    value = ValidateNoContent(value);
                    if (_owner is null || !_owner.SetScoped(ScopedProperties.StyleProgressStyleSetting, value)) _style = value;
                }
            }

            private string _style = "\x1b[33;1m";

            internal string DefaultStyle => _style;

            /// <summary>
            /// Gets or sets the max width of the progress bar.
            /// </summary>
            public int MaxWidth
            {
                get => _owner?.GetScoped(ScopedProperties.StyleProgressMaxWidthSetting, _maxWidth) ?? _maxWidth;
                set
                {
                    // Width less than 18 does not render correctly due to the different parts of the progress bar.
                    if (value < 18)
                    {
                        throw new ArgumentOutOfRangeException(nameof(MaxWidth), PSStyleStrings.ProgressWidthTooSmall);
                    }

                    if (_owner is null || !_owner.SetScoped(ScopedProperties.StyleProgressMaxWidthSetting, value)) _maxWidth = value;
                }
            }

            private int _maxWidth = 120;

            internal int DefaultMaxWidth => _maxWidth;

            /// <summary>
            /// Gets or sets the view for progress bar.
            /// </summary>
            public ProgressView View
            {
                get
                {
                    if (_owner?._scopedFacade is not null)
                    {
                        return _owner._scopedFacade.GetScopedStyleSetting(ScopedProperties.ProgressViewSetting);
                    }

                    if (_owner is not null && ExperimentalFeature.PSScopedProperties && LocalPipeline.GetExecutionContextFromTLS() is ExecutionContext context)
                    {
                        return ScopedProperties.GetProgressView(context);
                    }

                    return _view;
                }

                set
                {
                    if (_owner?._scopedFacade is not null)
                    {
                        _owner._scopedFacade.SetScopedStyleSetting(ScopedProperties.ProgressViewSetting, value);
                        return;
                    }

                    if (_owner is not null && ExperimentalFeature.PSScopedProperties && LocalPipeline.GetExecutionContextFromTLS() is ExecutionContext context)
                    {
                        ScopedProperties.SetProgressView(context, value);
                        return;
                    }

                    _view = value;
                }
            }

            private ProgressView _view = ProgressView.Minimal;

            private bool _useOSCIndicator;

            internal bool DefaultUseOSCIndicator => _useOSCIndicator;

            /// <summary>
            /// Gets or sets a value indicating whether to use Operating System Command (OSC) control sequences 'ESC ]9;4;' to show indicator in terminal.
            /// </summary>
            public bool UseOSCIndicator
            {
                get => _owner?.GetScoped(ScopedProperties.StyleProgressUseOSCIndicatorSetting, _useOSCIndicator) ?? _useOSCIndicator;
                set
                {
                    if (_owner is null || !_owner.SetScoped(ScopedProperties.StyleProgressUseOSCIndicatorSetting, value)) _useOSCIndicator = value;
                }
            }

            /// <summary>Returns a description of the progress rendering settings.</summary>
            /// <returns>The progress rendering settings description.</returns>
            public override string ToString() => SessionStateStrings.ScopedProgressStyleDescription;
        }

        /// <summary>
        /// Contains formatting styles for steams and objects.
        /// </summary>
        public sealed class FormattingData : IScopedPropertyContainer
        {
            private readonly PSStyle _owner;

            /// <summary>Initializes standalone formatting style settings.</summary>
            public FormattingData() : this(null) { }

            internal FormattingData(PSStyle owner) => _owner = owner;

            /// <summary>
            /// Gets or sets the accent style for formatting.
            /// </summary>
            public string FormatAccent
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFormattingFormatAccentSetting, _formatAccent) ?? _formatAccent;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFormattingFormatAccentSetting, value, v => _formatAccent = v);
            }

            private string _formatAccent = "\x1b[32;1m";

            /// <summary>
            /// Gets or sets the style for table headers.
            /// </summary>
            public string TableHeader
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFormattingTableHeaderSetting, _tableHeader) ?? _tableHeader;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFormattingTableHeaderSetting, value, v => _tableHeader = v);
            }

            private string _tableHeader = "\x1b[32;1m";

            /// <summary>
            /// Gets or sets the style for custom table headers.
            /// </summary>
            public string CustomTableHeaderLabel
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFormattingCustomTableHeaderLabelSetting, _customTableHeaderLabel) ?? _customTableHeaderLabel;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFormattingCustomTableHeaderLabelSetting, value, v => _customTableHeaderLabel = v);
            }

            private string _customTableHeaderLabel = "\x1b[32;1;3m";

            /// <summary>
            /// Gets or sets the accent style for errors.
            /// </summary>
            public string ErrorAccent
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFormattingErrorAccentSetting, _errorAccent) ?? _errorAccent;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFormattingErrorAccentSetting, value, v => _errorAccent = v);
            }

            private string _errorAccent = "\x1b[36;1m";

            /// <summary>
            /// Gets or sets the style for error messages.
            /// </summary>
            public string Error
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFormattingErrorSetting, _error) ?? _error;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFormattingErrorSetting, value, v => _error = v);
            }

            private string _error = "\x1b[31;1m";

            /// <summary>
            /// Gets or sets the style for warning messages.
            /// </summary>
            public string Warning
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFormattingWarningSetting, _warning) ?? _warning;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFormattingWarningSetting, value, v => _warning = v);
            }

            private string _warning = "\x1b[33;1m";

            /// <summary>
            /// Gets or sets the style for verbose messages.
            /// </summary>
            public string Verbose
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFormattingVerboseSetting, _verbose) ?? _verbose;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFormattingVerboseSetting, value, v => _verbose = v);
            }

            private string _verbose = "\x1b[33;1m";

            /// <summary>
            /// Gets or sets the style for debug messages.
            /// </summary>
            public string Debug
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFormattingDebugSetting, _debug) ?? _debug;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFormattingDebugSetting, value, v => _debug = v);
            }

            private string _debug = "\x1b[33;1m";

            /// <summary>
            /// Gets or sets the style for rendering feedback provider names.
            /// </summary>
            public string FeedbackName
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFormattingFeedbackNameSetting, _feedbackName) ?? _feedbackName;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFormattingFeedbackNameSetting, value, v => _feedbackName = v);
            }

            // Yellow by default.
            private string _feedbackName = "\x1b[33m";

            /// <summary>
            /// Gets or sets the style for rendering feedback message.
            /// </summary>
            public string FeedbackText
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFormattingFeedbackTextSetting, _feedbackText) ?? _feedbackText;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFormattingFeedbackTextSetting, value, v => _feedbackText = v);
            }

            // BrightCyan by default.
            private string _feedbackText = "\x1b[96m";

            /// <summary>
            /// Gets or sets the style for rendering feedback actions.
            /// </summary>
            public string FeedbackAction
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFormattingFeedbackActionSetting, _feedbackAction) ?? _feedbackAction;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFormattingFeedbackActionSetting, value, v => _feedbackAction = v);
            }

            // BrightWhite by default.
            private string _feedbackAction = "\x1b[97m";

            internal string DefaultFormatAccent => _formatAccent;

            internal string DefaultTableHeader => _tableHeader;

            internal string DefaultCustomTableHeaderLabel => _customTableHeaderLabel;

            internal string DefaultErrorAccent => _errorAccent;

            internal string DefaultError => _error;

            internal string DefaultWarning => _warning;

            internal string DefaultVerbose => _verbose;

            internal string DefaultDebug => _debug;

            internal string DefaultFeedbackName => _feedbackName;

            internal string DefaultFeedbackText => _feedbackText;

            internal string DefaultFeedbackAction => _feedbackAction;

            /// <summary>Returns a description of the formatting style settings.</summary>
            /// <returns>The formatting style settings description.</returns>
            public override string ToString() => SessionStateStrings.ScopedStyleFormattingDescription;
        }

        /// <summary>
        /// Contains formatting styles for FileInfo objects.
        /// </summary>
        public sealed class FileInfoFormatting : IScopedPropertyContainer
        {
            private readonly PSStyle _owner;

            /// <summary>Initializes standalone FileInfo formatting settings.</summary>
            public FileInfoFormatting() : this(null) { }

            internal FileInfoFormatting(PSStyle owner)
            {
                _owner = owner;
                _extension = new FileExtensionDictionary(owner);
                InitializeExtensions();
            }

            /// <summary>
            /// Gets or sets the style for directories.
            /// </summary>
            public string Directory
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFileInfoDirectorySetting, _directory) ?? _directory;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFileInfoDirectorySetting, value, v => _directory = v);
            }

            private string _directory = "\x1b[44;1m";

            /// <summary>
            /// Gets or sets the style for symbolic links.
            /// </summary>
            public string SymbolicLink
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFileInfoSymbolicLinkSetting, _symbolicLink) ?? _symbolicLink;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFileInfoSymbolicLinkSetting, value, v => _symbolicLink = v);
            }

            private string _symbolicLink = "\x1b[36;1m";

            /// <summary>
            /// Gets or sets the style for executables.
            /// </summary>
            public string Executable
            {
                get => _owner?.GetScoped(ScopedProperties.StyleFileInfoExecutableSetting, _executable) ?? _executable;
                set => PSStyle.SetText(_owner, ScopedProperties.StyleFileInfoExecutableSetting, value, v => _executable = v);
            }

            private string _executable = "\x1b[32;1m";

            internal string DefaultDirectory => _directory;

            internal string DefaultSymbolicLink => _symbolicLink;

            internal string DefaultExecutable => _executable;

            /// <summary>
            /// Custom dictionary handling validation of extension and content.
            /// </summary>
            public sealed class FileExtensionDictionary : IScopedPropertyContainer
            {
                private static string ValidateExtension(string extension)
                {
                    if (!extension.StartsWith('.'))
                    {
                        throw new ArgumentException(PSStyleStrings.ExtensionNotStartingWithPeriod);
                    }

                    return extension;
                }

                private readonly Dictionary<string, string> _extensionDictionary = new(StringComparer.OrdinalIgnoreCase);
                private readonly PSStyle _owner;

                /// <summary>Initializes an empty standalone file extension dictionary.</summary>
                public FileExtensionDictionary() : this(null) { }

                internal FileExtensionDictionary(PSStyle owner) => _owner = owner;

                internal Dictionary<string, string> CopyValues()
                    => new Dictionary<string, string>(_extensionDictionary, StringComparer.OrdinalIgnoreCase);

                /// <summary>
                /// Add new extension and decoration to dictionary.
                /// </summary>
                /// <param name="extension">Extension to add.</param>
                /// <param name="decoration">ANSI string value to add.</param>
                public void Add(string extension, string decoration)
                {
                    extension = ValidateExtension(extension);
                    decoration = ValidateNoContent(decoration);
                    Mutate(dictionary => dictionary.Add(extension, decoration));
                }

                /// <summary>
                /// Add new extension and decoration to dictionary without validation.
                /// </summary>
                /// <param name="extension">Extension to add.</param>
                /// <param name="decoration">ANSI string value to add.</param>
                internal void AddWithoutValidation(string extension, string decoration)
                {
                    _extensionDictionary.Add(extension, decoration);
                }

                /// <summary>
                /// Remove an extension from dictionary.
                /// </summary>
                /// <param name="extension">Extension to remove.</param>
                public void Remove(string extension)
                {
                    extension = ValidateExtension(extension);
                    if (_owner is not null && _owner.TryGetScoped(ScopedProperties.StyleFileInfoExtensionsSetting, out Dictionary<string, string> current))
                    {
                        if (!current.ContainsKey(extension)) return;
                    }
                    else if (!_extensionDictionary.ContainsKey(extension))
                    {
                        return;
                    }

                    Mutate(dictionary => dictionary.Remove(extension));
                }

                /// <summary>
                /// Clear the dictionary.
                /// </summary>
                public void Clear()
                {
                    if (_owner is not null && _owner.TryGetScoped(ScopedProperties.StyleFileInfoExtensionsSetting, out Dictionary<string, string> current))
                    {
                        if (current.Count == 0) return;
                    }
                    else if (_extensionDictionary.Count == 0)
                    {
                        return;
                    }

                    Mutate(dictionary => dictionary.Clear());
                }

                /// <summary>
                /// Gets or sets the decoration by specified extension.
                /// </summary>
                /// <param name="extension">Extension to get decoration for.</param>
                /// <returns>The decoration for specified extension.</returns>
                public string this[string extension]
                {
                    get
                    {
                        extension = ValidateExtension(extension);
                        return _owner is not null && _owner.TryGetScoped(ScopedProperties.StyleFileInfoExtensionsSetting, out Dictionary<string, string> dictionary)
                            ? dictionary[extension]
                            : _extensionDictionary[extension];
                    }

                    set
                    {
                        extension = ValidateExtension(extension);
                        value = ValidateNoContent(value);
                        Mutate(dictionary => dictionary[extension] = value);
                    }
                }

                /// <summary>
                /// Gets whether the dictionary contains the specified extension.
                /// </summary>
                /// <param name="extension">Extension to check for.</param>
                /// <returns>True if the dictionary contains the specified extension, otherwise false.</returns>
                public bool ContainsKey(string extension)
                {
                    if (string.IsNullOrEmpty(extension))
                    {
                        return false;
                    }

                    return _owner is not null && _owner.TryGetScoped(ScopedProperties.StyleFileInfoExtensionsSetting, out Dictionary<string, string> dictionary)
                        ? dictionary.ContainsKey(ValidateExtension(extension))
                        : _extensionDictionary.ContainsKey(ValidateExtension(extension));
                }

                /// <summary>
                /// Gets the extensions for the dictionary.
                /// </summary>
                /// <returns>The extensions for the dictionary.</returns>
                public IEnumerable<string> Keys
                {
                    get
                    {
                        return _owner is not null && _owner.TryGetScoped(ScopedProperties.StyleFileInfoExtensionsSetting, out Dictionary<string, string> dictionary)
                            ? Array.AsReadOnly(dictionary.Keys.ToArray())
                            : Array.AsReadOnly(_extensionDictionary.Keys.ToArray());
                    }
                }

                /// <summary>Returns a description of the file extension styles.</summary>
                /// <returns>The file extension styles description.</returns>
                public override string ToString() => SessionStateStrings.ScopedStyleFileInfoExtensionDescription;

                private void Mutate(Action<Dictionary<string, string>> mutation)
                {
                    if (_owner is not null && _owner.TryGetScoped(ScopedProperties.StyleFileInfoExtensionsSetting, out Dictionary<string, string> current))
                    {
                        var updated = new Dictionary<string, string>(current, StringComparer.OrdinalIgnoreCase);
                        mutation(updated);
                        _owner.SetScoped(ScopedProperties.StyleFileInfoExtensionsSetting, updated);
                    }
                    else
                    {
                        mutation(_extensionDictionary);
                    }
                }
            }

            private readonly FileExtensionDictionary _extension;

            /// <summary>
            /// Gets the style for archive.
            /// </summary>
            public FileExtensionDictionary Extension
            {
                get
                {
                    _owner?.ValidateView();
                    return _extension;
                }
            }

            internal Dictionary<string, string> DefaultExtensions => _extension.CopyValues();

            /// <summary>
            /// Initializes a new instance of the <see cref="FileInfoFormatting"/> class.
            /// </summary>
            private void InitializeExtensions()
            {
                // archives
                _extension.AddWithoutValidation(".zip", "\x1b[31;1m");
                _extension.AddWithoutValidation(".tgz", "\x1b[31;1m");
                _extension.AddWithoutValidation(".gz", "\x1b[31;1m");
                _extension.AddWithoutValidation(".tar", "\x1b[31;1m");
                _extension.AddWithoutValidation(".nupkg", "\x1b[31;1m");
                _extension.AddWithoutValidation(".cab", "\x1b[31;1m");
                _extension.AddWithoutValidation(".7z", "\x1b[31;1m");

                // powershell
                _extension.AddWithoutValidation(".ps1", "\x1b[33;1m");
                _extension.AddWithoutValidation(".psd1", "\x1b[33;1m");
                _extension.AddWithoutValidation(".psm1", "\x1b[33;1m");
                _extension.AddWithoutValidation(".ps1xml", "\x1b[33;1m");
            }

            /// <summary>Returns a description of the FileInfo formatting settings.</summary>
            /// <returns>The FileInfo formatting settings description.</returns>
            public override string ToString() => SessionStateStrings.ScopedStyleFileInfoDescription;
        }

        private OutputRendering _outputRendering = OutputRendering.Host;

        /// <summary>
        /// Gets or sets the rendering mode for output.
        /// </summary>
        public OutputRendering OutputRendering
        {
            get => GetScoped(ScopedProperties.StyleOutputRenderingSetting, _outputRendering);
            set
            {
                if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
                if (!SetScoped(ScopedProperties.StyleOutputRenderingSetting, value)) _outputRendering = value;
            }
        }

        private bool _autoSizeDefaultFormatting;

        /// <summary>
        /// Gets or sets whether to force the default implicit formatting to use 'AutoSize'.
        /// </summary>
        public bool AutoSizeDefaultFormatting
        {
            get => GetScoped(ScopedProperties.StyleAutoSizeDefaultFormattingSetting, _autoSizeDefaultFormatting);
            set
            {
                if (!SetScoped(ScopedProperties.StyleAutoSizeDefaultFormattingSetting, value)) _autoSizeDefaultFormatting = value;
            }
        }

        /// <summary>Returns a description of the style settings.</summary>
        /// <returns>The style settings description.</returns>
        public override string ToString() => SessionStateStrings.ScopedStyleDescription;

        /// <summary>
        /// Gets value to turn off all attributes.
        /// </summary>
        public string Reset => GetAnsiCode("\x1b[0m");

        /// <summary>
        /// Gets value to turn off blink.
        /// </summary>
        public string BlinkOff => GetAnsiCode("\x1b[25m");

        /// <summary>
        /// Gets value to turn on blink.
        /// </summary>
        public string Blink => GetAnsiCode("\x1b[5m");

        /// <summary>
        /// Gets value to turn off bold.
        /// </summary>
        public string BoldOff => GetAnsiCode("\x1b[22m");

        /// <summary>
        /// Gets value to turn on blink.
        /// </summary>
        public string Bold => GetAnsiCode("\x1b[1m");

        /// <summary>
        /// Gets value to turn off dim.
        /// </summary>
        public string DimOff => GetAnsiCode("\x1b[22m");

        /// <summary>
        /// Gets value to turn on dim.
        /// </summary>
        public string Dim => GetAnsiCode("\x1b[2m");

        /// <summary>
        /// Gets value to turn on hidden.
        /// </summary>
        public string Hidden => GetAnsiCode("\x1b[8m");

        /// <summary>
        /// Gets value to turn off hidden.
        /// </summary>
        public string HiddenOff => GetAnsiCode("\x1b[28m");

        /// <summary>
        /// Gets value to turn on reverse.
        /// </summary>
        public string Reverse => GetAnsiCode("\x1b[7m");

        /// <summary>
        /// Gets value to turn off reverse.
        /// </summary>
        public string ReverseOff => GetAnsiCode("\x1b[27m");

        /// <summary>
        /// Gets value to turn off standout.
        /// </summary>
        public string ItalicOff => GetAnsiCode("\x1b[23m");

        /// <summary>
        /// Gets value to turn on standout.
        /// </summary>
        public string Italic => GetAnsiCode("\x1b[3m");

        /// <summary>
        /// Gets value to turn off underlined.
        /// </summary>
        public string UnderlineOff => GetAnsiCode("\x1b[24m");

        /// <summary>
        /// Gets value to turn on underlined.
        /// </summary>
        public string Underline => GetAnsiCode("\x1b[4m");

        /// <summary>
        /// Gets value to turn off strikethrough.
        /// </summary>
        public string StrikethroughOff => GetAnsiCode("\x1b[29m");

        /// <summary>
        /// Gets value to turn on strikethrough.
        /// </summary>
        public string Strikethrough => GetAnsiCode("\x1b[9m");

        /// <summary>
        /// Gets ANSI representation of a hyperlink.
        /// </summary>
        /// <param name="text">Text describing the link.</param>
        /// <param name="link">A valid hyperlink.</param>
        /// <returns>String representing ANSI code for the hyperlink.</returns>
        public string FormatHyperlink(string text, Uri link)
        {
            ValidateView();
            return $"\x1b]8;;{link}\x1b\\{text}\x1b]8;;\x1b\\";
        }

        private readonly FormattingData _formatting;

        /// <summary>
        /// Gets the formatting rendering settings.
        /// </summary>
        public FormattingData Formatting { get { ValidateView(); return _formatting; } }

        private readonly ProgressConfiguration _progress;

        /// <summary>
        /// Gets the configuration for progress rendering.
        /// </summary>
        public ProgressConfiguration Progress { get { ValidateView(); return _progress; } }

        private readonly ForegroundColor _foreground;

        /// <summary>
        /// Gets foreground colors.
        /// </summary>
        public ForegroundColor Foreground { get { ValidateView(); return _foreground; } }

        private readonly BackgroundColor _background;

        /// <summary>
        /// Gets background colors.
        /// </summary>
        public BackgroundColor Background { get { ValidateView(); return _background; } }

        private readonly FileInfoFormatting _fileInfo;

        /// <summary>
        /// Gets FileInfo colors.
        /// </summary>
        public FileInfoFormatting FileInfo { get { ValidateView(); return _fileInfo; } }

        private static readonly PSStyle s_psstyle = new PSStyle();

        private PSStyle() : this(null) { }

        internal PSStyle(PSScopedPropertyFacade scopedFacade)
        {
            _scopedFacade = scopedFacade;
            _formatting = new FormattingData(this);
            _progress   = new ProgressConfiguration(this);
            _foreground = new ForegroundColor(this);
            _background = new BackgroundColor(this);
            _fileInfo = new FileInfoFormatting(this);
        }

        internal static PSStyle CreateScoped(PSScopedPropertyFacade facade) => new(facade);

        internal void ValidateView() => _scopedFacade?.Resolve();

        private string GetAnsiCode(string code)
        {
            ValidateView();
            return code;
        }

        internal static OutputRendering DefaultOutputRendering => s_psstyle._outputRendering;

        internal static bool DefaultAutoSizeDefaultFormatting => s_psstyle._autoSizeDefaultFormatting;

        internal static string DefaultProgressStyle => s_psstyle._progress.DefaultStyle;

        internal static int DefaultProgressMaxWidth => s_psstyle._progress.DefaultMaxWidth;

        internal static bool DefaultProgressUseOSCIndicator => s_psstyle._progress.DefaultUseOSCIndicator;

        internal static string DefaultFormatAccent => s_psstyle._formatting.DefaultFormatAccent;

        internal static string DefaultTableHeader => s_psstyle._formatting.DefaultTableHeader;

        internal static string DefaultCustomTableHeaderLabel => s_psstyle._formatting.DefaultCustomTableHeaderLabel;

        internal static string DefaultErrorAccent => s_psstyle._formatting.DefaultErrorAccent;

        internal static string DefaultError => s_psstyle._formatting.DefaultError;

        internal static string DefaultWarning => s_psstyle._formatting.DefaultWarning;

        internal static string DefaultVerbose => s_psstyle._formatting.DefaultVerbose;

        internal static string DefaultDebug => s_psstyle._formatting.DefaultDebug;

        internal static string DefaultFeedbackName => s_psstyle._formatting.DefaultFeedbackName;

        internal static string DefaultFeedbackText => s_psstyle._formatting.DefaultFeedbackText;

        internal static string DefaultFeedbackAction => s_psstyle._formatting.DefaultFeedbackAction;

        internal static string DefaultFileInfoDirectory => s_psstyle._fileInfo.DefaultDirectory;

        internal static string DefaultFileInfoSymbolicLink => s_psstyle._fileInfo.DefaultSymbolicLink;

        internal static string DefaultFileInfoExecutable => s_psstyle._fileInfo.DefaultExecutable;

        internal static Dictionary<string, string> DefaultFileInfoExtensions => s_psstyle._fileInfo.DefaultExtensions;

        internal bool TryGetScoped<T>(ScopedSetting<T> setting, out T value)
        {
            if (_scopedFacade is not null)
            {
                value = _scopedFacade.GetScopedStyleSetting(setting);
                return true;
            }

            if (ExperimentalFeature.PSScopedProperties && LocalPipeline.GetExecutionContextFromTLS() is ExecutionContext context)
            {
                value = context.EngineSessionState.CurrentScope.GetScopedSettingValue(setting);
                return true;
            }

            value = default;
            return false;
        }

        private T GetScoped<T>(ScopedSetting<T> setting, T fallback)
            => TryGetScoped(setting, out T value) ? value : fallback;

        internal bool SetScoped<T>(ScopedSetting<T> setting, T value)
        {
            if (_scopedFacade is not null)
            {
                _scopedFacade.SetScopedStyleSetting(setting, value);
                return true;
            }

            if (ExperimentalFeature.PSScopedProperties && LocalPipeline.GetExecutionContextFromTLS() is ExecutionContext context)
            {
                context.EngineSessionState.CurrentScope.SetScopedSettingValue(setting, value, context.EngineSessionState);
                return true;
            }

            return false;
        }

        private static void SetText<T>(PSStyle owner, ScopedSetting<T> setting, string value, Action<string> localSetter)
        {
            value = ValidateNoContent(value);
            if (owner is null || !owner.SetScoped(setting, (T)(object)value)) localSetter(value);
        }

        internal static string ValidateNoContent(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            var decorartedString = new ValueStringDecorated(text);
            if (decorartedString.ContentLength > 0)
            {
                throw new ArgumentException(string.Format(PSStyleStrings.TextContainsContent, decorartedString.ToString(OutputRendering.PlainText)));
            }

            return text;
        }

        /// <summary>
        /// Gets singleton instance.
        /// </summary>
        public static PSStyle Instance
        {
            get
            {
                return s_psstyle;
            }
        }

        /// <summary>
        /// The map of background console colors to escape sequences.
        /// </summary>
        private static readonly string[] BackgroundColorMap =
            {
                "\x1b[40m", // Black
                "\x1b[44m", // DarkBlue
                "\x1b[42m", // DarkGreen
                "\x1b[46m", // DarkCyan
                "\x1b[41m", // DarkRed
                "\x1b[45m", // DarkMagenta
                "\x1b[43m", // DarkYellow
                "\x1b[47m", // Gray
                "\x1b[100m", // DarkGray
                "\x1b[104m", // Blue
                "\x1b[102m", // Green
                "\x1b[106m", // Cyan
                "\x1b[101m", // Red
                "\x1b[105m", // Magenta
                "\x1b[103m", // Yellow
                "\x1b[107m", // White
            };

        /// <summary>
        /// The map of foreground console colors to escape sequences.
        /// </summary>
        private static readonly string[] ForegroundColorMap =
            {
                "\x1b[30m", // Black
                "\x1b[34m", // DarkBlue
                "\x1b[32m", // DarkGreen
                "\x1b[36m", // DarkCyan
                "\x1b[31m", // DarkRed
                "\x1b[35m", // DarkMagenta
                "\x1b[33m", // DarkYellow
                "\x1b[37m", // Gray
                "\x1b[90m", // DarkGray
                "\x1b[94m", // Blue
                "\x1b[92m", // Green
                "\x1b[96m", // Cyan
                "\x1b[91m", // Red
                "\x1b[95m", // Magenta
                "\x1b[93m", // Yellow
                "\x1b[97m", // White
            };

        /// <summary>
        /// Return the VT escape sequence for a ConsoleColor.
        /// </summary>
        /// <param name="color">The <see cref="ConsoleColor"/> to be mapped from.</param>
        /// <param name="isBackground">Whether or not it's a background color.</param>
        /// <returns>The VT escape sequence representing the color.</returns>
        internal static string MapColorToEscapeSequence(ConsoleColor color, bool isBackground)
        {
            int index = (int)color;
            if (index < 0 || index >= ForegroundColorMap.Length)
            {
                throw new ArgumentOutOfRangeException(paramName: nameof(color));
            }

            return (isBackground ? BackgroundColorMap : ForegroundColorMap)[index];
        }

        /// <summary>
        /// Return the VT escape sequence for a foreground color.
        /// </summary>
        /// <param name="foregroundColor">The foreground color to be mapped from.</param>
        /// <returns>The VT escape sequence representing the foreground color.</returns>
        public static string MapForegroundColorToEscapeSequence(ConsoleColor foregroundColor)
            => MapColorToEscapeSequence(foregroundColor, isBackground: false);

        /// <summary>
        /// Return the VT escape sequence for a background color.
        /// </summary>
        /// <param name="backgroundColor">The background color to be mapped from.</param>
        /// <returns>The VT escape sequence representing the background color.</returns>
        public static string MapBackgroundColorToEscapeSequence(ConsoleColor backgroundColor)
            => MapColorToEscapeSequence(backgroundColor, isBackground: true);

        /// <summary>
        /// Return the VT escape sequence for a pair of foreground and background colors.
        /// </summary>
        /// <param name="foregroundColor">The foreground color of the color pair.</param>
        /// <param name="backgroundColor">The background color of the color pair.</param>
        /// <returns>The VT escape sequence representing the foreground and background color pair.</returns>
        public static string MapColorPairToEscapeSequence(ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
            int foreIndex = (int)foregroundColor;
            int backIndex = (int)backgroundColor;

            if (foreIndex < 0 || foreIndex >= ForegroundColorMap.Length)
            {
                throw new ArgumentOutOfRangeException(paramName: nameof(foregroundColor));
            }

            if (backIndex < 0 || backIndex >= BackgroundColorMap.Length)
            {
                throw new ArgumentOutOfRangeException(paramName: nameof(backgroundColor));
            }

            string foreground = ForegroundColorMap[foreIndex];
            string background = BackgroundColorMap[backIndex];

            return string.Concat(
                foreground.AsSpan(start: 0, length: foreground.Length - 1),
                ";".AsSpan(),
                background.AsSpan(start: 2));
        }
    }

    #endregion PSStyle
}
