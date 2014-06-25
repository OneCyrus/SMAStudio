﻿using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SMAStudio.Commands
{
    public class RevertCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            if (parameter == null)
                return false;

            if (!(parameter is RunbookViewModel))
                return false;

            return true;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            if (parameter == null)
                return;

            var runbook = (RunbookViewModel)parameter;

            RevisionsWindow window = new RevisionsWindow(runbook);
            if ((bool)window.ShowDialog())
            {
                // Revert to this runbook
                var revision = window.SelectedVersion;

                if (revision == null)
                {
                    MessageBox.Show("The selected revision does not exist. Please try again with another one.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Core.Log.DebugFormat("Reverting {0} to revision {1}.", runbook.RunbookName, revision.VersionNumber);

                // How should we handle this? As I see it there are three scenarios...
                // 1 - Overwrite the existing revision with the old one, how will that
                //     work since the version number will be lower than the latest. Don't know
                //
                // 2 - Replace the content of the newest revision with the code from this one. Safest!
                //
                // 3 - Check in the runbook and then check it out again to create a revision of the old
                //     code and then replace the content with the reverted content.
                //
                // I'm going with option 3 at the moment as I see that it's the cleanest
                var checkInCommand = new CheckInCommand();
                var checkOutCommand = new CheckOutCommand();
                checkOutCommand.SilentCheckOut = true;

                // Check in the runbook
                Core.Log.DebugFormat("Checking in current revision of the runbook.");
                checkInCommand.Execute(runbook);

                // Check out the runbook
                Core.Log.DebugFormat("Checking out the runbook again, to create a new revision. SILENT MODE.");
                checkOutCommand.Execute(runbook);

                // Set the content as well
                Core.Log.DebugFormat("Downloading the content from the revision we're reverting to. ID: {0}", revision.RunbookVersion.RunbookVersionID);
                runbook.Content = revision.GetContent(true /* we always want the latest info at this point */);

                // Since SMA Studio won't detect the changes by itself (for a good reason),
                // we inform it about the unsaved changes :-)
                runbook.UnsavedChanges = true;
            }
        }
    }
}
