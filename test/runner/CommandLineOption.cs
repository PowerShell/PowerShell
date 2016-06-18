// File: CommandLineOptions.cs
//
// This is a re-usable component to be used when you 
// need to parse command-line options/parameters.
//
// Separates command line parameters from command line options.
// Uses reflection to populate member variables the derived class with the values 
// of the options.
//
// An option can start with "-" or "--". On Windows systems, it can start with "/" as well.
//
// I define 3 types of "options":
//   1. Boolean options (yes/no values), e.g: /r to recurse
//   2. Value options, e.g: /loglevel=3
//   2. Parameters: standalone strings like file names
//
// An example to explain:
//   csc /nologo /t:exe myfile.cs
//       |       |      |
//       |       |      + parameter
//       |       |
//       |       + value option
//       |
//       + boolean option
//
// Please see a short description of the CommandLineOptions class
// at http://codeblast.com/~gert/dotnet/sells.html
// 
// Gert Lombard (gert@codeblast.com)
// James Newkirk (jim@nunit.org)
using System;
using System.Reflection;
using System.Collections;
using System.Text;

namespace Codeblast
{

	//
	// The Attributes
	//

	[AttributeUsage(AttributeTargets.Field)]
	public class OptionAttribute : Attribute 
	{
		protected object optValue;
		protected string optName;
		protected string description;
        protected bool mandatory = false;

		public string Alias 
		{
			get { return optName; }
			set { optName = value; }
		}

		public object Value
		{
			get { return optValue; }
			set { optValue = value; }
		}

		public string Description 
		{
			get { return description; }
			set { description = value; }
		}

        public bool Mandatory
        {
            get { return mandatory; }
            set { mandatory = value; }
        }
	}

	//
	// The CommandLineOptions members
	//

	public abstract class CommandLineOptions
	{
		protected ArrayList parameters;
		protected bool isInvalid = false; 

		private int optionCount;
		private ArrayList invalidArguments = new ArrayList();
		private bool allowForwardSlash;

		public CommandLineOptions( string[] args )
			: this( System.IO.Path.DirectorySeparatorChar != '/', args ) {}

		public CommandLineOptions( bool allowForwardSlash, string[] args )
		{
			this.allowForwardSlash = allowForwardSlash;
			optionCount = Init( args );
            if ( MissingMandatoryOption() )
            {
                isInvalid = true;
            }
		}

        public bool MissingMandatoryOption()
        {
			Type t = this.GetType();
			FieldInfo[] fields = t.GetFields(BindingFlags.Instance|BindingFlags.Public);
			foreach (FieldInfo field in fields) 
            {
                OptionAttribute[] atts = (OptionAttribute[])field.GetCustomAttributes(typeof(OptionAttribute), true);
                foreach(OptionAttribute a in atts) { 
                    if ( a.Mandatory && field.GetValue(this) == null) {
                        InvalidOption(field.Name);
                        return true;
                    }
                }
            }
            return false;
        }

		public IList InvalidArguments
		{
			get { return invalidArguments; }
		}

		public bool NoArgs
		{
			get 
			{ 
				return ParameterCount == 0 && optionCount == 0;
			}
		}

		public bool AllowForwardSlash
		{
			get { return allowForwardSlash; }
		}

		public int Init(params string[] args)
		{
			int count = 0;
			int n = 0;
			while (n < args.Length)
			{
				int pos = IsOption(args[n]);
				if (pos > 0)
				{
					// It's an option:
					if (GetOption(args, ref n, pos))
						count++;
					else
						InvalidOption(args[Math.Min(n, args.Length-1)]);
				}
				else
				{
					if (parameters == null) parameters = new ArrayList();
					parameters.Add(args[n]);
					if ( !IsValidParameter(args[n]) )
						InvalidOption( args[n] );
				}
				n++;
			}
			return count;
		}

		// An option starts with "/", "-" or "--":
		protected virtual int IsOption(string opt)
		{
			char[] c = null;
			if (opt.Length < 2) 
			{
				return 0;
			}
			else if (opt.Length > 2)
			{
				c = opt.ToCharArray(0, 3);
				if (c[0] == '-' && c[1] == '-' && IsOptionNameChar(c[2])) return 2;
			}
			else
			{
				c = opt.ToCharArray(0, 2);
			}
			if ((c[0] == '-' || c[0] == '/' && AllowForwardSlash) && IsOptionNameChar(c[1])) return 1;
			return 0; 
		}

		protected virtual bool IsOptionNameChar(char c)
		{
			return Char.IsLetterOrDigit(c) || c == '?';
		}

		protected virtual void InvalidOption(string name)
		{
			invalidArguments.Add( name );
			isInvalid = true;
		}

		protected virtual bool IsValidParameter(string param)
		{
			return true;
		}

		protected virtual bool MatchAlias(FieldInfo field, string name)
		{
			object[] atts = (object[])field.GetCustomAttributes(typeof(OptionAttribute), true);
			foreach (OptionAttribute att in atts)
			{
				if (string.Compare(att.Alias, name, true) == 0) return true;
			}
			return false;
		}

		protected virtual FieldInfo GetMemberField(string name)
		{
			Type t = this.GetType();
			FieldInfo[] fields = t.GetFields(BindingFlags.Instance|BindingFlags.Public);
            FieldInfo myField = null;
            int matchcount = 0;
			foreach (FieldInfo field in fields)
			{
				// if (string.Compare(field.Name, name, true) >= 0) 
                if ( field.Name.IndexOf(name, StringComparison.CurrentCultureIgnoreCase) == 0 )
                {
                    matchcount++;
                    myField = field;
                }
                
				if (MatchAlias(field, name)) return field;
			}
            if ( matchcount == 1 )
            {
                return myField;
            }
            else {
                Console.WriteLine("Matchcount = {0}", matchcount);
            }
			return null;
		}

		protected virtual object GetOptionValue(FieldInfo field)
		{
			object[] atts = (object[])field.GetCustomAttributes(typeof(OptionAttribute), true);
			if (atts.Length > 0)
			{
				OptionAttribute att = (OptionAttribute)atts[0];
				return att.Value;
			}
			return null;
		}

		protected virtual bool GetOption(string[] args, ref int index, int pos)
		{
			try
			{
				object cmdLineVal = null;
				string opt = args[index].Substring(pos, args[index].Length-pos);
				SplitOptionAndValue(ref opt, ref cmdLineVal);
				FieldInfo field = GetMemberField(opt);
				if (field != null)
				{
					object value = GetOptionValue(field);
					if (value == null)
					{
						if (field.FieldType == typeof(bool))
							value = true; // default for bool values is true
						else if(field.FieldType == typeof(string))
						{
							value = cmdLineVal != null ? cmdLineVal : args[++index];
							field.SetValue(this, Convert.ChangeType(value, field.FieldType));
							string stringValue = (string)value;
							if(stringValue == null || stringValue.Length == 0) return false; 
							return true;
						}
                        else if(field.FieldType == typeof(string[]))
                        {
							value = cmdLineVal != null ? cmdLineVal : args[++index];
                            //ArrayList al = new ArrayList();
                            //foreach(string s in ((string)value).Split(','))
                            //{
                                //al.Add(s.Trim());
                            //}
                            string[] sa = ((string)value).Split(',');
                            string[] trimmed = new string[sa.Length];
                            for(int i = 0; i < sa.Length; i++) {
                                trimmed[i] = sa[i].Trim();
                            }
							field.SetValue(this, trimmed);
                            return true;
                        }
                        // JWT
						else if(field.FieldType.GetTypeInfo().IsEnum) {
							cmdLineVal = cmdLineVal != null ? cmdLineVal : args[++index];

							value = Enum.Parse( field.FieldType, (string)cmdLineVal, true );
                        }
						else
							value = cmdLineVal != null ? cmdLineVal : args[++index];
					}
					field.SetValue(this, Convert.ChangeType(value, field.FieldType));
					return true;
				}
			}
			catch (Exception) 
			{
				// Ignore exceptions like type conversion errors.
			}
			return false;
		}

		protected virtual void SplitOptionAndValue(ref string opt, ref object val)
		{
			// Look for ":" or "=" separator in the option:
			int pos = opt.IndexOfAny( new char[] { ':', '=' } );
			if (pos < 1) return;

			val = opt.Substring(pos+1);
			opt = opt.Substring(0, pos);
		}

		// Parameter accessor:
		public string this[int index]
		{
			get
			{
				if (parameters != null) return (string)parameters[index];
				return null;
			}
		}

		public ArrayList Parameters
		{
			get { return parameters; }
		}

		public int ParameterCount
		{
			get
			{
				return parameters == null ? 0 : parameters.Count;
			}
		}

		public virtual void Help()
		{
			Console.WriteLine(GetHelpText());
		}

		public virtual string GetHelpText()
		{
			StringBuilder helpText = new StringBuilder();

			Type t = this.GetType();
			FieldInfo[] fields = t.GetFields(BindingFlags.Instance|BindingFlags.Public);
            char optChar = allowForwardSlash ? '/' : '-';
			foreach (FieldInfo field in fields)
			{
				object[] atts = (object[])field.GetCustomAttributes(typeof(OptionAttribute), true);
				if (atts.Length > 0)
				{
					OptionAttribute att = (OptionAttribute)atts[0];
					if (att.Description != null)
					{
						string valType = "";
						if (att.Value == null)
						{
							if (field.FieldType == typeof(float)) valType = "=FLOAT";
							else if (field.FieldType == typeof(string)) valType = "=STR";
							else if (field.FieldType != typeof(bool)) valType = "=X";
						}

                        if (att.Mandatory) {
                            helpText.AppendFormat("{0}{1,-20}\t{2} (Mandatory)", optChar, field.Name+valType, att.Description);
                        }
                        else {
                            helpText.AppendFormat("{0}{1,-20}\t{2}", optChar, field.Name+valType, att.Description);
                        }
						if (att.Alias != null) 
							helpText.AppendFormat(" (Alias format: {0}{1}{2})", optChar, att.Alias, valType);
						helpText.Append( Environment.NewLine );
					}
				}
			}
			return helpText.ToString();
		}
	}
}


