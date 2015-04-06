﻿using SMAStudio.SMAWebService;
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
            Schedule vari = null;

            try
            {
                if (schedule.Schedule.ScheduleID != Guid.Empty)
                {
                    vari = _api.Current.Schedules.Where(v => v.ScheduleID.Equals(schedule.Schedule.ScheduleID)).FirstOrDefault();

                    if (vari == null)
                        return false;

                    //if (vari.IsEncrypted != schedule.IsEncrypted)
                    //{
                    //    MessageBox.Show("You cannot change encryption status of a variable.", "Error");
                    //    return false;
                    //}

                    vari.Name = schedule.Schedule.Name;
                    vari.Description = schedule.Schedule.Description;
                    vari.StartTime = schedule.Schedule.StartTime;
                    vari.IsEnabled = schedule.Schedule.IsEnabled;
                    if (schedule.IsExpirationEnabled)
                    {
                        vari.ExpiryTime = schedule.Schedule.ExpiryTime;
                    }
                    else
                    {
                        vari.ExpiryTime = null;
                    }

                    _api.Current.UpdateObject(schedule.Schedule);
                    _api.Current.SaveChanges();
                }
                else
                {
                    vari = new OneTimeSchedule();

                    vari.Name = schedule.Name;
                    vari.Description = schedule.Content;
                    vari.Name = schedule.Schedule.Name;
                    vari.Description = schedule.Schedule.Description;
                    vari.StartTime = schedule.Schedule.StartTime;
                    vari.IsEnabled = schedule.Schedule.IsEnabled;
                    vari.ExpiryTime = schedule.Schedule.ExpiryTime;

                    _api.Current.AddToSchedules(vari);
                    _api.Current.SaveChanges();

                    schedule.Schedule = vari;
                }

                schedule.UnsavedChanges = false;
                schedule.CachedChanges = false;

                _componentsViewModel.AddSchedule(schedule);

                return true;
            }
            catch (Exception ex)
            {
                Core.Log.Error("Unable to save the variable.", ex);
                MessageBox.Show("An error occurred when saving the variable. Please refer to the logs for more information.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
