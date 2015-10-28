namespace Microsoft.Samples.PowerShell.Host
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Management.Automation;
    using System.Management.Automation.Host;
    using System.Text;

    /// <summary>
    /// A sample implementation of the PSHostUserInterface abstract class for 
    /// console applications. Not all members are implemented. Those that are 
    /// not implemented throw a NotImplementedException exception or return 
    /// nothing. Members that are implemented include those that map easily to 
    /// Console APIs and a basic implementation of the prompt API provided. 
    /// </summary>
    internal class MyHostUserInterface : PSHostUserInterface, IHostUISupportsMultipleChoiceSelection
    {
        /// <summary>
        /// A reference to the PSRawUserInterface implementation.
        /// </summary>
        private MyRawUserInterface myRawUi = new MyRawUserInterface();

        /// <summary>
        /// Gets an instance of the PSRawUserInterface class for this host
        /// application.
        /// </summary>
        public override PSHostRawUserInterface RawUI
        {
            get { return this.myRawUi; }
        }

        /// <summary>
        /// Prompts the user for input. 
        /// <param name="caption">The caption or title of the prompt.</param>
        /// <param name="message">The text of the prompt.</param>
        /// <param name="descriptions">A collection of FieldDescription objects  
        /// that describe each field of the prompt.</param>
        /// <returns>A dictionary object that contains the results of the user 
        /// prompts.</returns>
        public override Dictionary<string, PSObject> Prompt(
                string caption, 
                string message,
                Collection<FieldDescription> descriptions)
        {
            this.Write(
                    ConsoleColor.White, 
                    ConsoleColor.Black,
                    caption + "\n" + message + " ");
            Dictionary<string, PSObject> results =
                new Dictionary<string, PSObject>();
            foreach (FieldDescription fd in descriptions)
            {
                string[] label = GetHotkeyAndLabel(fd.Label);
                this.WriteLine(label[1]);
                string userData = Console.ReadLine();
                if (userData == null)
                {
                    return null;
                }

                results[fd.Name] = PSObject.AsPSObject(userData);
            }

            return results;
        }

        /// <summary>

        /// Provides a set of choices that enable the user to choose a 
        /// single option from a set of options. 
        /// </summary>
        /// <param name="caption">Text that proceeds (a title) the choices.</param>
        /// <param name="message">A message that describes the choice.</param>
        /// <param name="choices">A collection of ChoiceDescription objects that  
        /// describ each choice.</param>
        /// <param name="defaultChoice">The index of the label in the Choices  
        /// parameter collection. To indicate no default choice, set to -1.</param>
        /// <returns>The index of the Choices parameter collection element that 
        /// corresponds to the option that is selected by the user.</returns>
        public override int PromptForChoice(
                string caption, 
                string message,
                Collection<ChoiceDescription> choices, 
                int defaultChoice)
        {
            // Write the caption and message strings in Blue.
            this.WriteLine(
                    ConsoleColor.Blue, 
                    ConsoleColor.Black,
                    caption + "\n" + message + "\n");

            // Convert the choice collection into something that is
            // easier to work with. See the BuildHotkeysAndPlainLabels 
            // method for details.
            string[,] promptData = BuildHotkeysAndPlainLabels(choices);

            // Format the overall choice prompt string to display.
            StringBuilder sb = new StringBuilder();
            for (int element = 0; element < choices.Count; element++)
            {
                sb.Append(String.Format(
                            CultureInfo.CurrentCulture, 
                            "|{0}> {1} ",
                            promptData[0, element], 
                            promptData[1, element]));
            }

            sb.Append(String.Format(
                        CultureInfo.CurrentCulture, 
                        "[Default is ({0}]",
                        promptData[0, defaultChoice]));

            // Read prompts until a match is made, the default is
            // chosen, or the loop is interrupted with ctrl-C.
            while (true)
            {
                this.WriteLine(ConsoleColor.Cyan, ConsoleColor.Black, sb.ToString());
                string data = Console.ReadLine().Trim().ToUpper();

                // If the choice string was empty, use the default selection.
                if (data.Length == 0)
                {
                    return defaultChoice;
                }

                // See if the selection matched and return the
                // corresponding index if it did.
                for (int i = 0; i < choices.Count; i++)
                {
                    if (promptData[0, i] == data)
                    {
                        return i;
                    }
                }

                this.WriteErrorLine("Invalid choice: " + data);
            }
        }

#region IHostUISupportsMultipleChoiceSelection Members

        /// <summary>
        /// Provides a set of choices that enable the user to choose a one or 
        /// more options from a set of options. 
        /// </summary>
        /// <param name="caption">Text that proceeds (a title) the choices.</param>
        /// <param name="message">A message that describes the choice.</param>
        /// <param name="choices">A collection of ChoiceDescription objects that  
        /// describ each choice.</param>
        /// <param name="defaultChoices">The index of the label in the Choices  
        /// parameter collection. To indicate no default choice, set to -1.</param>
        /// <returns>The index of the Choices parameter collection element that 
        /// corresponds to the option that is selected by the user.</returns>
        public Collection<int> PromptForChoice(
                string caption, 
                string message,
                Collection<ChoiceDescription> choices, 
                IEnumerable<int> defaultChoices)
        {
            // Write the caption and message strings in Blue.
            this.WriteLine(
                    ConsoleColor.Blue, 
                    ConsoleColor.Black,
                    caption + "\n" + message + "\n");

            // Convert the choice collection into something that is
            // easier to work with. See the BuildHotkeysAndPlainLabels 
            // method for details.
            string[,] promptData = BuildHotkeysAndPlainLabels(choices);

            // Format the overall choice prompt string to display.
            StringBuilder sb = new StringBuilder();
            for (int element = 0; element < choices.Count; element++)
            {
                sb.Append(String.Format(
                            CultureInfo.CurrentCulture, 
                            "|{0}> {1} ",
                            promptData[0, element], 
                            promptData[1, element]));
            }

            Collection<int> defaultResults = new Collection<int>();
            if (defaultChoices != null)
            {
                int countDefaults = 0;
                foreach (int defaultChoice in defaultChoices)
                {
                    ++countDefaults;
                    defaultResults.Add(defaultChoice);
                }

                if (countDefaults != 0)
                {
                    sb.Append(countDefaults == 1 ? "[Default choice is " : "[Default choices are ");
                    foreach (int defaultChoice in defaultChoices)
                    {
                        sb.AppendFormat(
                                CultureInfo.CurrentCulture, 
                                "\"{0}\",",
                                promptData[0, defaultChoice]);
                    }

                    sb.Remove(sb.Length - 1, 1);
                    sb.Append("]");
                }
            }

            this.WriteLine(
                    ConsoleColor.Cyan, 
                    ConsoleColor.Black, 
                    sb.ToString());
            // Read prompts until a match is made, the default is
            // chosen, or the loop is interrupted with ctrl-C.
            Collection<int> results = new Collection<int>();
            while (true)
            {
ReadNext:
                string prompt = string.Format(CultureInfo.CurrentCulture, "Choice[{0}]:", results.Count);
                this.Write(ConsoleColor.Cyan, ConsoleColor.Black, prompt);
                string data = Console.ReadLine().Trim().ToUpper();

                // If the choice string was empty, no more choices have been made.
                // If there were no choices made, return the defaults
                if (data.Length == 0)
                {
                    return (results.Count == 0) ? defaultResults : results;
                }

                // See if the selection matched and return the
                // corresponding index if it did.
                for (int i = 0; i < choices.Count; i++)
                {
                    if (promptData[0, i] == data)
                    {
                        results.Add(i);
                        goto ReadNext;
                    }
                }

                this.WriteErrorLine("Invalid choice: " + data);
            }
        }

#endregion

        /// <summary>
        /// Prompts the user for credentials with a specified prompt window 
        /// caption, prompt message, user name, and target name. In this 
        /// example this functionality is not needed so the method throws a 
        /// NotImplementException exception.
        /// </summary>
        /// <param name="caption">The caption for the message window.</param>
        /// <param name="message">The text of the message.</param>
        /// <param name="userName">The user name whose credential is to be 
        /// prompted for.</param>
        /// <param name="targetName">The name of the target for which the 
        /// credential is collected.</param>
        /// <returns>Throws a NotImplementedException exception.</returns>
        public override PSCredential PromptForCredential(
                string caption, 
                string message, 
                string userName, 
                string targetName)
        {
            throw new NotImplementedException(
                    "The method or operation is not implemented.");
        }

        /// <summary>
        /// Prompts the user for credentials by using a specified prompt window 
        /// caption, prompt message, user name and target name, credential 
        /// types allowed to be returned, and UI behavior options. In this 
        /// example this functionality is not needed so the method throws a 
        /// NotImplementException exception.
        /// </summary>
        /// <param name="caption">The caption for the message window.</param>
        /// <param name="message">The text of the message.</param>
        /// <param name="userName">The user name whose credential is to be 
        /// prompted for.</param>
        /// <param name="targetName">The name of the target for which the 
        /// credential is collected.</param>
        /// <param name="allowedCredentialTypes">A PSCredentialTypes constant  
        /// that identifies the type of credentials that can be returned.</param>
        /// <param name="options">A PSCredentialUIOptions constant that 
        /// identifies the UI behavior when it gathers the credentials.</param>
        /// <returns>Throws a NotImplementedException exception.</returns>
        public override PSCredential PromptForCredential(
                string caption, 
                string message, 
                string userName, 
                string targetName, 
                PSCredentialTypes allowedCredentialTypes, 
                PSCredentialUIOptions options)
        {
            throw new NotImplementedException(
                    "The method or operation is not implemented.");
        }

        /// <summary>
        /// Reads characters that are entered by the user until a newline 
        /// (carriage return) is encountered.
        /// </summary>
        /// <returns>The characters that are entered by the user.</returns>
        public override string ReadLine()
        {
            return Console.ReadLine();
        }

        /// <summary>
        /// Reads characters entered by the user until a newline (carriage return) 
        /// is encountered and returns the characters as a secure string. In this 
        /// example this functionality is not needed so the method throws a 
        /// NotImplementException exception.
        /// </summary>
        /// <returns>Throws a NotImplemented exception.</returns>
        public override System.Security.SecureString ReadLineAsSecureString()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        /// <summary>
        /// Writes characters to the output display of the host.
        /// </summary>
        /// <param name="value">The characters to be written.</param>
        public override void Write(string value)
        {
            Console.Write(value); 
        }

        /// <summary>
        /// Writes characters to the output display of the host with possible 
        /// foreground and background colors. 
        /// </summary>
        /// <param name="foregroundColor">The color of the characters.</param>
        /// <param name="backgroundColor">The backgound color to use.</param>
        /// <param name="value">The characters to be written.</param>
        public override void Write(
                ConsoleColor foregroundColor,
                ConsoleColor backgroundColor, 
                string value)
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
            Console.Write(value);
            //Console.Write(value);
            Console.ResetColor();
        }


        /// <summary>
        /// Writes a line of characters to the output display of the host 
        /// with foreground and background colors and appends a newline (carriage return). 
        /// </summary>
        /// <param name="foregroundColor">The forground color of the display. </param>
        /// <param name="backgroundColor">The background color of the display. </param>
        /// <param name="value">The line to be written.</param>
        public override void WriteLine(
                ConsoleColor foregroundColor,
                ConsoleColor backgroundColor, 
                string value)
        {
            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
            Console.WriteLine(value);
            Console.ResetColor();
        }

        /// <summary>
        /// Writes a debug message to the output display of the host.
        /// </summary>
        /// <param name="message">The debug message that is displayed.</param>
        public override void WriteDebugLine(string message)
        {
            this.WriteLine(
                    ConsoleColor.Yellow, 
                    ConsoleColor.Black,
                    String.Format(CultureInfo.CurrentCulture, "DEBUG: {0}", message));
        }


        /// <summary>
        /// Writes an error message to the output display of the host.
        /// </summary>
        /// <param name="value">The error message that is displayed.</param>
        public override void WriteErrorLine(string value)
        {
            this.WriteLine(
                    ConsoleColor.Red, 
                    ConsoleColor.Black, 
                    value);
        }

        /// <summary>
        /// Writes a newline character (carriage return) 
        /// to the output display of the host. 
        /// </summary>
        public override void WriteLine()
        {
            Console.WriteLine();
        }

        /// <summary>
        /// Writes a line of characters to the output display of the host 
        /// and appends a newline character(carriage return). 
        /// </summary>
        /// <param name="value">The line to be written.</param>
        public override void WriteLine(string value)
        {
            Console.WriteLine(value);
        }

        /// <summary>
        /// Writes a progress report to the output display of the host.
        /// </summary>
        /// <param name="sourceId">Unique identifier of the source of the record. </param>
        /// <param name="record">A ProgressReport object.</param>
        public override void WriteProgress(long sourceId, ProgressRecord record)
        {

        }

        /// <summary>
        /// Writes a verbose message to the output display of the host.
        /// </summary>
        /// <param name="message">The verbose message that is displayed.</param>
        public override void WriteVerboseLine(string message)
        {
            this.WriteLine(
                    ConsoleColor.Green, 
                    ConsoleColor.Black,
                    String.Format(CultureInfo.CurrentCulture, "VERBOSE: {0}", message));
        }

        /// <summary>
        /// Writes a warning message to the output display of the host.
        /// </summary>
        /// <param name="message">The warning message that is displayed.</param>
        public override void WriteWarningLine(string message)
        {
            this.WriteLine(
                    ConsoleColor.Yellow, 
                    ConsoleColor.Black,
                    String.Format(CultureInfo.CurrentCulture, "WARNING: {0}", message));
        }


        /// <summary>
        /// Parse a string containing a hotkey character.
        /// Take a string of the form
        ///    Yes to &amp;all
        /// and returns a two-dimensional array split out as
        ///    "A", "Yes to all".
        /// </summary>
        /// <param name="input">The string to process</param>
        /// <returns>
        /// A two dimensional array containing the parsed components.
        /// </returns>
        private static string[] GetHotkeyAndLabel(string input)
        {
            string[] result = new string[] { String.Empty, String.Empty };
            string[] fragments = input.Split('&');
            if (fragments.Length == 2)
            {
                if (fragments[1].Length > 0)
                {
                    result[0] = fragments[1][0].ToString().ToUpper();
                }

                result[1] = (fragments[0] + fragments[1]).Trim();
            }
            else
            {
                result[1] = input;
            }

            return result;
        }

        /// <summary>
        /// This is a private worker function splits out the
        /// accelerator keys from the menu and builds a two
        /// dimentional array with the first access containing the
        /// accelerator and the second containing the label string
        /// with the &amp; removed.
        /// </summary>
        /// <param name="choices">The choice collection to process</param>
        /// <returns>
        /// A two dimensional array containing the accelerator characters
        /// and the cleaned-up labels</returns>
        private static string[,] BuildHotkeysAndPlainLabels(
                Collection<ChoiceDescription> choices)
        {
            // Allocate the result array
            string[,] hotkeysAndPlainLabels = new string[2, choices.Count];

            for (int i = 0; i < choices.Count; ++i)
            {
                string[] hotkeyAndLabel = GetHotkeyAndLabel(choices[i].Label);
                hotkeysAndPlainLabels[0, i] = hotkeyAndLabel[0];
                hotkeysAndPlainLabels[1, i] = hotkeyAndLabel[1];
            }

            return hotkeysAndPlainLabels;
        }
    }
}

