namespace Microsoft.PowerShell.Linux.Host
{
    using System;
    using System.Collections.ObjectModel;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Text;

    /// <summary>
    /// This class is used to read the command line and color the text as 
    /// it is entered. Tokens are determined using the PSParser.Tokenize
    /// method.
    /// </summary>
    internal class ConsoleReadLine
    {
        /// <summary>
        /// Holds a reference to the runspace (for history access).
        /// </summary>
        private Runspace runspace;

        /// <summary>
        /// The buffer used to edit.
        /// </summary>
        private StringBuilder buffer = new StringBuilder();

	/// <summary>
	/// integeger for tracking up and down arrow history   
	/// </summary>
	private int historyIndex; 

	/// <summary>
	/// Used for storing tab completion   
	/// </summary>
	private CommandCompletion cmdCompleteOpt; 
	
        /// <summary>
        /// The position of the cursor within the buffer.
        /// </summary>
        private int current;

	/// <summary>
	/// Detects previously pressed key for TabCompletion
	/// </summary>
        /// <summary>
	private ConsoleKeyInfo previousKeyPress; 

	/// <summary>
	/// Retains TabCompletion position
	/// </summary>
	private int tabCompletionPos; 

	/// <summary>
	///  History Queue
	/// </summary>
	private Collection<PSObject> historyResult; 

	/// <summary>
	/// Hashtable for command completion options  
	/// </summary>
	private System.Collections.Hashtable options = new System.Collections.Hashtable();

	/// <summary>
	/// Retain original buffer for TabCompletion
	/// </summary>
	private StringBuilder tabBuffer;

	/// <summary>
	/// tabbuffer for storing result of tabcompletion
	/// </summary>
	private string tabResult; 

        /// The count of characters in buffer rendered.
        /// </summary>
        private int rendered;

        /// <summary>
        /// Store the anchor and handle cursor movement
        /// </summary>
        private Cursor cursor;

        /// <summary>
        /// The array of colors for tokens, indexed by PSTokenType
        /// </summary>
        private ConsoleColor[] tokenColors;

        /// <summary>
        /// We do not pick different colors for every token, those tokens
        /// use this default.
        /// </summary>
        private ConsoleColor defaultColor = ConsoleColor.White;

        /// <summary>
        /// Initializes a new instance of the ConsoleReadLine class.
        /// </summary>
        public ConsoleReadLine()
        {
            this.tokenColors = new ConsoleColor[]
                {
                    this.defaultColor,       // Unknown
                    ConsoleColor.Yellow,     // Command
                    ConsoleColor.Green,      // CommandParameter
                    ConsoleColor.Cyan,       // CommandArgument
                    ConsoleColor.Cyan,       // Number
                    ConsoleColor.Cyan,       // String
                    ConsoleColor.Green,      // Variable
                    this.defaultColor,       // Member
                    this.defaultColor,       // LoopLabel
                    ConsoleColor.DarkYellow, // Attribute
                    ConsoleColor.DarkYellow, // Type
                    ConsoleColor.DarkCyan,   // Operator
                    this.defaultColor,       // GroupStart
                    this.defaultColor,       // GroupEnd
                    ConsoleColor.Magenta,    // Keyword
                    ConsoleColor.Red,        // Comment
                    ConsoleColor.DarkCyan,   // StatementSeparator
                    this.defaultColor,       // NewLine
                    this.defaultColor,       // LineContinuation
                    this.defaultColor,       // Position            
                };
        }

        /// <summary>
        /// Read a line of text, colorizing while typing.
        /// </summary>
        /// <returns>The command line read</returns>
        public string Read(Runspace runspace)
        {
	    this.runspace = runspace;
            this.Initialize();

            while (true)
            {             
                ConsoleKeyInfo key = Console.ReadKey(true);

                switch (key.Key)
                {
		    case ConsoleKey.Backspace:
                        this.OnBackspace();
                        break;
		    case ConsoleKey.Delete:
                        this.OnDelete();
                        break;			
		    case ConsoleKey.Enter:
                        return this.OnEnter();
                   case ConsoleKey.RightArrow:
                        this.OnRight(key.Modifiers);
                        break;
                    case ConsoleKey.LeftArrow:
                        this.OnLeft(key.Modifiers);
                        break;
                    case ConsoleKey.Escape:
                        this.OnEscape();
                        break;
                    case ConsoleKey.Home:
                        this.OnHome();
                        break;
                    case ConsoleKey.End:
                        this.OnEnd();
                        break;
		    case ConsoleKey.Tab: 
			this.OnTab();
			break;
                    case ConsoleKey.UpArrow:
			this.OnUpArrow(); 
			break; 
                    case ConsoleKey.DownArrow:
			this.OnDownArrow();
			break;

		    // TODO: case ConsoleKey.LeftWindows: not available in linux 
		    // TODO: case ConsoleKey.RightWindows: not available in linux
			
                    default:

                        if (key.KeyChar == '\x0D')
                        {
                            goto case ConsoleKey.Enter;      // Ctrl-M
                        }

                        if (key.KeyChar == '\x08')
                        {
                            goto case ConsoleKey.Backspace;  // Ctrl-H
                        }

                        this.Insert(key.KeyChar);

                        this.Render();
			break;
                }
    		        previousKeyPress = key; 
            }
        }

        /// <summary>
        /// Initializes the buffer.
        /// </summary>
        private void Initialize()
        {
	    this.tabCompletionPos = 0; 
            this.historyIndex = 0; 
	    this.buffer.Length = 0;
            this.current = 0;
            this.rendered = 0;
            this.cursor = new Cursor();
        }

        /// <summary>
        /// Inserts a key.
        /// </summary>
        /// <param name="key">The key to insert.</param>
        private void Insert(char key)
        {
            this.buffer.Insert(this.current, key);
            this.current++;
            if (key == '\n')
                this.Render();
        }

        /// <summary>
        /// The End key was entered.
        /// </summary>
        private void OnEnd()
        {
            this.current = this.buffer.Length;
            this.cursor.Place(this.rendered);
        }

	/// <summary>
	///   The Tab key was entered 
	/// </summary>
	private void OnTab()
	{
	    if (previousKeyPress.Key != ConsoleKey.Tab || previousKeyPress.Key == ConsoleKey.Enter)
	    {
		tabBuffer = this.buffer;
		using (PowerShell powershell = PowerShell.Create())
		{
		    cmdCompleteOpt = CommandCompletion.CompleteInput(this.tabBuffer.ToString(), this.current, options, powershell);
		}
	    }

	    try
	    {
		tabResult = cmdCompleteOpt.CompletionMatches[tabCompletionPos].CompletionText;
	    }
	    catch (Exception)
	    {
		//todo continue nicely
	    }

	    tabCompletionPos++;

	    //if there is a command for the user before the uncompleted option
	    if (!String.IsNullOrEmpty(tabResult))
	    {			
		//handle file path slashes 
		if (tabResult.Contains(".\\"))
                {
                     tabResult = tabResult.Replace(".\\", "");
                }

		if (this.buffer.ToString().Contains(" "))
		{
		    var replaceIndex = cmdCompleteOpt.ReplacementIndex; 
		    string replaceBuffer = this.buffer.ToString();
		    
		    replaceBuffer = replaceBuffer.Remove(replaceIndex);	
		    tabResult = replaceBuffer + tabResult;	    
	   	}		

		OnEscape();

		BufferFromString(tabResult);

		//re-render 
	        this.Render();
	    }		

	} //end of OnTab()

	/// <summary>
	/// Set buffer to a string rather than inserting char by char
	/// </summary>
	private void BufferFromString(string endResult)
	{
	        //reset prompt and buffer
	    	OnEscape();

		//set the buffer to the string
		for (int i = 0; i < endResult.Length; i++)
		{
		    this.Insert(endResult[i]);		
		}

	}

        /// <summary>
        /// The Home key was entered.
        /// </summary>
        private void OnHome()
        {
	    this.current = 0;
            this.cursor.Reset();
        }
	
        /// <summary>
        /// The Escape key was enetered.
        /// </summary>
        private void OnEscape()
        {
            this.buffer.Length = 0;
            this.current = 0;
            this.Render();
        }

	/// <summary>
	/// The up arrow was pressed to retrieve history 
	/// </summary>
	private void OnUpArrow() {

	    if ((previousKeyPress.Key != ConsoleKey.UpArrow && previousKeyPress.Key != ConsoleKey.DownArrow) || previousKeyPress.Key == ConsoleKey.Enter)
	    {
		//first time getting the history
		using (Pipeline pipeline = this.runspace.CreatePipeline("Get-History"))
		{
		    historyResult = pipeline.Invoke();
	
		}

		BufferFromString(historyResult[historyIndex].Members["CommandLine"].Value.ToString());
		this.Render(); 
		historyIndex++;
	    }

	    else
	    {
		BufferFromString(historyResult[historyIndex].Members["CommandLine"].Value.ToString());
		this.Render();
		
		if (historyIndex < historyResult.Count -1)
		{
		    historyIndex++;
		}

		else 
		{
		    historyIndex = 0;
		}		
	    }  
	}
	
	/// <summary>
	///   Changes the history queue when the down arrow is pressed
	/// </summary>
	private void OnDownArrow()
	{
	    if ((previousKeyPress.Key != ConsoleKey.DownArrow && previousKeyPress.Key != ConsoleKey.UpArrow) || previousKeyPress.Key == ConsoleKey.Enter)
	    {	
		//first time getting the history
		using (Pipeline pipeline = this.runspace.CreatePipeline("Get-History"))
		{
		    historyResult = pipeline.Invoke();	
		}
		
		historyIndex = historyResult.Count -1;	
		BufferFromString(historyResult[historyIndex].Members["CommandLine"].Value.ToString());
		this.Render(); 

		historyIndex--;
	    }

	    else
	    {
		BufferFromString(historyResult[historyIndex].Members["CommandLine"].Value.ToString());
		this.Render();
		
	        if( historyIndex == 0)
		{
		    historyIndex = historyResult.Count -1; 
		}

		else
		{
		    historyIndex--;
		}

	    }
	}

        /// <summary>
        /// Moves to the left of the cursor position.
        /// </summary>
        /// <param name="consoleModifiers">Enumeration for Alt, Control, 
        /// and Shift keys.</param>
        private void OnLeft(ConsoleModifiers consoleModifiers)
        {
            if ((consoleModifiers & ConsoleModifiers.Control) != 0)
            {
                // Move back to the start of the previous word.
                if (this.buffer.Length > 0 && this.current != 0)
                {
                    bool nonLetter = IsSeperator(this.buffer[this.current - 1]);
                    while (this.current > 0 && (this.current - 1 < this.buffer.Length))
                    {
                        this.MoveLeft();

                        if (IsSeperator(this.buffer[this.current]) != nonLetter)
                        {
                            if (!nonLetter)
                            {
                                this.MoveRight();
                                break;
                            }

                            nonLetter = false;
                        }
                    }
                }
            }
            else
            {
                this.MoveLeft();
            }
        }

        /// <summary>
        /// Determines if a character is a seperator.
        /// </summary>
        /// <param name="ch">Character to investigate.</param>
        /// <returns>A value that indicates whether the character 
        /// is a seperator.</returns>
        private static bool IsSeperator(char ch)
        {
            return !Char.IsLetter(ch);
        }

        /// <summary>
        /// Moves to what is to the right of the cursor position.
        /// </summary>
        /// <param name="consoleModifiers">Enumeration for Alt, Control, 
        /// and Shift keys.</param>
        private void OnRight(ConsoleModifiers consoleModifiers)
        {
            if ((consoleModifiers & ConsoleModifiers.Control) != 0)
            {
                // Move to the next word.
                if (this.buffer.Length != 0 && this.current < this.buffer.Length)
                {
                    bool nonLetter = IsSeperator(this.buffer[this.current]);
                    while (this.current < this.buffer.Length)
                    {
                        this.MoveRight();

                        if (this.current == this.buffer.Length)
                        {
                            break;
                        }

                        if (IsSeperator(this.buffer[this.current]) != nonLetter)
                        {
                            if (nonLetter)
                            {
                                break;
                            }

                            nonLetter = true;
                        }
                    }
                }
            }
            else
            {
                this.MoveRight();
            }
        }

        /// <summary>
        /// Moves the cursor one character to the right.
        /// </summary>
        private void MoveRight()
        {
            if (this.current < this.buffer.Length)
            {
                char c = this.buffer[this.current];
                this.current++;
                Cursor.Move(1);
            }
        }
	
        /// <summary>
        /// Moves the cursor one character to the left.
        /// </summary>
        private void MoveLeft()
        {
            if (this.current > 0 && (this.current - 1 < this.buffer.Length))
            {
                this.current--;
                char c = this.buffer[this.current];
                Cursor.Move(-1);
            }
        }
	
        /// <summary>
        /// The Enter key was entered.
        /// </summary>
        /// <returns>A newline character.</returns>
        private string OnEnter()
        {
            Console.Out.Write("\n");
            return this.buffer.ToString();
        }
	
        /// <summary>
        /// The delete key was entered.
        /// </summary>
        private void OnDelete()
        {
            if (this.buffer.Length > 0 && this.current < this.buffer.Length)
            {
                this.buffer.Remove(this.current, 1);
                this.Render();
            }
        }
	
        /// <summary>
        /// The Backspace key was entered.
        /// </summary>
        private void OnBackspace()
        {
            if (this.buffer.Length > 0 && this.current > 0)
            {
                this.buffer.Remove(this.current - 1, 1);
                this.current--;
                this.Render();
            }
        }
	
        /// <summary>
        /// Displays the line.
        /// </summary>
        private void Render()
        {
	    this.cursor.Reset();

            string text = this.buffer.ToString();
	    
            // The PowerShell tokenizer is used to decide how to colorize
            // the input.  Any errors in the input are returned in 'errors',
            // but we won't be looking at those here.
            Collection<PSParseError> errors = null;
            Collection<PSToken> tokens = PSParser.Tokenize(text, out errors);
 	
            if (tokens.Count > 0)
            {
                // We can skip rendering tokens that end before the cursor.
                int i;
             
		for (i = 0; i < tokens.Count; ++i)
                {
                    if (this.current >= tokens[i].Start)
                    {
                        break;
                    }
                }

                // Place the cursor at the start of the first token to render.  The
                // last edit may require changes to the colorization of characters
                // preceding the cursor.
                //this.cursor.Place(tokens[i].Start);

                for (; i < tokens.Count; ++i)
                {
                    // Write out the token.  We don't use tokens[i].Content, instead we
                    // use the actual text from our input because the content sometimes
                    // excludes part of the token, e.g. the quote characters of a string.
                    Console.ForegroundColor = this.tokenColors[(int)tokens[i].Type];
                    Console.Out.Write(text.Substring(tokens[i].Start, tokens[i].Length));

                    // Whitespace doesn't show up in the array of tokens.  Write it out here.
                    if (i != (tokens.Count - 1))
                    {
                        Console.ForegroundColor = this.defaultColor;
                        for (int j = (tokens[i].Start + tokens[i].Length); j < tokens[i + 1].Start; ++j)
                        {
                            Console.Out.Write(text[j]);
                        }
                    }
                }

                // It's possible there is text left over to output.  This happens when there is
                // some error during tokenization, e.g. a string literal is missing a closing quote.
                Console.ForegroundColor = this.defaultColor;
                for (int j = tokens[i - 1].Start + tokens[i - 1].Length; j < text.Length; ++j)
                {
                    Console.Out.Write(text[j]);
                }
            }
            else
            {
                // If tokenization completely failed, just redraw the whole line.  This
                // happens most frequently when the first token is incomplete, like a string
                // literal missing a closing quote.
                this.cursor.Reset();
                Console.Out.Write(text);
            }

            // If characters were deleted, we must write over previously written characters
            if (text.Length < this.rendered)
            {
                Console.Out.Write(new string(' ', this.rendered - text.Length));
            }

            this.rendered = text.Length;
            this.cursor.Place(this.current);
        }
	
        /// <summary>
        /// A helper class for maintaining the cursor while editing the command line.
        /// </summary>
        internal class Cursor
        {
            /// <summary>
            /// The top anchor for reposition the cursor.
            /// </summary>
            private int anchorTop;

            /// <summary>
            /// The left anchor for repositioning the cursor.
            /// </summary>
            private int anchorLeft;
	    
            /// <summary>
            /// Initializes a new instance of the Cursor class.
            /// </summary>
            public Cursor()
            {
                this.anchorTop = Console.CursorTop;
                this.anchorLeft = Console.CursorLeft;
            }

            /// <summary>
            /// Moves the cursor.
            /// </summary>
            /// <param name="delta">The number of characters to move.</param>
            internal static void Move(int delta)
            {
                int position = Console.CursorTop * Console.BufferWidth + Console.CursorLeft + delta;

                Console.CursorLeft = position % Console.BufferWidth;
                Console.CursorTop = position / Console.BufferWidth;
            }

            /// <summary>
            /// Resets the cursor position.
            /// </summary>
            internal void Reset()
            {
                Console.CursorTop = this.anchorTop;
                Console.CursorLeft = this.anchorLeft;
            }

            /// <summary>
            /// Moves the cursor to a specific position.
            /// </summary>
            /// <param name="position">The new position.</param>
            internal void Place(int position)
            {
                Console.CursorLeft = (this.anchorLeft + position) % Console.BufferWidth;
                int cursorTop = this.anchorTop + (this.anchorLeft + position) / Console.BufferWidth;
                if (cursorTop >= Console.BufferHeight)
                {
                    this.anchorTop -= cursorTop - Console.BufferHeight + 1;
                    cursorTop = Console.BufferHeight - 1;
                }

                Console.CursorTop = cursorTop;
            }
        } // End Cursor 
    }
}
