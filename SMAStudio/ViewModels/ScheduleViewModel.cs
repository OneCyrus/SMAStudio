using SMAStudio.Models;
using SMAStudio.Resources;
using SMAStudio.SMAWebService;
using SMAStudio.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SMAStudio.ViewModels
{
    public class ScheduleViewModel : ObservableObject, IDocumentViewModel
    {
        private bool _unsavedChanges = false;
        private string _icon = Icons.Variable;
        private Schedule _schedule = null;

        public ScheduleViewModel()
        {

        }

        /// <summary>
        /// Event triggered when the text changes in the text editor when this variable
        /// is active.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void TextChanged(object sender, EventArgs e)
        {
            if (!(sender is TextBox))
                return;

            TextBox textBox = (TextBox)sender;
            bool isNameBox = textBox.Tag.Equals("Name") ? true : false;

            if (isNameBox)
            {
                if (!textBox.Text.Equals(Name))
                {
                    Name = textBox.Text;
                    UnsavedChanges = true;
                }
            }
            else
            {
                if (!textBox.Text.Equals(Content))
                {
                    Content = textBox.Text;
                    UnsavedChanges = true;
                }
            }
        }

        public void DocumentLoaded()
        {
            
        }

        #region Properties
        /// <summary>
        /// Gets or sets the schedule model object
        /// </summary>
        public Schedule Schedule
        {
            get { return _schedule; }
            set
            {
                _schedule = value;
                base.RaisePropertyChanged("Title");
                base.RaisePropertyChanged("Name");
            }
        }

        /// <summary>
        /// Gets or sets the ID of the variable
        /// </summary>
        public Guid ID
        {
            get { return Schedule.ScheduleID; }
            set { Schedule.ScheduleID = value; }
        }

        /// <summary>
        /// Gets the variable name accompanied wth a asterisk (*) if the variable contains
        /// unsaved data
        /// </summary>
        public string Title
        {
            get
            {
                string variableName = Schedule.Name;

                if (String.IsNullOrEmpty(variableName))
                    variableName += "untitled";

                if (UnsavedChanges)
                    variableName += "*";                

                return variableName;
            }
            set { Schedule.Name = value; }
        }

        /// <summary>
        /// Gets or sets the name of the variable
        /// </summary>
        public string Name
        {
            get
            {
                return Schedule.Name;
            }
            set { Schedule.Name = value; }
        }

        /// <summary>
        /// Gets or sets the value of the variable
        /// </summary>
        public string Content
        {
            get { return Schedule.Description == null ? "" : Schedule.Description;}
            set { Schedule.Description = value; }
        }

        /// <summary>
        /// Gets or sets wether or not this variable is encrypted
        /// </summary>
        public ScheduleType ScheduleType
        {                        
            get { if(Schedule.GetType() == typeof(DailySchedule)) return ScheduleType.DailySchedule; else return ScheduleType.OneTimeSchedule; }          
        }

        public DateTime CreationTime
        {
            get { return Schedule.CreationTime; }
            set { Schedule.CreationTime = value; }
        }

        public String Description
        {
            get { return Schedule.Description; }
            set { Schedule.Description = value; }
        }
        public DateTime? ExpiryTime
        {
            get { return Schedule.ExpiryTime; }
            set { Schedule.ExpiryTime = value; }
        }
        public bool IsEnabled
        {
            get { return Schedule.IsEnabled; }
            set { Schedule.IsEnabled = value; }
        }
        public DateTime LastModifiedTime
        {
            get { return Schedule.LastModifiedTime; }
            set { Schedule.LastModifiedTime = value; }
        }
        public DateTime? NextRun
        {
            get { return Schedule.NextRun; }
            set { Schedule.NextRun = value; }
        }

        public bool IsExpirationEnabled
        {
            get { return Schedule.ExpiryTime == null ? false : true; }
            set { IsExpirationEnabled = value; }
        }
        public List<Runbook> Runbooks
        {
            get { return Schedule.Runbooks.ToList(); }
            //private set { Schedule.Runbooks = value; }
        }
        public Guid ScheduleID
        {
            get { return Schedule.ScheduleID; }
            set { Schedule.ScheduleID = value; }
        }
        public DateTime StartTime
        {
            get { return Schedule.StartTime; }
            set { Schedule.StartTime = value; }
        }
        public Guid TenantID
        {
            get { return Schedule.TenantID; }
            set { Schedule.TenantID = value; }
        }
        public byte DayInterval
        {
            get { if (Schedule.GetType() == typeof(DailySchedule)) {return ((DailySchedule)Schedule).DayInterval; } else return new byte();}
            set { if (Schedule.GetType() == typeof(DailySchedule)) { ((DailySchedule)Schedule).DayInterval = value; } }
        }

        /// <summary>
        /// Gets or sets whether this Runbook contains unsaved work
        /// </summary>
        public bool UnsavedChanges
        {
            get { return _unsavedChanges; }
            set
            {
                if (_unsavedChanges.Equals(value))
                    return;

                _unsavedChanges = value;

                // Set the CachedChanges to false in order for our auto saving engine to store a
                // local copy in case the application crashes
                CachedChanges = false;

                base.RaisePropertyChanged("Name");
                base.RaisePropertyChanged("Title");
            }
        }

        /// <summary>
        /// Set to true if the runbook contains changes that are cached and not saved
        /// </summary>
        public bool CachedChanges
        {
            get;
            set;
        }

        /// <summary>
        /// Will always return true since a variable is always checked out
        /// </summary>
        public bool CheckedOut
        {
            get { return true; }
            set { }
        }

        /// <summary>
        /// Icon for a variable
        /// </summary>
        public string Icon
        {
            get { return _icon; }
            set { _icon = value; }
        }

        /// <summary>
        /// Last DateTime a key was pressed in the text editor of this runbook instance
        /// </summary>
        public DateTime LastTimeKeyDown
        {
            get;
            set;
        }

        /// <summary>
        /// Unused for variables
        /// </summary>
        public ObservableCollection<DocumentReference> References
        {
            get;
            set;
        }

        public bool IsExpanded
        {
            get;
            set;
        }
        #endregion                
    }

    public enum ScheduleType
    {
        OneTimeSchedule = 0,
        DailySchedule = 1
    }
}
