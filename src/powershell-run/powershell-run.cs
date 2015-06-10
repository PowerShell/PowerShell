using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Automation.Host;
using System.Globalization;
using System.Reflection;

namespace ps_hello_world
{
    // this is all from https://msdn.microsoft.com/en-us/library/ee706570%28v=vs.85%29.aspx

	internal class MyRawUserInterface : PSHostRawUserInterface
    {
        /// <summary>
        /// Gets or sets the background color of the displayed text.
        /// This maps to the corresponding Console.Background property.
        /// </summary>
        public override ConsoleColor BackgroundColor
        {
            get { return Console.BackgroundColor; }
            set { Console.BackgroundColor = value; }
        }
    
        /// <summary>
        /// Gets or sets the size of the host buffer. In this example the 
        /// buffer size is adapted from the Console buffer size members.
        /// </summary>
        public override Size BufferSize
        {
            get { return new Size(200,500); }
            set {  }
            //get { return new Size(Console.BufferWidth, Console.BufferHeight); }
            //set { Console.SetBufferSize(value.Width, value.Height); }
        }
    
        /// <summary>
        /// Gets or sets the cursor position. In this example this 
        /// functionality is not needed so the property throws a 
        /// NotImplementException exception.
        /// </summary>
        public override Coordinates CursorPosition
        {
            get { throw new NotImplementedException(
                       "The method or operation is not implemented."); }
            set { throw new NotImplementedException(
                       "The method or operation is not implemented."); }
        }
    
        /// <summary>
        /// Gets or sets the size of the displayed cursor. In this example 
        /// the cursor size is taken directly from the Console.CursorSize 
        /// property.
        /// </summary>
        public override int CursorSize
        {
            get { return 12; }
            set { }
            //get { return Console.CursorSize; }
            //set { Console.CursorSize = value; }
        }
    
        /// <summary>
        /// Gets or sets the foreground color of the displayed text.
        /// This maps to the corresponding Console.ForgroundColor property.
        /// </summary>
        public override ConsoleColor ForegroundColor
        {
            get { return Console.ForegroundColor; }
            set { Console.ForegroundColor = value; }
        }
    
        /// <summary>
        /// Gets a value indicating whether the user has pressed a key. This maps   
        /// to the corresponding Console.KeyAvailable property.
        /// </summary>
        public override bool KeyAvailable
        {
          	get { return false; }
      	//  get { return Console.KeyAvailable; }
        }
    
        /// <summary>
        /// Gets the dimensions of the largest window that could be 
        /// rendered in the current display, if the buffer was at the least 
        /// that large. This example uses the Console.LargestWindowWidth and 
        /// Console.LargestWindowHeight properties to determine the returned 
        /// value of this property.
        /// </summary>
        public override Size MaxPhysicalWindowSize
        {
        //  get { return new Size(Console.LargestWindowWidth, Console.LargestWindowHeight); }
          	get { return new Size(1024,768); }
        }
    
        /// <summary>
        /// Gets the dimentions of the largest window size that can be 
        /// displayed. This example uses the Console.LargestWindowWidth and 
        /// console.LargestWindowHeight properties to determine the returned 
        /// value of this property.
        /// </summary>
        public override Size MaxWindowSize
        {
        //  get { return new Size(Console.LargestWindowWidth, Console.LargestWindowHeight); }
        	get { return new Size(1024,768); }
        }
    
        /// <summary>
        /// Gets or sets the position of the displayed window. This example 
        /// uses the Console window position APIs to determine the returned 
        /// value of this property.
        /// </summary>
        public override Coordinates WindowPosition
        {
        //  get { return new Coordinates(Console.WindowLeft, Console.WindowTop); }
        //  set { Console.SetWindowPosition(value.X, value.Y); }
            get { return new Coordinates(0,0); }
            set { }
        }
    
        /// <summary>
        /// Gets or sets the size of the displayed window. This example 
        /// uses the corresponding Console window size APIs to determine the  
        /// returned value of this property.
        /// </summary>
        public override Size WindowSize
        {
        //  get { return new Size(Console.WindowWidth, Console.WindowHeight); }
        //  set { Console.SetWindowSize(value.Width, value.Height); }
            get { return new Size(1024,768); }
            set { }
        }
    
        /// <summary>
        /// Gets or sets the title of the displayed window. The example 
        /// maps the Console.Title property to the value of this property.
        /// </summary>
        public override string WindowTitle
        {
        //  get { return Console.Title; }
        //  set { Console.Title = value; }
            get { return "window title"; }
            set { }
        }
    
        /// <summary>
        /// This API resets the input buffer. In this example this 
        /// functionality is not needed so the method returns nothing.
        /// </summary>
        public override void FlushInputBuffer()
        {
        }
    
        /// <summary>
        /// This API returns a rectangular region of the screen buffer. In 
        /// this example this functionality is not needed so the method throws 
        /// a NotImplementException exception.
        /// </summary>
        /// <param name="rectangle">Defines the size of the rectangle.</param>
        /// <returns>Throws a NotImplementedException exception.</returns>
        public override BufferCell[,] GetBufferContents(Rectangle rectangle)
        {
            throw new NotImplementedException(
                   "The method or operation is not implemented.");
        }
    
        /// <summary>
        /// This API reads a pressed, released, or pressed and released keystroke 
        /// from the keyboard device, blocking processing until a keystroke is 
        /// typed that matches the specified keystroke options. In this example 
        /// this functionality is not needed so the method throws a
        /// NotImplementException exception.
        /// </summary>
        /// <param name="options">Options, such as IncludeKeyDown,  used when 
        /// reading the keyboard.</param>
        /// <returns>Throws a NotImplementedException exception.</returns>
        public override KeyInfo ReadKey(ReadKeyOptions options)
        {
            throw new NotImplementedException(
                    "The method or operation is not implemented.");
        }
    
        /// <summary>
        /// This API crops a region of the screen buffer. In this example 
        /// this functionality is not needed so the method throws a
        /// NotImplementException exception.
        /// </summary>
        /// <param name="source">The region of the screen to be scrolled.</param>
        /// <param name="destination">The region of the screen to receive the 
        /// source region contents.</param>
        /// <param name="clip">The region of the screen to include in the operation.</param>
        /// <param name="fill">The character and attributes to be used to fill all cell.</param>
        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill)
        {
            throw new NotImplementedException(
                    "The method or operation is not implemented.");
        }
    
        /// <summary>
        /// This method copies an array of buffer cells into the screen buffer 
        /// at a specified location. In this example this functionality is 
        /// not needed so the method throws a NotImplementedException exception.
        /// </summary>
        /// <param name="origin">The parameter is not used.</param>
        /// <param name="contents">The parameter is not used.</param>
        public override void SetBufferContents(Coordinates origin, 
                                               BufferCell[,] contents)
        {
            throw new NotImplementedException(
                    "The method or operation is not implemented.");
        }
    
        /// <summary>
        /// This method copies a given character, foreground color, and background 
        /// color to a region of the screen buffer. In this example this 
        /// functionality is not needed so the method throws a
        /// NotImplementException exception./// </summary>
        /// <param name="rectangle">Defines the area to be filled. </param>
        /// <param name="fill">Defines the fill character.</param>
        public override void SetBufferContents(Rectangle rectangle, BufferCell fill)
        {
            throw new NotImplementedException(
                    "The method or operation is not implemented.");
        }
    }


	internal class MyHostUI : PSHostUserInterface
	{
		private MyRawUserInterface myRawUi = new MyRawUserInterface();

    	public override PSHostRawUserInterface RawUI
    	{
    		get { return this.myRawUi; }
    	}

		public override Dictionary<string, PSObject> Prompt(
                                                        string caption, 
                                                        string message, 
                                                        System.Collections.ObjectModel.Collection<FieldDescription> descriptions)
    	{
    		throw new NotImplementedException(
    	       "The method or operation is not implemented.");
    	}

		public override int PromptForChoice(string caption, string message, System.Collections.ObjectModel.Collection<ChoiceDescription> choices, int defaultChoice)
    	{
    		throw new NotImplementedException("The method or operation is not implemented.");
    	}

		public override PSCredential PromptForCredential(
                                                     string caption, 
                                                     string message, 
                                                     string userName, 
                                                     string targetName)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        public override PSCredential PromptForCredential(
                                                     string caption, 
                                                     string message, 
                                                     string userName, 
                                                     string targetName, 
                                                     PSCredentialTypes allowedCredentialTypes, 
                                                     PSCredentialUIOptions options)
        {
          throw new NotImplementedException("The method or operation is not implemented.");
        }
    
        public override string ReadLine()
        {
            return Console.ReadLine();
        }
    
        public override System.Security.SecureString ReadLineAsSecureString()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }
    
        public override void Write(string value)
        {
    	    Console.BackgroundColor = ConsoleColor.Green;
            Console.Write(value);
    	    Console.ResetColor();
        }
    
        public override void Write(
                                   ConsoleColor foregroundColor, 
                                   ConsoleColor backgroundColor, 
                                   string value)
        {
            // Colors are ignored.
    	    Console.ForegroundColor = foregroundColor;
    	    Console.BackgroundColor = backgroundColor;
            Console.Write(value);
    	    Console.ResetColor();
        }
    
        public override void WriteDebugLine(string message)
        {
    	    Console.BackgroundColor = ConsoleColor.Gray;
            Console.Write(String.Format(
                                      CultureInfo.CurrentCulture, 
                                      "DEBUG: {0}", 
                                      message));
    	    Console.ResetColor();
    	    Console.WriteLine();
        }
    
        public override void WriteErrorLine(string value)
        {
    	    Console.BackgroundColor = ConsoleColor.Red;
            Console.Write(String.Format(
                                      CultureInfo.CurrentCulture, 
                                      "ERROR: {0}", 
                                      value));
    	    Console.ResetColor();
    	    Console.WriteLine();
        }
    
        public override void WriteLine()
        {
            System.Console.WriteLine();
        }
    
    
    
        public override void WriteLine(string value)
        {
    	    Write(value);
    	    Console.WriteLine();
        }
    
        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
    	    Write(foregroundColor,backgroundColor,value);
    	    Console.WriteLine();
        }
    
        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
        }
    
        public override void WriteVerboseLine(string message)
        {
    	    Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(String.Format(CultureInfo.CurrentCulture, "VERBOSE: {0}", message));
    	    Console.ResetColor();
        }
    
        public override void WriteWarningLine(string message)
        {
    	    Console.BackgroundColor = ConsoleColor.Yellow;
            Console.WriteLine(String.Format(CultureInfo.CurrentCulture, "WARNING: {0}", message));
    	    Console.ResetColor();
        }
	}

	internal class MyHost : PSHost
	{
		private Program program;
		private CultureInfo originalCultureInfo = new CultureInfo("en-US");
		private CultureInfo originalUICultureInfo = new CultureInfo("en-US");
		private Guid myId = Guid.NewGuid();
		
		public MyHost(Program program)
		{
			this.program = program;
		}

		private MyHostUI myHostUI = new MyHostUI();

		public override System.Globalization.CultureInfo CurrentCulture
		{
			get { return this.originalCultureInfo; }
		}

		public override System.Globalization.CultureInfo CurrentUICulture
		{
			get { return this.originalUICultureInfo; }
		}

		public override Guid InstanceId
		{
			get { return myId; }
		}

		public override string Name
		{
			get { return "MyHost"; }
		}

		public override PSHostUserInterface UI
		{
			get { return myHostUI; }
		}

		public override Version Version
		{
			get { return new Version(0,0,0,0); }
		}

		public override void EnterNestedPrompt()
		{
			throw new NotImplementedException("EnterNestedPrompt not implemented");
		}

		public override void ExitNestedPrompt()
		{
			throw new NotImplementedException("ExitNestedPrompt not implemented");
		}
		
		public override void NotifyBeginApplication()
		{
			Console.WriteLine("MyHost: NotifyBeginApplication");
			return;
		}

		public override void NotifyEndApplication()
		{
			return;
		}

		public override void SetShouldExit(int exitCode)
		{
			Console.WriteLine("SetShouldExit: " + exitCode);
		}
	}

    class Program
    {
        public static void init()
        {
            string psBasePath = System.IO.Directory.GetCurrentDirectory();
            PowerShellAssemblyLoadContextInitializer.SetPowerShellAssemblyLoadContext(psBasePath);

            // PH: this debugging requires a change in the core PS stuff that was stashed away
            //     during source cleanup
            /*
			PowerShellAssemblyLoadContext asmLoadContext = PowerShellAssemblyLoadContextInitializer.AsmLoadContext;
			IEnumerable<Assembly> assemblies = asmLoadContext.GetAssemblies("System.Management.Automation");
			foreach (Assembly a in assemblies)
			{
				Console.WriteLine("a: " + a);
			}*/
        }

		static void test1(string[] args)
		{
            InitialSessionState iss = InitialSessionState.CreateDefault2();
            Runspace rs = RunspaceFactory.CreateRunspace(iss);
            rs.Open();
            PowerShell ps = PowerShell.Create();
            ps.Runspace = rs;

            //ps.AddScript("\"Hello World!\"");
            Console.WriteLine(args[0]);
            ps.AddScript(args[0]);
            foreach (string str in ps.Invoke<string>())
            {
                Console.Write(str);
            }
 		}

		static void test2(string[] args)
		{
			MyHost myHost = new MyHost(new Program());

			InitialSessionState iss = InitialSessionState.CreateDefault2();

			using (Runspace rs = RunspaceFactory.CreateRunspace(myHost,iss))
			{
				rs.Open();
				using (PowerShell ps = PowerShell.Create())
				{
					ps.Runspace = rs;

					foreach (var arg in args)
					{
						Console.WriteLine("script: " + arg);
						ps.AddScript(arg);
					}
					ps.AddCommand("out-default");
					ps.Commands.Commands[0].MergeMyResults(PipelineResultTypes.Error,PipelineResultTypes.Output);
					ps.Invoke();
				}
			}
		}
		
        static void Main(string[] args)
        {
            init();
			
			//test1(args);
			test2(args);
        }
    }
}

