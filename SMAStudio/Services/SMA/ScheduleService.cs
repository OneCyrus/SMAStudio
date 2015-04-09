using SMAStudio.SMAWebService;
using SMAStudio.Util;
using SMAStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Services.Client;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SMAStudio.Services
{
    sealed class ScheduleService : BaseService, IScheduleService
    {
        private IApiService _api;
        private IList<Schedule> _scheduleCache = null;
        private ObservableCollection<ScheduleViewModel> _scheduleViewModelCache = null;

        private IWorkspaceViewModel _workspaceViewModel;
        private IComponentsViewModel _componentsViewModel;

        public ScheduleService()
        {
            _api = Core.Resolve<IApiService>();
            _workspaceViewModel = Core.Resolve<IWorkspaceViewModel>();
            _componentsViewModel = Core.Resolve<IComponentsViewModel>();
        }

        public IList<Schedule> GetSchedules(bool forceDownload = false)
        {
            try
            {
                if (_scheduleCache == null || forceDownload)
                    _scheduleCache = _api.Current.Schedules.OrderBy(v => v.Name).ToList();

                return _scheduleCache;
            }
            catch (DataServiceTransportException e)
            {
                Core.Log.Error("Unable to retrieve schedule from SMA", e);
                base.NotifyConnectionError();

                return new List<Schedule>();
            }
        }

        public ObservableCollection<ScheduleViewModel> GetScheduleViewModels(bool forceDownload = false)
        {
            if (_scheduleCache == null || forceDownload)
                GetSchedules(forceDownload);

            if (_scheduleViewModelCache != null && !forceDownload)
                return _scheduleViewModelCache;

            _scheduleViewModelCache = new ObservableCollection<ScheduleViewModel>();

            if (_scheduleCache == null)
                return new ObservableCollection<ScheduleViewModel>();

            foreach (var schedule in _scheduleCache)
            {
                var viewModel = new ScheduleViewModel
                {
                    Schedule = schedule
                };

                _scheduleViewModelCache.Add(viewModel);
            }

            return _scheduleViewModelCache;
        }

        public bool Create()
        {
            try
            {
                var newSchedule = new ScheduleViewModel
                {
                    Schedule = new SMAWebService.OneTimeSchedule(),
                    CheckedOut = true,
                    UnsavedChanges = true
                };
                
                newSchedule.Schedule.Name = string.Empty;
                newSchedule.Schedule.Description = string.Empty;

                newSchedule.Schedule.ScheduleID = Guid.Empty;
                newSchedule.StartTime = DateTime.Now;

                _workspaceViewModel.OpenDocument(newSchedule);

                // Reload the data from SMA
                _componentsViewModel.Load(true /* force download */);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to create a new schedule.", ex);
            }

            return false;
        }

        public bool Update(ScheduleViewModel schedule)
        {
            Schedule sched = null;

            try
            {
                var _runbookService = Core.Resolve<RunbookService>();

                if (schedule.Schedule.ScheduleID != Guid.Empty)
                {
                    sched = _api.Current.Schedules.Where(v => v.ScheduleID.Equals(schedule.Schedule.ScheduleID)).FirstOrDefault();

                    if (sched == null)
                        return false;

                    //if (vari.IsEncrypted != schedule.IsEncrypted)
                    //{
                    //    MessageBox.Show("You cannot change encryption status of a variable.", "Error");
                    //    return false;
                    //}

                    sched.Name = schedule.Schedule.Name;
                    sched.Description = schedule.Schedule.Description;
                    sched.StartTime = schedule.Schedule.StartTime.ToUniversalTime();
                    sched.IsEnabled = schedule.Schedule.IsEnabled;
                    if (schedule.IsExpirationEnabled)
                    {
                        sched.ExpiryTime = schedule.Schedule.ExpiryTime;
                    }
                    else
                    {
                        sched.ExpiryTime = null;
                    }

                    _api.Current.UpdateObject(schedule.Schedule);
                    _api.Current.SaveChanges();
                }
                else
                {
                    sched = new OneTimeSchedule();

                    sched.Name = schedule.Name;
                    sched.Description = schedule.Content;
                    sched.Name = schedule.Schedule.Name;
                    sched.Description = schedule.Schedule.Description;
                    sched.StartTime = schedule.Schedule.StartTime.ToUniversalTime();
                    sched.IsEnabled = schedule.Schedule.IsEnabled;
                    sched.ExpiryTime = schedule.Schedule.ExpiryTime;
                                                                                                                     
                    _api.Current.AddToSchedules(sched);                                                          
                    _api.Current.SaveChanges();

                    var jobsched = _api.Current.Schedules.Where(x => x.Name == schedule.Name).First();

                    var runbook = _runbookService.GetRunbook("DiscoverAllLocalModules");
                    runbook.StartOnSchedule(_api.Current, jobsched);
                    


                    //var runbook = _runbookService.GetRunbook("DiscoverAllLocalModules");
                    //runbook.StartOnSchedule(_api.Current, sched);

                    schedule.Schedule = sched;
                }

                schedule.UnsavedChanges = false;
                schedule.CachedChanges = false;

                _componentsViewModel.AddSchedule(schedule);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to save the schedule.", ex);
                MessageBox.Show("An error occurred when saving the schedule. Please refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        public bool Delete(ScheduleViewModel scheduleViewModel)
        {
            try
            {
                var variable = _api.Current.Schedules.Where(v => v.ScheduleID == scheduleViewModel.ID).FirstOrDefault();

                if (variable == null)
                {
                    Core.Log.DebugFormat("Trying to remove a schedule that doesn't exist. GUID {0}", scheduleViewModel.ID);
                    return false;
                }

                _api.Current.DeleteObject(variable);
                _api.Current.SaveChanges();

                // Remove the variable from the list of variables
                if (_componentsViewModel != null)
                    _componentsViewModel.RemoveSchedule(scheduleViewModel);

                // If the variable is open, we close it
                if (_workspaceViewModel != null && _workspaceViewModel.Documents.Contains(scheduleViewModel))
                    _workspaceViewModel.Documents.Remove(scheduleViewModel);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to remove the variable.", ex);
                MessageBox.Show("An error occurred when trying to remove the variable. Please refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }        
    }
}
