// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation;

internal static partial class ScopedProperties
{
    internal static readonly ScopedSetting<OutputRendering> StyleOutputRenderingSetting = new(
        "Style.OutputRendering", PSStyle.DefaultOutputRendering, validate: ValidateStyleEnum, defaultFactory: () => PSStyle.DefaultOutputRendering);

    internal static readonly ScopedSetting<bool> StyleAutoSizeDefaultFormattingSetting = new(
        "Style.AutoSizeDefaultFormatting", PSStyle.DefaultAutoSizeDefaultFormatting, defaultFactory: () => PSStyle.DefaultAutoSizeDefaultFormatting);

    internal static readonly ScopedSetting<string> StyleProgressStyleSetting = new(
        "Style.Progress.Style", PSStyle.DefaultProgressStyle, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultProgressStyle);

    internal static readonly ScopedSetting<int> StyleProgressMaxWidthSetting = new(
        "Style.Progress.MaxWidth", PSStyle.DefaultProgressMaxWidth, validate: ValidateProgressWidth, defaultFactory: () => PSStyle.DefaultProgressMaxWidth);

    internal static readonly ScopedSetting<bool> StyleProgressUseOSCIndicatorSetting = new(
        "Style.Progress.UseOSCIndicator", PSStyle.DefaultProgressUseOSCIndicator, defaultFactory: () => PSStyle.DefaultProgressUseOSCIndicator);

    internal static readonly ScopedSetting<string> StyleFormattingFormatAccentSetting = new(
        "Style.Formatting.FormatAccent", PSStyle.DefaultFormatAccent, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultFormatAccent);

    internal static readonly ScopedSetting<string> StyleFormattingTableHeaderSetting = new(
        "Style.Formatting.TableHeader", PSStyle.DefaultTableHeader, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultTableHeader);

    internal static readonly ScopedSetting<string> StyleFormattingCustomTableHeaderLabelSetting = new(
        "Style.Formatting.CustomTableHeaderLabel", PSStyle.DefaultCustomTableHeaderLabel, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultCustomTableHeaderLabel);

    internal static readonly ScopedSetting<string> StyleFormattingErrorAccentSetting = new(
        "Style.Formatting.ErrorAccent", PSStyle.DefaultErrorAccent, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultErrorAccent);

    internal static readonly ScopedSetting<string> StyleFormattingErrorSetting = new(
        "Style.Formatting.Error", PSStyle.DefaultError, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultError);

    internal static readonly ScopedSetting<string> StyleFormattingWarningSetting = new(
        "Style.Formatting.Warning", PSStyle.DefaultWarning, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultWarning);

    internal static readonly ScopedSetting<string> StyleFormattingVerboseSetting = new(
        "Style.Formatting.Verbose", PSStyle.DefaultVerbose, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultVerbose);

    internal static readonly ScopedSetting<string> StyleFormattingDebugSetting = new(
        "Style.Formatting.Debug", PSStyle.DefaultDebug, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultDebug);

    internal static readonly ScopedSetting<string> StyleFormattingFeedbackNameSetting = new(
        "Style.Formatting.FeedbackName", PSStyle.DefaultFeedbackName, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultFeedbackName);

    internal static readonly ScopedSetting<string> StyleFormattingFeedbackTextSetting = new(
        "Style.Formatting.FeedbackText", PSStyle.DefaultFeedbackText, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultFeedbackText);

    internal static readonly ScopedSetting<string> StyleFormattingFeedbackActionSetting = new(
        "Style.Formatting.FeedbackAction", PSStyle.DefaultFeedbackAction, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultFeedbackAction);

    internal static readonly ScopedSetting<string> StyleFileInfoDirectorySetting = new(
        "Style.FileInfo.Directory", PSStyle.DefaultFileInfoDirectory, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultFileInfoDirectory);

    internal static readonly ScopedSetting<string> StyleFileInfoSymbolicLinkSetting = new(
        "Style.FileInfo.SymbolicLink", PSStyle.DefaultFileInfoSymbolicLink, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultFileInfoSymbolicLink);

    internal static readonly ScopedSetting<string> StyleFileInfoExecutableSetting = new(
        "Style.FileInfo.Executable", PSStyle.DefaultFileInfoExecutable, validate: value => PSStyle.ValidateNoContent(value), defaultFactory: () => PSStyle.DefaultFileInfoExecutable);

    internal static readonly ScopedSetting<Dictionary<string, string>> StyleFileInfoExtensionsSetting = new(
        "Style.FileInfo.Extension", PSStyle.DefaultFileInfoExtensions, defaultFactory: () => PSStyle.DefaultFileInfoExtensions);

    internal static readonly ScopedSetting[] StyleSettings =
    [
        StyleOutputRenderingSetting,
        StyleAutoSizeDefaultFormattingSetting,
        StyleProgressStyleSetting,
        StyleProgressMaxWidthSetting,
        StyleProgressUseOSCIndicatorSetting,
        StyleFormattingFormatAccentSetting,
        StyleFormattingTableHeaderSetting,
        StyleFormattingCustomTableHeaderLabelSetting,
        StyleFormattingErrorAccentSetting,
        StyleFormattingErrorSetting,
        StyleFormattingWarningSetting,
        StyleFormattingVerboseSetting,
        StyleFormattingDebugSetting,
        StyleFormattingFeedbackNameSetting,
        StyleFormattingFeedbackTextSetting,
        StyleFormattingFeedbackActionSetting,
        StyleFileInfoDirectorySetting,
        StyleFileInfoSymbolicLinkSetting,
        StyleFileInfoExecutableSetting,
        StyleFileInfoExtensionsSetting,
    ];

    private static void ValidateProgressWidth(int value)
    {
        if (value < 18)
        {
            throw new ArgumentOutOfRangeException("MaxWidth", PSStyleStrings.ProgressWidthTooSmall);
        }
    }

    private static void ValidateStyleEnum<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            LanguagePrimitives.EnumSingleTypeConverter.ThrowForUndefinedEnum("UndefinedIntegerToEnum", value, typeof(TEnum));
        }
    }
}
