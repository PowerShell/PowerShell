# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
$definition = @'
using System;
using System.Collections.Generic;
using System.Threading;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Security;
using System.Collections;

namespace TestHost
{
    public class TestHostRawUI : PSHostRawUserInterface
    {
        private ConsoleColor _backgroundColor = ConsoleColor.Black;
        public override ConsoleColor BackgroundColor
        {
            get { return _backgroundColor; }
            set { _backgroundColor = value; }
        }
        private ConsoleColor _foregroundColor = ConsoleColor.White;
        public override ConsoleColor ForegroundColor
        {
            get { return _foregroundColor; }
            set { _foregroundColor = value; }
        }
        private string _windowTitle = "title";
        public override string WindowTitle
        {
            get { return _windowTitle; }
            set { _windowTitle = value; }
        }
        private Size _bufferSize = new Size(10,10);
        public override Size BufferSize
        {
            get { return _bufferSize; }
            set { _bufferSize = value; }
        }
        private Coordinates _cursorPosition = new Coordinates(0, 0);
        public override Coordinates CursorPosition
        {
            get { return _cursorPosition; }
            set { _cursorPosition = value; }
        }
        private int _cursorSize = 10;
        public override int CursorSize
        {
            get { return _cursorSize; }
            set { _cursorSize = value; }
        }
        private bool _keyAvailable = false;
        public override bool KeyAvailable
        {
            get { return _keyAvailable; }
        }
        public override Size MaxPhysicalWindowSize
        {
            get { throw new NotImplementedException(); }
        }
        public override Size MaxWindowSize
        {
            get { throw new NotImplementedException(); }
        }
        private Coordinates _windowPosition = new Coordinates(0, 0);
        public override Coordinates WindowPosition
        {
            get { return _windowPosition; }

            set { _windowPosition = value; }
        }
        private Size _windowSize = new Size(80, 40);
        public override Size WindowSize
        {
            get { return _windowSize; }
            set { _windowSize = value; }
        }
        public override BufferCell[,] GetBufferContents(Rectangle rectangle) { throw new NotImplementedException(); }
        public override void FlushInputBuffer() { throw new NotImplementedException(); }
        public override KeyInfo ReadKey(ReadKeyOptions options) { throw new NotImplementedException(); }
        public override void SetBufferContents(Coordinates origin, BufferCell[,] contents) { throw new NotImplementedException(); }
        public override void SetBufferContents(Rectangle rectangle, BufferCell fill) { throw new NotImplementedException(); }
        public override void ScrollBufferContents(Rectangle source, Coordinates destination, Rectangle clip, BufferCell fill) { throw new NotImplementedException(); }
    }

    public class Streams
    {
        public ArrayList ConsoleOutput = new ArrayList();
        public ArrayList Input         = new ArrayList();
        public ArrayList Error         = new ArrayList();
        public ArrayList Verbose       = new ArrayList();
        public ArrayList Debug         = new ArrayList();
        public ArrayList Warning       = new ArrayList();
        public ArrayList Information   = new ArrayList();
        public ArrayList Progress      = new ArrayList();
        public ArrayList Prompt        = new ArrayList();
        public void Clear() {
            ConsoleOutput.Clear();
            Input.Clear();
            Error.Clear();
            Verbose.Clear();
            Debug.Clear();
            Warning.Clear();
            Information.Clear();
            Progress.Clear();
            Prompt.Clear();
        }
    }
    public class TestPSHostUserInterface : PSHostUserInterface
    {
        private PSHostRawUserInterface _rawui = new TestHostRawUI();
        public string ReadLineData = "This is readline data";
        public int PromptedChoice = 0;
        public string StringForSecureString = "TEST";
        public string UserNameForCredential = "Admin";
        public object promptResponse = "this is a prompt response";

        public Streams Streams = new Streams();
        public override PSHostRawUserInterface RawUI
        {
            get { return _rawui; }
        }

        public override Dictionary<string, PSObject> Prompt(string caption, string message, Collection<FieldDescription> descriptions)
        {
            if (descriptions == null || descriptions[0] == null)
            {
                throw new ArgumentException("descriptions");
            }

            string s = descriptions[0].Name;
            Streams.Prompt.Add(caption + ":" + message + ":" + s);
            Dictionary<string, PSObject> d = new Dictionary<string, PSObject>();
            d.Add(s, new PSObject(promptResponse));
            return d;
        }

        public override int PromptForChoice(string caption, string message, Collection<ChoiceDescription> choices, int defaultChoice)
        {
            Streams.Prompt.Add(caption + ":" + message);
            return PromptedChoice;
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            Streams.Prompt.Add("Credential:" + caption + ":" + message);
            SecureString ss = ReadLineAsSecureString();
            string userNameToUse = string.IsNullOrEmpty(userName) ? UserNameForCredential : userName;
            return new PSCredential(userNameToUse, ss);
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            Streams.Prompt.Add("Credential:" + caption + ":" + message);
            SecureString ss = ReadLineAsSecureString();
            string userNameToUse = string.IsNullOrEmpty(userName) ? UserNameForCredential : userName;
            return new PSCredential(userNameToUse, ss);
        }

        public override string ReadLine()
        {
            return ReadLineData;
        }

        public override SecureString ReadLineAsSecureString()
        {
            SecureString ss = new SecureString();
            foreach(char c in StringForSecureString.ToCharArray()) { ss.AppendChar(c); }
            return ss;
        }

        // Cmdlets call 'Write' and 'WriteLine' methods implicitly.
        // To see difference between 'Write' and 'WriteLine' with and w/o colors in the debug output
        // we need use a meta information.
        // So we make a output string as:
        // <Foregraund color name> : <Background color name> : <'user value'> : <'NewLine' or 'NoNewLine'>
        //
        public override void Write(string value)
        {
            Streams.ConsoleOutput.Add("::"+value+":NoNewLine");
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            Streams.ConsoleOutput.Add(foregroundColor+":"+backgroundColor+":"+value+":NoNewLine");
        }

        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            Streams.ConsoleOutput.Add(foregroundColor+":"+backgroundColor+":"+value+":NewLine");
        }

        public override void WriteDebugLine(string message)
        {
            Streams.Debug.Add(message);
        }

        public override void WriteErrorLine(string value)
        {
            Streams.Error.Add(value);
        }

        public override void WriteLine(string value)
        {
            Streams.ConsoleOutput.Add("::"+value+":NewLine");
        }

        public override void WriteProgress(long sourceId, ProgressRecord record)
        {
            Streams.Progress.Add(record);
        }

        public override void WriteVerboseLine(string message)
        {
            Streams.Verbose.Add(message);
        }

        public override void WriteWarningLine(string message)
        {
            Streams.Warning.Add(message);
        }

        public override void WriteInformation(InformationRecord record)
        {
            HostInformationMessage hostOutput = record.MessageData as HostInformationMessage;
            if (hostOutput != null) {
                 string message = hostOutput.Message;
                 Streams.Information.Add(message);
            }
        }
    }

    public class TestHost : PSHost
    {
        private Guid _instanceId = Guid.NewGuid();
        private PSHostUserInterface _ui = new TestPSHostUserInterface();
        public string _hostName = "TEST HOST";
        public Version _version = new Version(6, 0);
        int promptLevel = 0;
        public override CultureInfo CurrentCulture
        {
            get { return Thread.CurrentThread.CurrentCulture; }
        }

        public override CultureInfo CurrentUICulture
        {
            get {  return Thread.CurrentThread.CurrentUICulture; }
        }

        public override Guid InstanceId
        {
            get { return _instanceId; }
        }

        public override string Name
        {
            get { return _hostName; }
        }

        public override PSHostUserInterface UI
        {
            get { return _ui; }
        }

        public override Version Version
        {
            get { return _version; }
        }

        public override void EnterNestedPrompt()
        {
            promptLevel++;
        }

        public override void ExitNestedPrompt()
        {
            promptLevel--;
        }

        public override void NotifyBeginApplication()
        {
            throw new NotImplementedException();
        }

        public override void NotifyEndApplication()
        {
            throw new NotImplementedException();
        }

        public override void SetShouldExit(int exitCode)
        {
            throw new NotImplementedException();
        }
    }
}

'@
## '
function New-TestHost
{
    If ($IsCoreCLR)
    {
        $references = @()
    }
    else
    {
        $references = "System.Management.Automation"
    }

    if ( ! ("TestHost.TestHost" -as "type" )) {
       $t = Add-Type -pass $definition -ref $references
    }

    [TestHost.TestHost]::New()
}
