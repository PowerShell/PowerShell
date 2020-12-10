// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    #region OutputRendering
    /// <summary>
    /// Defines the options for output rendering.
    /// </summary>
    public enum OutputRendering
    {
        /// <summary>Automatic by PowerShell.</summary>
        Automatic = 0,

        /// <summary>Render as plaintext.</summary>
        PlainText = 1,

        /// <summary>Render as ANSI.</summary>
        Ansi = 2,

        /// <summary>Render ANSI only to host.</summary>
        Host = 3,
    }
    #endregion OutputRendering

    #region PSStyle
    /// <summary>
    /// Contains configuration for how PowerShell renders text.
    /// </summary>
    public class PSStyle
    {
        /// <summary>
        /// Contains foreground colors.
        /// </summary>
        public class ForegroundColor
        {
            /// <summary>
            /// Gets the color black.
            /// </summary>
            public string Black { get; } = "\x1b[30m";

            /// <summary>
            /// Gets the color blue.
            /// </summary>
            public string Blue { get; } = "\x1b[34m";

            /// <summary>
            /// Gets the color cyan.
            /// </summary>
            public string Cyan { get; } = "\x1b[36m";

            /// <summary>
            /// Gets the color dark gray.
            /// </summary>
            public string DarkGray { get; } = "\x1b[90m";

            /// <summary>
            /// Gets the color green.
            /// </summary>
            public string Green { get; } = "\x1b[32m";

            /// <summary>
            /// Gets the color light blue.
            /// </summary>
            public string LightBlue { get; } = "\x1b[94m";

            /// <summary>
            /// Gets the color light cyan.
            /// </summary>
            public string LightCyan { get; } = "\x1b[96m";

            /// <summary>
            /// Gets the color light gray.
            /// </summary>
            public string LightGray { get; } = "\x1b[97m";

            /// <summary>
            /// Gets the color light green.
            /// </summary>
            public string LightGreen { get; } = "\x1b[92m";

            /// <summary>
            /// Gets the color light magenta.
            /// </summary>
            public string LightMagenta { get; } = "\x1b[95m";

            /// <summary>
            /// Gets the color light red.
            /// </summary>
            public string LightRed { get; } = "\x1b[91m";

            /// <summary>
            /// Gets the color light yellow.
            /// </summary>
            public string LightYellow { get; } = "\x1b[93m";

            /// <summary>
            /// Gets the color magenta.
            /// </summary>
            public string Magenta { get; } = "\x1b[35m";

            /// <summary>
            /// Gets the color read.
            /// </summary>
            public string Red { get; } = "\x1b[31m";

            /// <summary>
            /// Gets the color white.
            /// </summary>
            public string White { get; } = "\x1b[37m";

            /// <summary>
            /// Gets the color yellow.
            /// </summary>
            public string Yellow { get; } = "\x1b[33m";

            /// <summary>
            /// Set as RGB (Red, Green, Blue).
            /// </summary>
            /// <param name="red">Byte value representing red.</param>
            /// <param name="green">Byte value representing green.</param>
            /// <param name="blue">Byte value representing blue.</param>
            /// <returns>String representing ANSI code for RGB value.</returns>
            public string FromRgb(byte red, byte green, byte blue)
            {
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
        }

        /// <summary>
        /// Contains background colors.
        /// </summary>
        public class BackgroundColor
        {
            /// <summary>
            /// Gets the color black.
            /// </summary>
            public string Black { get; } = "\x1b[40m";

            /// <summary>
            /// Gets the color blue.
            /// </summary>
            public string Blue { get; } = "\x1b[44m";

            /// <summary>
            /// Gets the color cyan.
            /// </summary>
            public string Cyan { get; } = "\x1b[46m";

            /// <summary>
            /// Gets the color dark gray.
            /// </summary>
            public string DarkGray { get; } = "\x1b[100m";

            /// <summary>
            /// Gets the color green.
            /// </summary>
            public string Green { get; } = "\x1b[42m";

            /// <summary>
            /// Gets the color light blue.
            /// </summary>
            public string LightBlue { get; } = "\x1b[104m";

            /// <summary>
            /// Gets the color light cyan.
            /// </summary>
            public string LightCyan { get; } = "\x1b[106m";

            /// <summary>
            /// Gets the color light gray.
            /// </summary>
            public string LightGray { get; } = "\x1b[107m";

            /// <summary>
            /// Gets the color light green.
            /// </summary>
            public string LightGreen { get; } = "\x1b[102m";

            /// <summary>
            /// Gets the color light magenta.
            /// </summary>
            public string LightMagenta { get; } = "\x1b[105m";

            /// <summary>
            /// Gets the color light red.
            /// </summary>
            public string LightRed { get; } = "\x1b[101m";

            /// <summary>
            /// Gets the color light yellow.
            /// </summary>
            public string LightYellow { get; } = "\x1b[103m";

            /// <summary>
            /// Gets the color magenta.
            /// </summary>
            public string Magenta { get; } = "\x1b[45m";

            /// <summary>
            /// Gets the color read.
            /// </summary>
            public string Red { get; } = "\x1b[41m";

            /// <summary>
            /// Gets the color white.
            /// </summary>
            public string White { get; } = "\x1b[47m";

            /// <summary>
            /// Gets the color yellow.
            /// </summary>
            public string Yellow { get; } = "\x1b[43m";

            /// <summary>
            /// The color set as RGB (Red, Green, Blue).
            /// </summary>
            /// <param name="red">Byte value representing red.</param>
            /// <param name="green">Byte value representing green.</param>
            /// <param name="blue">Byte value representing blue.</param>
            /// <returns>String representing ANSI code for RGB value.</returns>
            public string FromRgb(byte red, byte green, byte blue)
            {
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
        }

        /// <summary>
        /// Contains formatting styles for steams and objects.
        /// </summary>
        public class FormattingData
        {
            /// <summary>
            /// Gets or sets the accent style for formatting.
            /// </summary>
            public string FormatAccent { get; set; } = "\x1b[32;1m";

            /// <summary>
            /// Gets or sets the accent style for errors.
            /// </summary>
            public string ErrorAccent { get; set; } = "\x1b[36;1m";

            /// <summary>
            /// Gets or sets the style for error messages.
            /// </summary>
            public string Error { get; set; } = "\x1b[31;1m";

            /// <summary>
            /// Gets or sets the style for warning messages.
            /// </summary>
            public string Warning { get; set; } = "\x1b[33;1m";

            /// <summary>
            /// Gets or sets the style for verbose messages.
            /// </summary>
            public string Verbose { get; set; } = "\x1b[33;1m";

            /// <summary>
            /// Gets or sets the style for debug messages.
            /// </summary>
            public string Debug { get; set; } = "\x1b[33;1m";
        }

        private static OutputRendering s_outputRendering = OutputRendering.Automatic;        

        /// <summary>
        /// Gets or sets the rendering mode for output.
        /// </summary>
        public OutputRendering OutputRendering
        {
            get
            {
                return s_outputRendering;
            }

            set
            {
                s_outputRendering = value;
            }
        }

        /// <summary>
        /// Gets value to turn off all attributes.
        /// </summary>
        public string Reset { get; } = "\x1b[0m";

        /// <summary>
        /// Gets value to turn off blink.
        /// </summary>
        public string BlinkOff { get; } = "\x1b[5m";

        /// <summary>
        /// Gets value to turn on blink.
        /// </summary>
        public string Blink { get; } = "\x1b[25m";

        /// <summary>
        /// Gets value to turn off bold.
        /// </summary>
        public string BoldOff { get; } = "\x1b[22m";

        /// <summary>
        /// Gets value to turn on blink.
        /// </summary>
        public string Bold { get; } = "\x1b[1m";

        /// <summary>
        /// Gets value to turn on hidden.
        /// </summary>
        public string Hidden { get; } = "\x1b[8m";

        /// <summary>
        /// Gets value to turn off hidden.
        /// </summary>
        public string HiddenOff { get; } = "\x1b[28m";

        /// <summary>
        /// Gets value to turn on reverse.
        /// </summary>
        public string Reverse { get; } = "\x1b[7m";

        /// <summary>
        /// Gets value to turn off reverse.
        /// </summary>
        public string ReverseOff { get; } = "\x1b[27m";

        /// <summary>
        /// Gets value to turn off standout.
        /// </summary>
        public string ItalicOff { get; } = "\x1b[23m";

        /// <summary>
        /// Gets value to turn on standout.
        /// </summary>
        public string Italic { get; } = "\x1b[3m";

        /// <summary>
        /// Gets value to turn off underlined.
        /// </summary>
        public string UnderlineOff { get; } = "\x1b[24m";

        /// <summary>
        /// Gets value to turn on underlined.
        /// </summary>
        public string Underline { get; } = "\x1b[4m";

        /// <summary>
        /// Gets the formatting rendering settings.
        /// </summary>
        public FormattingData Formatting { get; }

        /// <summary>
        /// Gets foreground colors.
        /// </summary>
        public ForegroundColor Foreground { get; }

        /// <summary>
        /// Gets background colors.
        /// </summary>
        public BackgroundColor Background { get; }

        private static readonly PSStyle s_psstyle = new PSStyle();

        private PSStyle()
        {
            Formatting = new FormattingData();
            Foreground = new ForegroundColor();
            Background = new BackgroundColor();
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
    }
    #endregion PSStyle
}
