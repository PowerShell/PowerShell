// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;

#pragma warning disable 1634, 1691 // Stops compiler from warning about unknown warnings

namespace System.Management.Automation.Host
{
    #region Ancillary types.

    // I would have preferred to make these nested types within PSHostRawUserInterface, but that
    // is evidently discouraged by the .net design guidelines.

    /// <summary>
    /// Represents an (x,y) coordinate pair.
    /// </summary>
    public
    struct Coordinates
    {
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private int x;
        private int y;

        #endregion

        /// <summary>
        /// Gets and sets the X coordinate.
        /// </summary>
        public int X
        {
            get { return x; }

            set { x = value; }
        }

        /// <summary>
        /// Gets and sets the Y coordinate.
        /// </summary>
        public int Y
        {
            get { return y; }

            set { y = value; }
        }

        /// <summary>
        /// Initializes a new instance of the Coordinates class and defines the X and Y values.
        /// </summary>
        /// <param name="x">
        /// The X coordinate
        /// </param>
        /// <param name="y">
        /// The Y coordinate
        /// </param>
        public
        Coordinates(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Overrides <see cref="System.Object.ToString"/>
        /// </summary>
        /// <returns>
        /// "a,b" where a and b are the values of the X and Y properties.
        /// </returns>
        public override
        string
        ToString()
        {
            return string.Create(CultureInfo.InvariantCulture, $"{X},{Y}");
        }

        /// <summary>
        /// Overrides <see cref="System.Object.Equals(object)"/>
        /// </summary>
        /// <param name="obj">
        /// object to be compared for equality.
        /// </param>
        /// <returns>
        /// True if <paramref name="objB"/> is Coordinates and its X and Y values are the same as those of this instance,
        /// false if not.
        /// </returns>
        public override
        bool
        Equals(object obj)
        {
            bool result = false;

            if (obj is Coordinates)
            {
                result = this == ((Coordinates)obj);
            }

            return result;
        }

        /// <summary>
        /// Overrides <see cref="System.Object.GetHashCode"/>
        /// </summary>
        /// <returns>
        /// Hash code for this instance.
        /// </returns>
        public override
        int
        GetHashCode()
        {
            // idea: consider X the high-order part of a 64-bit in, and Y the lower order half.  Then use the int64.GetHashCode.

            UInt64 i64 = 0;

            if (X < 0)
            {
                if (X == Int32.MinValue)
                {
                    // add one and invert to avoid an overflow.

                    i64 = (UInt64)(-1 * (X + 1));
                }
                else
                {
                    i64 = (UInt64)(-X);
                }
            }
            else
            {
                i64 = (UInt64)X;
            }

            // rotate 32 bits to the left.

            i64 *= 0x100000000U;

            // mask in Y

            if (Y < 0)
            {
                if (Y == Int32.MinValue)
                {
                    i64 += (UInt64)(-1 * (Y + 1));
                }
                else
                {
                    i64 += (UInt64)(-Y);
                }
            }
            else
            {
                i64 += (UInt64)Y;
            }

            int result = i64.GetHashCode();

            return result;
        }

        /// <summary>
        /// Compares two instances for equality.
        /// </summary>
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if the respective X and Y values are the same, false otherwise.
        /// </returns>
        public static
        bool
        operator ==(Coordinates first, Coordinates second)
        {
            bool result = first.X == second.X && first.Y == second.Y;

            return result;
        }

        /// <summary>
        /// Compares two instances for inequality.
        /// </summary>
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if any of the respective either X or Y field is not the same, false otherwise.
        /// </returns>
        public static
        bool
        operator !=(Coordinates first, Coordinates second)
        {
            return !(first == second);
        }
    }

    /// <summary>
    /// Represents a width and height pair.
    /// </summary>
    public
    struct Size
    {
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private int width;
        private int height;

        #endregion

        /// <summary>
        /// Gets and sets the Width.
        /// </summary>
        public int Width
        {
            get { return width; }

            set { width = value; }
        }

        /// <summary>
        /// Gets and sets the Height.
        /// </summary>
        public int Height
        {
            get { return height; }

            set { height = value; }
        }

        /// <summary>
        /// Initialize a new instance of the Size class and defines the Width and Height values.
        /// </summary>
        /// <param name="width">
        /// The Width
        /// </param>
        /// <param name="height">
        /// The Height
        /// </param>
        public
        Size(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        /// <summary>
        /// Overloads <see cref="System.Object.ToString"/>
        /// </summary>
        /// <returns>
        /// "a,b" where a and b are the values of the Width and Height properties.
        /// </returns>
        public override
        string
        ToString()
        {
            return string.Create(CultureInfo.InvariantCulture, $"{Width},{Height}");
        }

        /// <summary>
        /// Overrides <see cref="System.Object.Equals(object)"/>
        /// </summary>
        /// <param name="obj">
        /// object to be compared for equality.
        /// </param>
        /// <returns>
        /// True if <paramref name="obj"/> is Size and its Width and Height values are the same as those of this instance,
        /// false if not.
        /// </returns>
        public override
        bool
        Equals(object obj)
        {
            bool result = false;

            if (obj is Size)
            {
                result = this == ((Size)obj);
            }

            return result;
        }

        /// <summary>
        /// Overrides <see cref="System.Object.GetHashCode"/>
        /// </summary>
        /// <returns>
        /// Hash code for this instance.
        /// <!--
        /// consider Width the high-order part of a 64-bit in, and
        /// Height the lower order half.  Then use the int64.GetHashCode.-->
        /// </returns>
        public override
        int
        GetHashCode()
        {
            // idea: consider Width the high-order part of a 64-bit in, and Height the lower order half.  Then use the int64.GetHashCode.

            UInt64 i64 = 0;

            if (Width < 0)
            {
                if (Width == Int32.MinValue)
                {
                    // add one and invert to avoid an overflow.

                    i64 = (UInt64)(-1 * (Width + 1));
                }
                else
                {
                    i64 = (UInt64)(-Width);
                }
            }
            else
            {
                i64 = (UInt64)Width;
            }

            // rotate 32 bits to the left.

            i64 *= 0x100000000U;

            // mask in Height

            if (Height < 0)
            {
                if (Height == Int32.MinValue)
                {
                    i64 += (UInt64)(-1 * (Height + 1));
                }
                else
                {
                    i64 += (UInt64)(-Height);
                }
            }
            else
            {
                i64 += (UInt64)Height;
            }

            int result = i64.GetHashCode();

            return result;
        }

        /// <summary>
        /// Compares two instances for equality.
        /// </summary>
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if the respective Width and Height fields are the same, false otherwise.
        /// </returns>
        public static
        bool
        operator ==(Size first, Size second)
        {
            bool result = first.Width == second.Width && first.Height == second.Height;

            return result;
        }

        /// <summary>
        /// Compares two instances for inequality.
        /// </summary>
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if any of the respective Width and Height fields are not the same, false otherwise.
        /// </returns>
        public static
        bool
        operator !=(Size first, Size second)
        {
            return !(first == second);
        }
    }

    /// <summary>
    /// Governs the behavior of <see cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey()"/>
    /// and <see cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey(System.Management.Automation.Host.ReadKeyOptions)"/>
    /// </summary>
    [Flags]
    public
    enum
    ReadKeyOptions
    {
        /// <summary>
        /// Allow Ctrl-C to be processed as a keystroke, as opposed to causing a break event.
        /// </summary>
        AllowCtrlC = 0x0001,

        /// <summary>
        /// Do not display the character for the key in the window when pressed.
        /// </summary>
        NoEcho = 0x0002,

        /// <summary>
        /// Include key down events.  Either one of IncludeKeyDown and IncludeKeyUp or both must be specified.
        /// </summary>
        IncludeKeyDown = 0x0004,

        /// <summary>
        /// Include key up events.  Either one of IncludeKeyDown and IncludeKeyUp or both must be specified.
        /// </summary>
        IncludeKeyUp = 0x0008
    }

    /// <summary>
    /// Defines the states of Control Key.
    /// </summary>
    [Flags]
    public
    enum ControlKeyStates
    {
        /// <summary>
        /// The right alt key is pressed.
        /// </summary>
        RightAltPressed = 0x0001,

        /// <summary>
        /// The left alt key is pressed.
        /// </summary>
        LeftAltPressed = 0x0002,

        /// <summary>
        /// The right ctrl key is pressed.
        /// </summary>
        RightCtrlPressed = 0x0004,

        /// <summary>
        /// The left ctrl key is pressed.
        /// </summary>
        LeftCtrlPressed = 0x0008,

        /// <summary>
        /// The shift key is pressed.
        /// </summary>
        ShiftPressed = 0x0010,

        /// <summary>
        /// The numlock light is on.
        /// </summary>
        NumLockOn = 0x0020,

        /// <summary>
        /// The scrolllock light is on.
        /// </summary>
        ScrollLockOn = 0x0040,

        /// <summary>
        /// The capslock light is on.
        /// </summary>
        CapsLockOn = 0x0080,

        /// <summary>
        /// The key is enhanced.
        /// </summary>
        EnhancedKey = 0x0100
    }

    /// <summary>
    /// Represents information of a keystroke.
    /// </summary>
    public
    struct KeyInfo
    {
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private int virtualKeyCode;
        private char character;
        private ControlKeyStates controlKeyState;
        private bool keyDown;

        #endregion

        /// <summary>
        /// Gets and set device-independent key.
        /// </summary>
        public int VirtualKeyCode
        {
            get { return virtualKeyCode; }

            set { virtualKeyCode = value; }
        }

        /// <summary>
        /// Gets and set unicode Character of the key.
        /// </summary>
        public char Character
        {
            get { return character; }

            set { character = value; }
        }

        /// <summary>
        /// State of the control keys.
        /// </summary>
        public ControlKeyStates ControlKeyState
        {
            get { return controlKeyState; }

            set { controlKeyState = value; }
        }

        /// <summary>
        /// Gets and set the status of whether this instance is generated by a key pressed or released.
        /// </summary>
        public bool KeyDown
        {
            get { return keyDown; }

            set { keyDown = value; }
        }

        /// <summary>
        /// Initialize a new instance of the KeyInfo class and defines the VirtualKeyCode,
        /// Character, ControlKeyState and KeyDown values.
        /// </summary>
        /// <param name="virtualKeyCode">
        /// The virtual key code
        /// </param>
        /// <param name="ch">
        /// The character
        /// </param>
        /// <param name="controlKeyState">
        /// The control key state
        /// </param>
        /// <param name="keyDown">
        /// Whether the key is pressed or released
        /// </param>
        public
        KeyInfo
        (
            int virtualKeyCode,
            char ch,
            ControlKeyStates controlKeyState,
            bool keyDown
        )
        {
            this.virtualKeyCode = virtualKeyCode;
            this.character = ch;
            this.controlKeyState = controlKeyState;
            this.keyDown = keyDown;
        }

        /// <summary>
        /// Overloads <see cref="System.Object.ToString"/>
        /// </summary>
        /// <returns>
        /// "a,b,c,d" where a, b, c, and d are the values of the VirtualKeyCode, Character, ControlKeyState, and KeyDown properties.
        /// </returns>
        public override
        string
        ToString()
        {
            return string.Create(CultureInfo.InvariantCulture, $"{VirtualKeyCode},{Character},{ControlKeyState},{KeyDown}");
        }
        /// <summary>
        /// Overrides <see cref="System.Object.Equals(object)"/>
        /// </summary>
        /// <param name="obj">
        /// object to be compared for equality.
        /// </param>
        /// <returns>
        /// True if <paramref name="obj"/> is KeyInfo and its VirtualKeyCode, Character, ControlKeyState, and KeyDown values are the
        /// same as those of this instance, false if not.
        /// </returns>
        public override
        bool
        Equals(object obj)
        {
            bool result = false;

            if (obj is KeyInfo)
            {
                result = this == ((KeyInfo)obj);
            }

            return result;
        }

        /// <summary>
        /// Overrides <see cref="System.Object.GetHashCode"/>
        /// </summary>
        /// <returns>
        /// Hash code for this instance.
        /// <!--consider KeyDown (true == 1, false == 0) the highest-order nibble,
        ///                ControlKeyState the second to fourth highest-order nibbles
        ///                VirtualKeyCode the lower-order nibbles of a 32-bit int,
        ///       Then use the UInt32.GetHashCode.-->
        /// </returns>
        public override
        int
        GetHashCode()
        {
            // idea: consider KeyDown (true == 1, false == 0) the highest-order nibble,
            //                ControlKeyState the second to fourth highest-order nibbles
            //                VirtualKeyCode the lower-order nibbles of a 32-bit int,
            //       Then use the UInt32.GetHashCode.

            UInt32 i32 = KeyDown ? 0x10000000U : 0;

            // mask in ControlKeyState
            i32 |= ((uint)ControlKeyState) << 16;

            // mask in the VirtualKeyCode
            i32 |= (UInt32)VirtualKeyCode;

            return i32.GetHashCode();
        }

        /// <summary>
        /// Compares two instances for equality.
        /// </summary>
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if the respective Character, ControlKeyStates , KeyDown, and VirtualKeyCode fields
        /// are the same, false otherwise.
        /// </returns>
        /// <exception/>
        public static
        bool
        operator ==(KeyInfo first, KeyInfo second)
        {
            bool result = first.Character == second.Character && first.ControlKeyState == second.ControlKeyState &&
                          first.KeyDown == second.KeyDown && first.VirtualKeyCode == second.VirtualKeyCode;

            return result;
        }

        /// <summary>
        /// Compares two instances for inequality.
        /// </summary>
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if any of the respective Character, ControlKeyStates , KeyDown, or VirtualKeyCode fields
        /// are the different, false otherwise.
        /// </returns>
        /// <exception/>
        public static
        bool
        operator !=(KeyInfo first, KeyInfo second)
        {
            return !(first == second);
        }
    }

    /// <summary>
    /// Represents a rectangular region of the screen.
    /// <!--We use this structure instead of System.Drawing.Rectangle because S.D.R
    /// is way overkill and would bring in another assembly.-->
    /// </summary>
    public
    struct Rectangle
    {
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private int left;
        private int top;
        private int right;
        private int bottom;

        #endregion

        /// <summary>
        /// Gets and sets the left side of the rectangle.
        /// </summary>
        public int Left
        {
            get { return left; }

            set { left = value; }
        }

        /// <summary>
        /// Gets and sets the top of the rectangle.
        /// </summary>
        public int Top
        {
            get { return top; }

            set { top = value; }
        }

        /// <summary>
        /// Gets and sets the right side of the rectangle.
        /// </summary>
        public int Right
        {
            get { return right; }

            set { right = value; }
        }

        /// <summary>
        /// Gets and sets the bottom of the rectangle.
        /// </summary>
        public int Bottom
        {
            get { return bottom; }

            set { bottom = value; }
        }

        /// <summary>
        /// Initialize a new instance of the Rectangle class and defines the Left, Top, Right, and Bottom values.
        /// </summary>
        /// <param name="left">
        /// The left side of the rectangle
        /// </param>
        /// <param name="top">
        /// The top of the rectangle
        /// </param>
        /// <param name="right">
        /// The right side of the rectangle
        /// </param>
        /// <param name="bottom">
        /// The bottom of the rectangle
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="right"/> is less than <paramref name="left"/>;
        /// <paramref name="bottom"/> is less than <paramref name="top"/>
        /// </exception>
        public
        Rectangle(int left, int top, int right, int bottom)
        {
            if (right < left)
            {
                // "right" and "left" are not localizable
                throw PSTraceSource.NewArgumentException(nameof(right), MshHostRawUserInterfaceStrings.LessThanErrorTemplate, "right", "left");
            }

            if (bottom < top)
            {
                // "bottom" and "top" are not localizable
                throw PSTraceSource.NewArgumentException(nameof(bottom), MshHostRawUserInterfaceStrings.LessThanErrorTemplate, "bottom", "top");
            }

            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }

        /// <summary>
        /// Initializes a new instance of the Rectangle class and defines the Left, Top, Right, and Bottom values
        /// by <paramref name="upperLeft"/>, the upper left corner and <paramref name="lowerRight"/>, the lower
        /// right corner.
        /// <!--
        /// Added based on feedback from review with BCL PM.
        /// -->
        /// </summary>
        /// <param name="upperLeft">
        /// The Coordinates of the upper left corner of the Rectangle
        /// </param>
        /// <param name="lowerRight">
        /// The Coordinates of the lower right corner of the Rectangle
        /// </param>
        /// <exception/>
        public
        Rectangle(Coordinates upperLeft, Coordinates lowerRight)
            : this(upperLeft.X, upperLeft.Y, lowerRight.X, lowerRight.Y)
        {
        }

        /// <summary>
        /// Overloads <see cref="System.Object.ToString"/>
        /// </summary>
        /// <returns>
        /// "a,b ; c,d" where a, b, c, and d are values of the Left, Top, Right, and Bottom properties.
        /// </returns>
        public override
        string
        ToString()
        {
            return string.Create(CultureInfo.InvariantCulture, $"{Left},{Top} ; {Right},{Bottom}");
        }

        /// <summary>
        /// Overrides <see cref="System.Object.Equals(object)"/>
        /// </summary>
        /// <param name="obj">
        /// object to be compared for equality.
        /// </param>
        /// <returns>
        /// True if <paramref name="obj"/> is Rectangle and its Left, Top, Right, and Bottom values are the same as those of this instance,
        /// false if not.
        /// </returns>
        public override
        bool
        Equals(object obj)
        {
            bool result = false;

            if (obj is Rectangle)
            {
                result = this == ((Rectangle)obj);
            }

            return result;
        }

        /// <summary>
        /// Overrides <see cref="System.Object.GetHashCode"/>
        /// </summary>
        /// <returns>
        /// Hash code for this instance.
        /// <!-- consider (Top XOR Bottom) the high-order part of a 64-bit int,
        ///                (Left XOR Right) the lower order half.  Then use the int64.GetHashCode.-->
        /// </returns>
        /// <exception/>
        public override
        int
        GetHashCode()
        {
            // idea: consider (Top XOR Bottom) the high-order part of a 64-bit int,
            //                (Left XOR Right) the lower order half.  Then use the int64.GetHashCode.

            UInt64 i64 = 0;

            int upper = Top ^ Bottom;
            if (upper < 0)
            {
                if (upper == Int32.MinValue)
                {
                    // add one and invert to avoid an overflow.

                    i64 = (UInt64)(-1 * (upper + 1));
                }
                else
                {
                    i64 = (UInt64)(-upper);
                }
            }
            else
            {
                i64 = (UInt64)upper;
            }

            // rotate 32 bits to the left.

            i64 *= 0x100000000U;

            // mask in lower

            int lower = Left ^ Right;
            if (lower < 0)
            {
                if (lower == Int32.MinValue)
                {
                    i64 += (UInt64)(-1 * (lower + 1));
                }
                else
                {
                    i64 += (UInt64)(-upper);
                }
            }
            else
            {
                i64 += (UInt64)lower;
            }

            int result = i64.GetHashCode();

            return result;
        }

        /// <summary>
        /// Compares two instances for equality.
        /// </summary>
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if the respective Top, Left, Bottom, and Right fields are the same, false otherwise.
        /// </returns>
        public static
        bool
        operator ==(Rectangle first, Rectangle second)
        {
            bool result = first.Top == second.Top && first.Left == second.Left &&
             first.Bottom == second.Bottom && first.Right == second.Right;

            return result;
        }

        /// <summary>
        /// Compares two instances for inequality.
        /// </summary>
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if any of the respective Top, Left, Bottom, and Right fields are not the same, false otherwise.
        /// </returns>
        /// <exception/>
        public static
        bool
        operator !=(Rectangle first, Rectangle second)
        {
            return !(first == second);
        }
    }

    /// <summary>
    /// Represents a character, a foregroundColor color, and background color.
    /// </summary>
    public
    struct BufferCell
    {
        #region DO NOT REMOVE OR RENAME THESE FIELDS - it will break remoting compatibility with Windows PowerShell

        private char character;
        private ConsoleColor foregroundColor;
        private ConsoleColor backgroundColor;
        private BufferCellType bufferCellType;

        #endregion

        /// <summary>
        /// Gets and sets the character value.
        /// </summary>
        public char Character
        {
            get { return character; }

            set { character = value; }
        }

        // we reuse System.ConsoleColor - it's in the core assembly, and I think it would be confusing to create another
        // essentially identical enum

        /// <summary>
        /// Gets and sets the foreground color.
        /// </summary>
        public ConsoleColor ForegroundColor
        {
            get { return foregroundColor; }

            set { foregroundColor = value; }
        }

        /// <summary>
        /// Gets and sets the background color.
        /// </summary>
        public ConsoleColor BackgroundColor
        {
            get { return backgroundColor; }

            set { backgroundColor = value; }
        }

        /// <summary>
        /// Gets and sets the type value.
        /// </summary>
        public BufferCellType BufferCellType
        {
            get { return bufferCellType; }

            set { bufferCellType = value; }
        }

        /// <summary>
        /// Initializes a new instance of the BufferCell class and defines the
        /// Character, ForegroundColor, BackgroundColor and Type values.
        /// </summary>
        /// <param name="character">
        /// The character in this BufferCell object
        /// </param>
        /// <param name="foreground">
        /// The foreground color of this BufferCell object
        /// </param>
        /// <param name="background">
        /// The foreground color of this BufferCell object
        /// </param>
        /// <param name="bufferCellType">
        /// The type of this BufferCell object
        /// </param>
        public
        BufferCell(char character, ConsoleColor foreground, ConsoleColor background, BufferCellType bufferCellType)
        {
            this.character = character;
            this.foregroundColor = foreground;
            this.backgroundColor = background;
            this.bufferCellType = bufferCellType;
        }

        /// <summary>
        /// Overloads <see cref="System.Object.ToString"/>
        /// </summary>
        /// <returns>
        /// "'a' b c d" where a, b, c, and d are the values of the Character, ForegroundColor, BackgroundColor, and Type properties.
        /// </returns>
        public override
        string
        ToString()
        {
            return string.Create(CultureInfo.InvariantCulture, $"'{Character}' {ForegroundColor} {BackgroundColor} {BufferCellType}");
        }

        /// <summary>
        /// Overrides <see cref="System.Object.Equals(object)"/>
        /// </summary>
        /// <param name="obj">
        /// object to be compared for equality.
        /// </param>
        /// <returns>
        /// True if <paramref name="obj"/> is BufferCell and its Character, ForegroundColor, BackgroundColor, and BufferCellType values
        /// are the same as those of this instance, false if not.
        /// </returns>
        public override
        bool
        Equals(object obj)
        {
            bool result = false;

            if (obj is BufferCell)
            {
                result = this == ((BufferCell)obj);
            }

            return result;
        }

        /// <summary>
        /// Overrides <see cref="System.Object.GetHashCode"/>
        /// <!-- consider (ForegroundColor XOR BackgroundColor) the high-order part of a 32-bit int,
        ///      and Character the lower order half.  Then use the int32.GetHashCode.-->
        /// </summary>
        /// <returns>
        /// Hash code for this instance.
        ///
        /// </returns>
        public override
        int
        GetHashCode()
        {
            // idea: consider (ForegroundColor XOR BackgroundColor) the high-order part of a 32-bit int,
            //                and Character the lower order half.  Then use the int32.GetHashCode.

            UInt32 i32 = ((uint)(ForegroundColor ^ BackgroundColor)) << 16;

            // mask in Height

            i32 |= (UInt16)Character;
            int result = i32.GetHashCode();

            return result;
        }

        /// <summary>
        /// Compares two instances for equality.
        /// </summary>
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if the respective Character, ForegroundColor, BackgroundColor, and BufferCellType values are the same, false otherwise.
        /// </returns>
        public static
        bool
        operator ==(BufferCell first, BufferCell second)
        {
            bool result = first.Character == second.Character &&
                          first.BackgroundColor == second.BackgroundColor &&
                          first.ForegroundColor == second.ForegroundColor &&
                          first.BufferCellType == second.BufferCellType;

            return result;
        }

        /// <summary>
        /// Compares two instances for inequality.
        /// </summary>
        /// <param name="first">
        /// The left side operand.
        /// </param>
        /// <param name="second">
        /// The right side operand.
        /// </param>
        /// <returns>
        /// true if any of the respective Character, ForegroundColor, BackgroundColor, and BufferCellType values are not the same,
        /// false otherwise.
        /// </returns>
        public static
        bool
        operator !=(BufferCell first, BufferCell second)
        {
            return !(first == second);
        }

        private const string StringsBaseName = "MshHostRawUserInterfaceStrings";
    }

    /// <summary>
    /// Defines three types of BufferCells to accommodate for hosts that use up to two cells
    /// to display a character in some languages such as Chinese and Japanese.
    /// </summary>
    public enum
    BufferCellType
    {
        /// <summary>
        /// Character occupies one BufferCell.
        /// </summary>
        Complete,

        /// <summary>
        /// Character occupies two BufferCells and this is the leading one.
        /// </summary>
        Leading,

        /// <summary>
        /// Preceded by a Leading BufferCell.
        /// </summary>
        Trailing
    }

    #endregion Ancillary types

    /// <summary>
    /// Defines the lowest-level user interface functions that an interactive application hosting PowerShell
    /// <see cref="System.Management.Automation.Runspaces.Runspace"/> can choose to implement if it wants to
    /// support any cmdlet that does character-mode interaction with the user.
    /// </summary>
    /// <remarks>
    /// It models an 2-dimensional grid of cells called a Buffer.  A buffer has a visible rectangular region, called a window.
    /// Each cell of the grid has a character, a foreground color, and a background color.  When the buffer has input focus, it
    /// shows a cursor positioned in one cell.  Keystrokes can be read from the buffer and optionally echoed at the current
    /// cursor position.
    /// </remarks>
    /// <seealso cref="System.Management.Automation.Host.PSHost"/>
    /// <seealso cref="System.Management.Automation.Host.PSHostUserInterface"/>
    public abstract
    class PSHostRawUserInterface
    {
        /// <summary>
        /// Protected constructor which does nothing.  Provided per .Net design guidelines section 4.3.1.
        /// </summary>
        protected
        PSHostRawUserInterface()
        {
            // do nothing
        }

        /// <summary>
        /// Gets or sets the color used to render characters on the screen buffer. Each character cell in the screen buffer can
        /// have a separate foreground color.
        /// </summary>
        /// <!--Design note: we separate Foreground and Background colors into separate properties rather than having a single
        /// property that is a ColorAttribute.  While a single property that takes a struct is consistent with all of our
        /// other properties that take structs (e.g. -Position, -Size), I anticipate that the more common use-case for color
        /// is to just change the foreground color.-->
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.BackgroundColor"/>
        public abstract
        ConsoleColor
        ForegroundColor
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the color used to render the background behind characters on the screen buffer.  Each character cell in
        /// the screen buffer can have a separate background color.
        /// </summary>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ForegroundColor"/>
        public abstract
        ConsoleColor
        BackgroundColor
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the cursor position in the screen buffer.  The view window always adjusts it's location over the screen
        /// buffer such that the cursor is always visible.
        /// </summary>
        /// <remarks>
        /// To write to the screen buffer without updating the cursor position, use
        /// <see cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/> or
        /// <see cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxPhysicalWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowPosition"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        public abstract
        Coordinates
        CursorPosition
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets position of the view window relative to the screen buffer, in characters. (0,0) is the upper left of the screen
        /// buffer.
        /// </summary>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxPhysicalWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.CursorPosition"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxWindowSize"/>
        public abstract
        Coordinates
        WindowPosition
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the cursor size as a percentage 0..100.
        /// </summary>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.CursorPosition"/>
        public abstract
        int
        CursorSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the current size of the screen buffer, measured in character cells.
        /// </summary>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxPhysicalWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.CursorPosition"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowPosition"/>
        public abstract
        Size
        BufferSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the current view window size, measured in character cells.  The window size cannot be larger than the
        /// dimensions returned by <see cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxPhysicalWindowSize"/>.
        /// </summary>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxPhysicalWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.BufferSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.CursorPosition"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowPosition"/>
        public abstract
        Size
        WindowSize
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the size of the largest window possible for the current buffer, current font, and current display hardware.
        /// The view window cannot be larger than the screen buffer or the current display (the display the window is rendered on).
        /// </summary>
        /// <value>
        /// The largest dimensions the window can be resized to without resizing the screen buffer.
        /// </value>
        /// <remarks>
        /// Always returns a value less than or equal to
        /// <see cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxPhysicalWindowSize"/>.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxPhysicalWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.BufferSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.CursorPosition"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowPosition"/>
        public abstract
        Size
        MaxWindowSize
        {
            get;
        }

        /// <summary>
        /// Gets the largest window possible for the current font and display hardware, ignoring the current buffer dimensions.  In
        /// other words, the dimensions of the largest window that could be rendered in the current display, if the buffer was
        /// at least as large.
        /// </summary>
        /// <remarks>
        /// To resize the window to this dimension, use <see cref="System.Management.Automation.Host.PSHostRawUserInterface.BufferSize"/>
        /// to first check and, if necessary, adjust, the screen buffer size.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.MaxWindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.BufferSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.CursorPosition"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowSize"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowPosition"/>
        public abstract
        Size
        MaxPhysicalWindowSize
        {
            get;
        }

        /// <summary>
        /// Reads a key stroke from the keyboard device, blocking until a keystroke is typed.
        /// Same as ReadKey(ReadKeyOptions.IncludeKeyDown)
        /// </summary>
        /// <returns>
        /// Key stroke when a key is pressed.
        /// </returns>
        /// <example>
        ///     <code>
        ///         $Host.UI.RawUI.ReadKey()
        ///     </code>
        /// </example>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey(ReadKeyOptions)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.FlushInputBuffer"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.KeyAvailable"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.WindowPosition"/>
        public
        KeyInfo
        ReadKey()
        {
            return ReadKey(ReadKeyOptions.IncludeKeyDown);
        }

        /// <summary>
        /// Reads a key stroke from the keyboard device, blocking until a keystroke is typed.
        /// Either one of ReadKeyOptions.IncludeKeyDown and ReadKeyOptions.IncludeKeyUp or both must be specified.
        /// </summary>
        /// <param name="options">
        /// A bit mask of the options to be used to read the keyboard. Constants defined by
        /// <see cref="System.Management.Automation.Host.ReadKeyOptions"/>
        /// </param>
        /// <returns>
        /// Key stroke depending on the value of <paramref name="options"/>.
        /// </returns>
        /// <exception cref="System.ArgumentException">
        /// Neither ReadKeyOptions.IncludeKeyDown nor ReadKeyOptions.IncludeKeyUp is specified.
        /// </exception>
        /// <example>
        ///     <code>
        ///         $option = [System.Management.Automation.Host.ReadKeyOptions]"IncludeKeyDown";
        ///         $host.UI.RawUI.ReadKey($option)
        ///     </code>
        /// </example>
        /// <seealso cref="System.Management.Automation.Host.ReadKeyOptions"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey()"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey(System.Management.Automation.Host.ReadKeyOptions)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.FlushInputBuffer"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.KeyAvailable"/>
        public abstract
        KeyInfo
        ReadKey(ReadKeyOptions options);

        /// <summary>
        /// Resets the keyboard input buffer.
        /// </summary>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey()"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey(System.Management.Automation.Host.ReadKeyOptions)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.KeyAvailable"/>
        public abstract
        void
        FlushInputBuffer();

        /// <summary>
        /// A non-blocking call to examine if a keystroke is waiting in the input buffer.
        /// </summary>
        /// <value>
        /// True if a keystroke is waiting in the input buffer, false if not.
        /// </value>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey()"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ReadKey(ReadKeyOptions)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.FlushInputBuffer"/>
        public abstract
        bool
        KeyAvailable
        {
            get;
        }

        /// <summary>
        /// Gets or sets the titlebar text of the current view window.
        /// </summary>
        public abstract
        string
        WindowTitle
        {
            get;
            set;
        }

        /// <summary>
        /// Copies the <see cref="System.Management.Automation.Host.BufferCell"/> array into the screen buffer at the
        /// given origin, clipping such that cells in the array that would fall outside the screen buffer are ignored.
        /// </summary>
        /// <param name="origin">
        /// The top left corner of the rectangular screen area to which <paramref name="contents"/> is copied.
        /// </param>
        /// <param name="contents">
        /// A rectangle of <see cref="System.Management.Automation.Host.BufferCell"/> objects to be copied to the
        /// screen buffer.
        /// </param>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public abstract
        void
        SetBufferContents(Coordinates origin, BufferCell[,] contents);

        /// <summary>
        /// Copies a given character to all of the character cells in the screen buffer with the indicated colors.
        /// </summary>
        /// <param name="rectangle">
        /// The rectangle on the screen buffer to which <paramref name="fill"/> is copied.
        /// If all elements are -1, the entire screen buffer will be copied with <paramref name="fill"/>.
        /// </param>
        /// <param name="fill">
        /// The character and attributes used to fill <paramref name="rectangle"/>.
        /// </param>
        /// <remarks>
        /// Provided for clearing regions -- less chatty than passing an array of cells.
        /// </remarks>
        /// <example>
        ///     <code>
        ///         using System;
        ///         using System.Management.Automation;
        ///         using System.Management.Automation.Host;
        ///         namespace Microsoft.Samples.Cmdlet
        ///         {
        ///             [Cmdlet("Clear","Screen")]
        ///             public class ClearScreen : PSCmdlet
        ///             {
        ///                 protected override void BeginProcessing()
        ///                 {
        ///                     Host.UI.RawUI.SetBufferContents(new Rectangle(-1, -1, -1, -1),
        ///                         new BufferCell(' ', Host.UI.RawUI.ForegroundColor, Host.UI.RawUI.BackgroundColor))
        ///                 }
        ///             }
        ///         }
        ///     </code>
        /// </example>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public abstract
        void
        SetBufferContents(Rectangle rectangle, BufferCell fill);

        /// <summary>
        /// Extracts a rectangular region of the screen buffer.
        /// </summary>
        /// <param name="rectangle">
        /// The rectangle on the screen buffer to extract.
        /// </param>
        /// <returns>
        /// An array of <see cref="System.Management.Automation.Host.BufferCell"/> objects extracted from
        /// the rectangular region of the screen buffer specified by <paramref name="rectangle"/>
        /// </returns>
        /// <remarks>
        /// If the rectangle is completely outside of the screen buffer, a BufferCell array of zero rows and column will be
        /// returned.
        ///
        /// If the rectangle is partially outside of the screen buffer, the area where the screen buffer and rectangle overlap
        /// will be read and returned. The size of the returned array is the same as that of r. Each BufferCell in the
        /// non-overlapping area of this array is set as follows:
        ///
        /// Character is the space (' ')
        /// ForegroundColor to the current foreground color, given by the ForegroundColor property of this class.
        /// BackgroundColor to the current background color, given by the BackgroundColor property of this class.
        ///
        /// The resulting array is organized in row-major order for performance reasons.  The screen buffer, however, is
        /// organized in column-major order -- e.g. you specify the column index first, then the row index second, as in (x, y).
        /// This means that a cell at screen buffer position (x, y) is in the array element [y, x].
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public abstract
        BufferCell[,]
        GetBufferContents(Rectangle rectangle);

        /// <summary>
        /// Scroll a region of the screen buffer.
        /// </summary>
        /// <param name="source">
        /// Indicates the region of the screen to be scrolled.
        /// </param>
        /// <param name="destination">
        /// Indicates the upper left coordinates of the region of the screen to receive the source region contents.  The target
        /// region is the same size as the source region.
        /// </param>
        /// <param name="clip">
        /// Indicates the region of the screen to include in the operation.  If a cell would be changed by the operation but
        /// does not fall within the clip region, it will be unchanged.
        /// </param>
        /// <param name="fill">
        /// The character and attributes to be used to fill any cells within the intersection of the source rectangle and
        /// clipping rectangle that are left "empty" by the move.
        /// </param>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        public abstract
        void
        ScrollBufferContents
        (
            Rectangle source,
            Coordinates destination,
            Rectangle clip,
            BufferCell fill
        );

        /// <summary>
        /// Determines the number of BufferCells a substring of a string occupies.
        /// </summary>
        /// <param name="source">
        /// The string whose substring length we want to know.
        /// </param>
        /// <param name="offset">
        /// Offset where the substring begins in <paramref name="source"/>
        /// </param>
        /// <returns>
        /// The default implementation calls <see cref="PSHostRawUserInterface.LengthInBufferCells(string)"/> method
        /// with the substring extracted from the <paramref name="source"/> string
        /// starting at the offset <paramref name="offset"/>
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public virtual
        int
        LengthInBufferCells
        (
            string source,
            int offset
        )
        {
            if (source == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(source));
            }

            // this implementation is inefficient
            // it is here to help with backcompatibility
            // it preserves the old behavior from the times
            // when there was only Length(string) overload
            string substring = offset == 0 ? source : source.Substring(offset);
            return this.LengthInBufferCells(substring);
        }

        /// <summary>
        /// Determines the number of BufferCells a string occupies.
        /// </summary>
        /// <param name="source">
        /// The string whose length we want to know.
        /// </param>
        /// <returns>
        /// The default implementation returns the length of <paramref name="source"/>
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string, int)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public virtual
        int
        LengthInBufferCells
        (
            string source
        )
        {
            if (source == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(source));
            }

            return source.Length;
        }

        /// <summary>
        /// Determines the number of BufferCells a character occupies.
        /// </summary>
        /// <param name="source">
        /// The character whose length we want to know.
        /// </param>
        /// <returns>
        /// The default implementation returns 1.
        /// </returns>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public virtual
        int
        LengthInBufferCells
        (
            char source
        )
        {
            return 1;
        }

        /// <summary>
        /// Creates a two dimensional array of BufferCells by examining each character in <paramref name="contents"/>.
        /// </summary>
        /// <param name="contents">
        /// String array based on which the two dimensional array of BufferCells will be created.
        /// </param>
        /// <param name="foregroundColor">
        /// Foreground color of the buffer cells in the resulting array.
        /// </param>
        /// <param name="backgroundColor">
        /// Background color of the buffer cells in the resulting array.
        /// </param>
        /// <returns>
        /// A two dimensional array of BufferCells whose characters are the same as those in <paramref name="contents"/>
        /// and whose foreground and background colors set to <paramref name="foregroundColor"/> and
        /// <paramref name="backgroundColor"/>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="contents"/> is null;
        /// Any string in <paramref name="contents"/> is null or empty
        /// </exception>
        /// <remarks>
        /// If a character C takes one BufferCell to display as determined by LengthInBufferCells,
        /// one BufferCell is allocated with its Character set to C and BufferCellType to BufferCell.Complete.
        /// On the other hand, if C takes two BufferCell, two adjacent BufferCells on a row in
        /// the returned array will be allocated: the first has Character set to C and BufferCellType to
        /// <see cref="System.Management.Automation.Host.BufferCellType.Leading"/> and the second
        /// Character set to (char)0 and Type to
        /// <see cref="System.Management.Automation.Host.BufferCellType.Trailing"/>. Hence, the returned
        /// BufferCell array has <paramref name="contents"/>.Length number of rows and number of columns
        /// equal to the largest number of cells a string in <paramref name="contents"/> takes. The
        /// foreground and background colors of the cells are initialized to
        /// <paramref name="foregroundColor"/> and <paramref name="backgroundColor"/>, respectively.
        /// The resulting array is suitable for use with <see cref="PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// and <see cref="PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
#pragma warning disable 56506
        public
        BufferCell[,]
        NewBufferCellArray(string[] contents, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
        {
#pragma warning disable 56506

            if (contents == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(contents));
            }

            byte[][] charLengths = new byte[contents.Length][];
            int maxStringLengthInBufferCells = 0;
            for (int i = 0; i < contents.Length; i++)
            {
                if (string.IsNullOrEmpty(contents[i]))
                {
                    continue;
                }

                int lengthInBufferCells = 0;
                charLengths[i] = new byte[contents[i].Length];
                for (int j = 0; j < contents[i].Length; j++)
                {
                    charLengths[i][j] = (byte)LengthInBufferCells(contents[i][j]);
                    lengthInBufferCells += charLengths[i][j];
                }

                if (maxStringLengthInBufferCells < lengthInBufferCells)
                {
                    maxStringLengthInBufferCells = lengthInBufferCells;
                }
            }

            if (maxStringLengthInBufferCells <= 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(contents), MshHostRawUserInterfaceStrings.AllNullOrEmptyStringsErrorTemplate);
            }

            BufferCell[,] results = new BufferCell[contents.Length, maxStringLengthInBufferCells];
            for (int i = 0; i < contents.Length; i++)
            {
                int resultJ = 0;
                for (int j = 0; j < contents[i].Length; j++, resultJ++)
                {
                    if (charLengths[i][j] == 1)
                    {
                        results[i, resultJ] =
                            new BufferCell(contents[i][j], foregroundColor, backgroundColor, BufferCellType.Complete);
                    }
                    else if (charLengths[i][j] == 2)
                    {
                        results[i, resultJ] =
                            new BufferCell(contents[i][j], foregroundColor, backgroundColor, BufferCellType.Leading);
                        resultJ++;
                        results[i, resultJ] =
                            new BufferCell((char)0, foregroundColor, backgroundColor, BufferCellType.Trailing);
                    }
                }
                while (resultJ < maxStringLengthInBufferCells)
                {
                    results[i, resultJ] = new BufferCell(' ', foregroundColor, backgroundColor, BufferCellType.Complete);
                    resultJ++;
                }
            }

            return results;
#pragma warning restore 56506
        }
#pragma warning restore 56506

        /// <summary>
        /// Creates a 2D array of BufferCells by examining <paramref name="contents"/>.Character.
        /// <see cref="PSHostRawUserInterface"/>
        /// </summary>
        /// <param name="width">
        /// The number of columns of the resulting array
        /// </param>
        /// <param name="height">
        /// The number of rows of the resulting array
        /// </param>
        /// <param name="contents">
        /// The cell to be copied to each of the elements of the resulting array.
        /// </param>
        /// <returns>
        /// A <paramref name="width"/> by <paramref name="height"/> array of BufferCells where each cell's value is
        /// based on <paramref name="contents"/>
        /// <paramref name="backgroundColor"/>
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="width"/> is less than 1;
        /// <paramref name="height"/> is less than 1.
        /// </exception>
        /// <remarks>
        /// If the character takes one BufferCell to display as determined by LengthInBufferCells,
        /// one BufferCell is allocated with its Character set to the character and BufferCellType to
        /// BufferCell.Complete.
        /// On the other hand, if it takes two BufferCells, two adjacent BufferCells on a row
        /// in the returned array will be allocated: the first has Character
        /// set to the character and BufferCellType to BufferCellType.Leading and the second Character
        /// set to (char)0 and BufferCellType to BufferCellType.Trailing. Moreover, if <paramref name="width"/>
        /// is odd, the last column will just contain the leading cell.
        /// <paramref name="prototype"/>.BufferCellType is not used in creating the array.
        /// The resulting array is suitable for use with the PSHostRawUserInterface.SetBufferContents method.
        /// </remarks>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(Size, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public
        BufferCell[,]
        NewBufferCellArray(int width, int height, BufferCell contents)
        {
            if (width <= 0)
            {
                // "width" is not localizable
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(width), width,
                    MshHostRawUserInterfaceStrings.NonPositiveNumberErrorTemplate, "width");
            }

            if (height <= 0)
            {
                // "height" is not localizable
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(height), height,
                    MshHostRawUserInterfaceStrings.NonPositiveNumberErrorTemplate, "height");
            }

            BufferCell[,] buffer = new BufferCell[height, width];
            int charLength = LengthInBufferCells(contents.Character);
            if (charLength == 1)
            {
                for (int r = 0; r < buffer.GetLength(0); ++r)
                {
                    for (int c = 0; c < buffer.GetLength(1); ++c)
                    {
                        buffer[r, c] = contents;
                        buffer[r, c].BufferCellType = BufferCellType.Complete;
                    }
                }
            }
            else if (charLength == 2)
            {
                int normalizedWidth = width % 2 == 0 ? width : width - 1;
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < normalizedWidth; j++)
                    {
                        buffer[i, j] = contents;
                        buffer[i, j].BufferCellType = BufferCellType.Leading;
                        j++;
                        buffer[i, j] = new BufferCell((char)0,
                            contents.ForegroundColor, contents.BackgroundColor,
                            BufferCellType.Trailing);
                    }

                    if (normalizedWidth < width)
                    {
                        buffer[i, normalizedWidth] = contents;
                        buffer[i, normalizedWidth].BufferCellType = BufferCellType.Leading;
                    }
                }
            }

            return buffer;
        }

        /// <summary>
        /// Same as <see cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// </summary>
        /// <param name="size">
        /// The width and height of the resulting array.
        /// </param>
        /// <param name="contents">
        /// The cell to be copied to each of the elements of the resulting array.
        /// </param>
        /// <returns>
        /// An array of BufferCells whose size is <paramref name="size"/> and where each cell's value is
        /// based on <paramref name="contents"/>
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If <paramref name="size"/>.Width or <paramref name="size"/>.Height is less than 1.
        /// </exception>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(int, int, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.NewBufferCellArray(string[], ConsoleColor, ConsoleColor)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(char)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.LengthInBufferCells(string)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Rectangle, BufferCell)"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.SetBufferContents(Coordinates, BufferCell[,])"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.GetBufferContents"/>
        /// <seealso cref="System.Management.Automation.Host.PSHostRawUserInterface.ScrollBufferContents"/>
        public
        BufferCell[,]
        NewBufferCellArray(Size size, BufferCell contents)
        {
            return NewBufferCellArray(size.Width, size.Height, contents);
        }
    }
}
