/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.PowerShell
{
    public partial class PSConsoleReadLine
    {
        private string _visualEditTemporaryFilename = null;
        private Func<string, bool> _savedAddToHistoryHandler = null;

        /// <summary>
        /// Edit the command line in a text editor specified by $env:EDITOR or $env:VISUAL
        /// </summary>
        public static void ViEditVisually(ConsoleKeyInfo? key = null, object arg = null)
        {
            string editorOfChoice = GetPreferredEditor();
            if (string.IsNullOrWhiteSpace(editorOfChoice))
            {
                Ding();
                return;
            }

            _singleton._visualEditTemporaryFilename = GetTemporaryPowerShellFile();
            using (FileStream fs = File.OpenWrite(_singleton._visualEditTemporaryFilename))
            {
                using (TextWriter tw = new StreamWriter(fs))
                {
                    tw.Write(_singleton._buffer.ToString());
                }
            }

            _singleton._savedAddToHistoryHandler = _singleton.Options.AddToHistoryHandler;
            _singleton.Options.AddToHistoryHandler = ((string s) =>
            {
                return false;
            });

            _singleton._buffer.Clear();
            _singleton._current = 0;
            _singleton.Render();
            _singleton._buffer.Append(editorOfChoice + " \'" + _singleton._visualEditTemporaryFilename + "\'");
            AcceptLine();
        }

        private static string GetTemporaryPowerShellFile()
        {
            string filename;
            do
            {
                filename = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".ps1");
            } while (File.Exists(filename) || Directory.Exists(filename));

            return filename;
        }

        private void ProcessViVisualEditing()
        {
            if (_visualEditTemporaryFilename == null)
            {
                return;
            }

            Options.AddToHistoryHandler = _savedAddToHistoryHandler;
            _savedAddToHistoryHandler = null;

            string editedCommand = null;
            using (TextReader tr = File.OpenText(_visualEditTemporaryFilename))
            {
                editedCommand = tr.ReadToEnd();
            }
            File.Delete(_visualEditTemporaryFilename);
            _visualEditTemporaryFilename = null;

            if (!string.IsNullOrWhiteSpace(editedCommand))
            {
                while (editedCommand.Length > 0 && char.IsWhiteSpace(editedCommand[editedCommand.Length - 1]))
                {
                    editedCommand = editedCommand.Substring(0, editedCommand.Length - 1);
                }
                editedCommand = editedCommand.Replace(Environment.NewLine, "\n");
                _buffer.Clear();
                _buffer.Append(editedCommand);
                _current = _buffer.Length - 1;
                Render();
                //_queuedKeys.Enqueue(Keys.Enter);
            }
        }

        private static string GetPreferredEditor()
        {
            string[] names = {"VISUAL", "EDITOR"};
            EnvironmentVariableTarget[] targets = {
                                                      EnvironmentVariableTarget.Machine,
                                                      EnvironmentVariableTarget.Process,
                                                      EnvironmentVariableTarget.User
                                                  };
            foreach (string name in names)
            {
                foreach (EnvironmentVariableTarget target in targets)
                {
                    string editor = Environment.GetEnvironmentVariable(name, target);
                    if (!string.IsNullOrWhiteSpace(editor))
                    {
                        return editor;
                    }
                }
            }

            return null;
        }
    }
}
