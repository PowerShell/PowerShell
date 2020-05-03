// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Microsoft.Management.UI.Internal
{
    internal static class CommandHelper
    {
        internal static void ExecuteCommand(ICommand command, object parameter, IInputElement target)
        {
            RoutedCommand command2 = command as RoutedCommand;
            if (command2 != null)
            {
                if (command2.CanExecute(parameter, target))
                {
                    command2.Execute(parameter, target);
                }
            }
            else if (command.CanExecute(parameter))
            {
                command.Execute(parameter);
            }
        }

        internal static bool CanExecuteCommand(ICommand command, object parameter, IInputElement target)
        {
            if (command == null)
            {
                return false;
            }

            RoutedCommand command2 = command as RoutedCommand;

            if (command2 != null)
            {
                return command2.CanExecute(parameter, target);
            }
            else
            {
                return command.CanExecute(parameter);
            }
        }
    }
}
